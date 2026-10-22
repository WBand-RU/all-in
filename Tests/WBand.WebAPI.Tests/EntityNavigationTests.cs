using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WBand.WebAPI.Data;
using Band = WBand.Modules.BandModule.Data.Band;
using User = WBand.Modules.BandModule.Data.User;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class EntityNavigationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Relationships_WhenBandIsOwnedByAnotherContext_LoadSongGraphAndPreserveDatabaseReferences()
    {
        var now = DateTimeOffset.UtcNow;
        var creator = new User
        {
            KeycloakId = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.com",
            CreatedAt = now, UpdatedAt = now, LastLoginAt = now,
        };
        var band = new Band
        {
            Id = Guid.CreateVersion7(), Name = "Navigation test", CreatedBy = creator.KeycloakId, CreatedAt = now,
        };
        await using (var bands = fixture.CreateBandSession())
        {
            bands.Users.Add(creator);
            bands.Bands.Add(band);
            await bands.SaveChangesAsync();
        }
        var song = new Song
        {
            Id = Guid.CreateVersion7(), BandId = band.Id, Title = "Song", CreatedAt = now,
            CreatedBy = creator.KeycloakId,
        };
        song.Partitions.Add(new Partition
        {
            Id = Guid.CreateVersion7(), SongId = song.Id, Name = "Слова", CreatedAt = now,
            CreatedBy = creator.KeycloakId,
        });
        song.Revisions.Add(new SongRevision
        {
            Id = Guid.CreateVersion7(), SongId = song.Id, Title = song.Title, ContentVersion = 1,
            CreatedAt = now, CreatedBy = creator.KeycloakId,
        });
        await using (var writer = fixture.CreateSongSession())
        {
            writer.Songs.Add(song);
            await writer.SaveChangesAsync();
        }
        await using var reader = fixture.CreateSongSession();
        var loaded = await reader.Songs.Include(x => x.Partitions).Include(x => x.Revisions)
            .AsSplitQuery().SingleAsync(x => x.Id == song.Id);
        Assert.Equal(band.Id, loaded.BandId);
        Assert.Equal(creator.KeycloakId, loaded.CreatedBy);
        Assert.NotEqual(creator.Id, loaded.CreatedBy);
        Assert.Same(loaded, Assert.Single(loaded.Partitions).Song);
        Assert.Same(loaded, Assert.Single(loaded.Revisions).Song);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(loaded));
        Assert.True(json.RootElement.TryGetProperty(nameof(Song.CreatedBy), out _));
        Assert.False(json.RootElement.TryGetProperty("CreatedByUser", out _));
        Assert.False(json.RootElement.TryGetProperty(nameof(Song.Partitions), out _));

        await using var invalid = fixture.CreateSongSession();
        invalid.Songs.Add(new Song
        {
            Id = Guid.CreateVersion7(), BandId = Guid.NewGuid(), Title = "Missing band",
            CreatedAt = now, CreatedBy = creator.KeycloakId,
        });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => invalid.SaveChangesAsync());
        Assert.Equal(Npgsql.PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<Npgsql.PostgresException>(error.InnerException).SqlState);
    }
}
