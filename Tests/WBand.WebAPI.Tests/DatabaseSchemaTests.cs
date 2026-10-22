using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class DatabaseSchemaTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Migrations_WhenRunTwice_CreateAllTablesInPublic()
    {
        await using var db = fixture.CreateSession();
        var tables = await db.QueryAsync<string>("select table_name from information_schema.tables where table_schema = 'public'");
        foreach (var table in new[] { "users", "bands", "band_members", "band_invitations", "band_departures", "songs", "song_partitions", "song_sections", "song_content_cells", "song_revisions" })
            Assert.Contains(table, tables);
        Assert.DoesNotContain("outbox", tables);
        Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
    }

    [Theory]
    [InlineData("bands", "created_by")]
    [InlineData("band_members", "user_id")]
    [InlineData("band_invitations", "inviter_id")]
    [InlineData("band_departures", "user_id")]
    [InlineData("songs", "created_by")]
    [InlineData("songs", "updated_by")]
    [InlineData("songs", "deleted_by")]
    [InlineData("song_partitions", "created_by")]
    [InlineData("song_partitions", "updated_by")]
    [InlineData("song_sections", "created_by")]
    [InlineData("song_sections", "updated_by")]
    [InlineData("song_content_cells", "created_by")]
    [InlineData("song_content_cells", "updated_by")]
    [InlineData("song_revisions", "created_by")]
    public async Task Schema_WhenEntityReferencesUser_HasForeignKeyToKeycloakId(string table, string column)
    {
        await using var db = fixture.CreateSession();
        var definition = await db.SingleOrDefaultAsync<string>("""
            select pg_get_constraintdef(c.oid) from pg_constraint c
            join pg_class t on t.oid = c.conrelid
            where c.contype = 'f' and t.relname = @table
            and pg_get_constraintdef(c.oid) like '%' || @column || ') REFERENCES users(keycloak_id)%'
            """, new { table, column });
        Assert.NotNull(definition);
        Assert.Contains("ON DELETE RESTRICT", definition);
    }
}
