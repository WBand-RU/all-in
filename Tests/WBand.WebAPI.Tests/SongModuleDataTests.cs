using Microsoft.EntityFrameworkCore;
using Npgsql;
using WBand.Modules.BandModule.Contracts;
using WBand.Persistence;
using Xunit;
using Band = WBand.Modules.BandModule.Data.Band;
using User = WBand.Modules.BandModule.Data.User;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class SongModuleDataTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Save_WhenSongGraphIsValid_PreservesEmptyCellsRepeatedNamesTextAndMetadataHistory()
    {
        var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var user = new User
        {
            KeycloakId = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.invalid",
            CreatedAt = now, UpdatedAt = now, LastLoginAt = default,
        };
        var band = new Band { Id = Guid.NewGuid(), Name = "Song storage", CreatedBy = user.KeycloakId, CreatedAt = now };
        await using (var bands = fixture.CreateBandSession())
        {
            bands.Users.Add(user);
            bands.Bands.Add(band);
            await bands.SaveChangesAsync();
        }
        var song = new Song
        {
            Id = Guid.NewGuid(), BandId = band.Id, Title = "Песня", Authors = ["Автор"], Key = "Am", Bpm = 120,
            CreatedAt = now, CreatedBy = user.KeycloakId,
        };
        var partition = new Partition
        {
            Id = Guid.NewGuid(), SongId = song.Id, Name = "Слова", CreatedAt = now, CreatedBy = user.KeycloakId,
        };
        var first = new SongSection
        {
            Id = Guid.NewGuid(), SongId = song.Id, Name = "Куплет", CreatedAt = now, CreatedBy = user.KeycloakId,
        };
        var second = new SongSection
        {
            Id = Guid.NewGuid(), SongId = song.Id, Name = "Куплет", Order = 1, CreatedAt = now, CreatedBy = user.KeycloakId,
        };
        const string text = "  [Am]Первая строка\nВторая [G/B]строка  \n";
        var cells = new[] { (first.Id, text), (second.Id, "") }.Select(row => new SongContentCell
        {
            Id = Guid.NewGuid(), SongId = song.Id, SectionId = row.Id, PartitionId = partition.Id,
            Content = row.Item2, CreatedAt = now, CreatedBy = user.KeycloakId,
        }).ToArray();
        var revision = new SongRevision
        {
            Id = Guid.NewGuid(), SongId = song.Id, ContentVersion = song.ContentVersion,
            Title = song.Title, Authors = [.. song.Authors], Key = song.Key, Bpm = song.Bpm,
            Status = song.Status, CreatedAt = now, CreatedBy = user.KeycloakId,
        };
        await using (var writer = fixture.CreateSongSession())
        {
            await using var transaction = await writer.BeginTransactionAsync();
            await writer.InsertAsync(song);
            await writer.InsertAsync(partition);
            await writer.InsertAsync(first);
            await writer.InsertAsync(second);
            foreach (var cell in cells) await writer.InsertAsync(cell);
            await writer.InsertAsync(revision);
            await transaction.CommitAsync();
        }
        await using var reader = fixture.CreateSongSession();
        var loaded = await reader.Songs.Include(x => x.Sections).Include(x => x.Partitions).ThenInclude(x => x.Cells)
            .Include(x => x.Revisions).AsSplitQuery().SingleAsync(x => x.Id == song.Id);
        Assert.Equal(user.KeycloakId, loaded.CreatedBy);
        Assert.Equal(2, loaded.Sections.Count);
        Assert.All(loaded.Sections, section => Assert.Equal("Куплет", section.Name));
        Assert.Equal(new[] { 0, 1 }, loaded.Sections.OrderBy(x => x.Order).Select(x => x.Order));
        var column = Assert.Single(loaded.Partitions);
        Assert.Equal(text, Assert.Single(column.Cells, x => x.SectionId == first.Id).Content);
        Assert.Equal("", Assert.Single(column.Cells, x => x.SectionId == second.Id).Content);
        var history = Assert.Single(loaded.Revisions);
        Assert.Equal(revision.Id, history.Id);
        Assert.Equal(song.Authors, history.Authors);
        Assert.Equal(now, history.CreatedAt);
        var raw = await reader.SingleOrDefaultAsync<SongContentCell>("select * from song_content_cells where id = @id", new { id = cells[0].Id });
        Assert.Equal(text, raw!.Content);

        var otherId = Guid.NewGuid();
        await using (var invalid = fixture.CreateSongSession())
        {
            await using var transaction = await invalid.BeginTransactionAsync();
            var other = new Song { Id = otherId, BandId = band.Id, Title = "Other", CreatedAt = now, CreatedBy = user.KeycloakId };
            await invalid.InsertAsync(other);
            var otherPartition = new Partition
            {
                Id = Guid.NewGuid(), SongId = other.Id, Name = "Other", CreatedAt = now, CreatedBy = user.KeycloakId,
            };
            await invalid.InsertAsync(otherPartition);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => invalid.InsertAsync(new SongContentCell
            {
                Id = Guid.NewGuid(), SongId = other.Id, SectionId = first.Id, PartitionId = otherPartition.Id,
                Content = "Wrong song", CreatedAt = now, CreatedBy = user.KeycloakId,
            }));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        }
        Assert.Equal(2, await reader.SongContentCells.CountAsync(x => x.SongId == song.Id));
        Assert.False(await reader.Songs.AnyAsync(x => x.Id == otherId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BandMutation_WhenSongOrBandContextOwnsLock_BlocksTheOtherUntilRollback(bool ownerIsBand)
    {
        await using var songs = fixture.CreateSongSession();
        await using var bands = fixture.CreateBandSession();
        await using var owner = ownerIsBand ? await bands.BeginTransactionAsync() : await songs.BeginTransactionAsync();
        await using var contenderTransaction = ownerIsBand ? await songs.BeginTransactionAsync() : await bands.BeginTransactionAsync();
        var bandId = Guid.NewGuid();
        if (ownerIsBand) await bands.LockBandAsync(bandId, default);
        else await songs.LockBandAsync(bandId, default);
        RelationalDataContext contender = ownerIsBand ? songs : bands;
        const string query = "select pg_try_advisory_xact_lock(hashtextextended(@resource, 0))";
        var parameters = new { resource = BandMutationLock.ResourceName(bandId) };
        Assert.False(await contender.SingleOrDefaultAsync<bool>(query, parameters));
        await owner.RollbackAsync();
        Assert.True(await contender.SingleOrDefaultAsync<bool>(query, parameters));
    }
}
