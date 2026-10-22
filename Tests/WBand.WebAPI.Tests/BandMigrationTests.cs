using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using WBand.Modules.BandModule;
using WBand.Modules.BandModule.Data;
using WBand.WebAPI.Data;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class BandMigrationTests(DatabaseFixture fixture)
{
    private static readonly string[] BandTables = ["users", "bands", "band_members", "band_departures", "band_invitations"];

    [Fact]
    public Task Migrate_WhenLegacyTablesContainData_PreservesRowsAndDatabaseObjectIdentitiesAcrossRoundTrip() => Isolated(async connection =>
    {
        await using var host = Host(connection);
        await host.Database.MigrateAsync();
        await SeedLegacy(host);
        var before = await Manifest(host, "public", [.. BandTables, "songs"]);
        await BandModule.ApplyMigrationsAsync(connection);
        await BandModule.ApplyMigrationsAsync(connection);
        var after = await Manifest(host, "bands", BandTables);
        Assert.Equal(before[..5], after);
        Assert.Equal(before[5], Assert.Single(await Manifest(host, "public", ["songs"])));
        Assert.Equal(0, await host.SingleOrDefaultAsync<int>("select count(*)::int from pg_tables where schemaname = 'public' and tablename in ('users', 'bands', 'band_members', 'band_departures', 'band_invitations')"));

        await using var bands = Bands(connection);
        await bands.GetService<IMigrator>().MigrateAsync("0");
        Assert.Equal(before, await Manifest(host, "public", [.. BandTables, "songs"]));
        await BandModule.ApplyMigrationsAsync(connection);
        Assert.Equal(after, await Manifest(host, "bands", BandTables));
        Assert.Equal(before[5], Assert.Single(await Manifest(host, "public", ["songs"])));
    });

    [Fact]
    public Task Migrate_WhenDatabaseIsEmpty_CreatesOwnedTablesAndPreservesNewDataOnRollback() => Isolated(async connection =>
    {
        await BandModule.ApplyMigrationsAsync(connection);
        await using var bands = Bands(connection);
        bands.Users.Add(new WBand.Modules.BandModule.Data.User
        {
            KeycloakId = Guid.NewGuid(), Email = "fresh@example.invalid", CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow, LastLoginAt = default,
        });
        await bands.SaveChangesAsync();
        await using var reader = Host(connection);
        var before = await Manifest(reader, "bands", BandTables);
        await bands.GetService<IMigrator>().MigrateAsync("0");
        Assert.Equal(before, await Manifest(reader, "public", BandTables));
        await BandModule.ApplyMigrationsAsync(connection);
        Assert.Equal(before, await Manifest(reader, "bands", BandTables));
        Assert.False(bands.Database.HasPendingModelChanges());
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Migrate_WhenSchemasAreIncompleteOrConflicting_RejectsWithoutChangingExistingData(bool conflicting) => Isolated(async connection =>
    {
        await using var host = Host(connection);
        if (conflicting)
        {
            await host.Database.MigrateAsync();
            await SeedLegacy(host);
            await host.ExecuteAsync("create schema bands; create table bands.users (id uuid primary key); insert into bands.users values (gen_random_uuid())");
        }
        else
        {
            await host.ExecuteAsync("create table public.users (id uuid primary key); insert into public.users values (gen_random_uuid())");
        }
        var tables = conflicting ? BandTables : ["users"];
        var before = await Manifest(host, "public", tables);
        var owned = conflicting ? await Manifest(host, "bands", ["users"]) : [];
        var error = await Assert.ThrowsAsync<PostgresException>(() => BandModule.ApplyMigrationsAsync(connection));
        Assert.Contains("conflicting or incomplete", error.MessageText);
        Assert.Equal(before, await Manifest(host, "public", tables));
        if (conflicting) Assert.Equal(owned, await Manifest(host, "bands", ["users"]));
    });

    [Fact]
    public Task Migrate_WhenIndexNameConflictsAfterSomeTablesMoved_RollsBackTheEntireTransfer() => Isolated(async connection =>
    {
        await using var host = Host(connection);
        await host.Database.MigrateAsync();
        await SeedLegacy(host);
        var before = await Manifest(host, "public", [.. BandTables, "songs"]);
        await host.ExecuteAsync("create schema bands; create table bands.blocker (id uuid constraint \"PK_band_members\" primary key)");
        var error = await Assert.ThrowsAsync<PostgresException>(() => BandModule.ApplyMigrationsAsync(connection));
        Assert.Equal(PostgresErrorCodes.DuplicateTable, error.SqlState);
        Assert.Equal(before, await Manifest(host, "public", [.. BandTables, "songs"]));
        Assert.Equal(0, await host.SingleOrDefaultAsync<int>("select count(*)::int from pg_tables where schemaname = 'bands' and tablename in ('users', 'bands', 'band_members', 'band_departures', 'band_invitations')"));
        await host.ExecuteAsync("drop table bands.blocker");
        await BandModule.ApplyMigrationsAsync(connection);
        Assert.Equal(before[..5], await Manifest(host, "bands", BandTables));
    });

    [Fact]
    public Task Rollback_WhenPublicTableAlreadyExists_RejectsWithoutChangingOwnedData() => Isolated(async connection =>
    {
        await BandModule.ApplyMigrationsAsync(connection);
        await using var host = Host(connection);
        var before = await Manifest(host, "bands", BandTables);
        await host.ExecuteAsync("create table public.band_invitations (id uuid primary key)");
        await using var bands = Bands(connection);
        var error = await Assert.ThrowsAsync<PostgresException>(() => bands.GetService<IMigrator>().MigrateAsync("0"));
        Assert.Contains("public table already exists", error.MessageText);
        Assert.Equal(before, await Manifest(host, "bands", BandTables));
        Assert.Single(await bands.Database.GetAppliedMigrationsAsync());
    });

    private async Task Isolated(Func<string, Task> action)
    {
        var name = "band_migration_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(fixture.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"create database {name}", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            await action(new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name, Pooling = false }.ConnectionString);
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"drop database {name} with (force)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static DataContext Host(string connection) => new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(connection).Options);

    private static BandDataContext Bands(string connection) => new(new DbContextOptionsBuilder<BandDataContext>()
        .UseNpgsql(new NpgsqlConnectionStringBuilder(connection) { SearchPath = "bands,public" }.ConnectionString,
            postgres => postgres.MigrationsHistoryTable("__band_migrations", "bands")).Options);

    private static async Task<string[]> Manifest(DataContext db, string schema, string[] tables)
    {
        var result = new List<string>();
        foreach (var table in tables)
        {
            // Schema/table identifiers are constants from the tests, never external input.
            var value = await db.SingleOrDefaultAsync<string>($"""
                select c.oid::text || ':' || (select coalesce(jsonb_agg(to_jsonb(t) order by id), '[]'::jsonb)::text from {schema}.{table} t)
                    || ':' || coalesce((select string_agg(indexrelid::text, ',' order by indexrelid) from pg_index where indrelid = c.oid), '')
                    || ':' || coalesce((select string_agg(oid::text || '/' || confrelid::text, ',' order by oid) from pg_constraint where conrelid = c.oid), '')
                from pg_class c where c.oid = @relation::regclass
                """, new { relation = $"{schema}.{table}" });
            result.Add(value!);
        }
        return result.ToArray();
    }

    private static Task<int> SeedLegacy(DataContext db) => db.ExecuteAsync("""
        insert into public.users (id, keycloak_id, email, first_name, last_name, created_at, updated_at, last_login_at)
            values (@user, @subject, 'legacy@example.invalid', 'Имя', 'Фамилия', now(), now(), now());
        insert into public.bands (id, name, created_by, created_at) values (@band, 'Группа', @subject, now());
        insert into public.band_members (id, band_id, user_id, email, role, joined_at)
            values (gen_random_uuid(), @band, @subject, 'legacy@example.invalid', 0, now());
        insert into public.band_departures (id, band_id, user_id, email, reason, left_at, rejoined_at)
            values (gen_random_uuid(), @band, @subject, 'legacy@example.invalid', 0, now() - interval '2 days', now() - interval '1 day');
        insert into public.band_invitations (id, band_id, inviter_id, inviter_email, invitee_email, role, status, created_at, expires_at, responded_at)
            select gen_random_uuid(), @band, @subject, 'legacy@example.invalid', 'invite-' || status || '@example.invalid', 1, status,
                now() - interval '1 day', now() + interval '6 days', case when status = 0 then null else now() end
                from generate_series(0, 4) status;
        insert into public.songs (id, band_id, title, authors, status, content_version, created_at, created_by)
            values (gen_random_uuid(), @band, 'Песня', ARRAY['Автор'], 1, 1, now(), @subject);
        """, new { user = Guid.NewGuid(), subject = Guid.NewGuid(), band = Guid.NewGuid() });
}
