using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using WBand.WebAPI.Data;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class PartitionMigrationTests(DatabaseFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateMatrix_WhenUpgradingLegacyOrFlatContent_PreservesTextAndSharedSections(bool alreadyAppliedFlatMigration)
    {
        // Use an isolated database so upgrading populated legacy tables cannot affect other tests.
        var database = "partition_migration_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(fixture.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"create database {database}", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = database, Pooling = false };
            await using var db = new DataContext(new DbContextOptionsBuilder<DataContext>().UseNpgsql(connection.ConnectionString).Options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261013211033_InitialWebApi");
            var userId = Guid.NewGuid();
            var bandId = Guid.NewGuid();
            var songId = Guid.NewGuid();
            var sectionId = Guid.NewGuid();
            const string lyrics = "  First line\nSecond line  \n";
            const string chords = "  [Am]  [G/B]\n[C]     [F]  ";
            await db.ExecuteAsync("""
                insert into public.users (id, keycloak_id, email, first_name, last_name, created_at, updated_at, last_login_at)
                    values (@userId, @userId, 'migration@example.com', '', '', now(), now(), now());
                insert into public.bands (id, name, created_by, created_at) values (@bandId, 'Migration', @userId, now());
                insert into public.songs (id, band_id, title, authors, status, content_version, created_at, created_by)
                    values (@songId, @bandId, 'Legacy', ARRAY[]::text[], 0, 1, now(), @userId);
                insert into public.song_sections (id, song_id, name, "order", lyrics, chords, revision, created_at, created_by, updated_at, updated_by)
                    values (@sectionId, @songId, 'Куплет', 5, @lyrics, @chords, 7, now(), @userId, now(), @userId),
                        (gen_random_uuid(), @songId, 'Аккорды', 10, null, @chords, 1, now(), @userId, null, null),
                        (gen_random_uuid(), @songId, 'Дубликат', 15, @lyrics, @lyrics, 1, now(), @userId, null, null),
                        (gen_random_uuid(), @songId, 'Пустая', 20, null, null, 1, now(), @userId, null, null);
                """, new { userId, bandId, songId, sectionId, lyrics, chords });

            if (alreadyAppliedFlatMigration)
            {
                await migrator.MigrateAsync("20261019224506_SongPartitions");
                // Simulate the originally applied version, before section backups were added.
                await db.ExecuteAsync("drop table public.song_matrix_legacy_sections, public.song_matrix_legacy_partitions");
                await db.ExecuteAsync("""
                    insert into public.song_partitions (id, song_id, name, "order", content, revision, created_at, created_by)
                        values (gen_random_uuid(), @songId, 'Заметки барабанщика', 50, 'Сбивка', 3, now(), @userId);
                    """, new { songId, userId });
            }
            await migrator.MigrateAsync();
            var sections = await db.QueryAsync<SongSection>("select * from public.song_sections order by \"order\"");
            var partitions = await db.QueryAsync<Partition>("select * from public.song_partitions order by \"order\"");
            var cells = await db.QueryAsync<SongContentCell>("select * from public.song_content_cells");
            Assert.Equal(4, sections.Count);
            Assert.Equal(sectionId, sections[0].Id);
            Assert.Equal("Куплет", sections[0].Name);
            var firstRow = cells.Where(x => x.SectionId == sectionId).ToArray();
            Assert.Equal(2, firstRow.Length);
            Assert.Contains(firstRow, x => x.Content == lyrics);
            Assert.Contains(firstRow, x => x.Content == chords);
            Assert.All(firstRow, cell =>
            {
                Assert.Equal(7, cell.Revision);
                Assert.Equal(userId, cell.CreatedBy);
                Assert.Equal(userId, cell.UpdatedBy);
                Assert.NotNull(cell.UpdatedAt);
            });
            Assert.Equal(1, partitions.Count(x => x.Name == "Слова"));
            Assert.Equal(1, partitions.Count(x => x.Name == "Аккорды"));
            Assert.All(cells, cell => Assert.Equal(songId, cell.SongId));
            if (!alreadyAppliedFlatMigration)
            {
                Assert.Equal(["Куплет", "Аккорды", "Дубликат", "Пустая"], sections.Select(x => x.Name));
                Assert.Equal([5, 10, 15, 20], sections.Select(x => x.Order));
                Assert.Equal(2, partitions.Count);
                Assert.Equal(5, cells.Count); // Identical lyrics/chords are still two independent cells.
                Assert.Equal(2, cells.Count(x => x.SectionId == sections[2].Id));
            }
            else
            {
                var drums = Assert.Single(partitions, x => x.Name == "Заметки барабанщика");
                Assert.Equal("Сбивка", Assert.Single(cells, x => x.PartitionId == drums.Id).Content);
                Assert.Equal(6, cells.Count);
            }
            await migrator.MigrateAsync();
            Assert.Equal(cells.Count, await db.SingleOrDefaultAsync<int>("select count(*)::int from public.song_content_cells"));
            await migrator.MigrateAsync("20261019224506_SongPartitions");
            var restored = await db.QueryAsync<string>("select content as \"Value\" from public.song_partitions");
            Assert.All(cells.Where(x => x.Content.Length > 0), cell => Assert.Contains(restored, text => text.Contains(cell.Content)));

        }
        finally
        {
            await using var drop = new NpgsqlCommand($"drop database {database} with (force)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
