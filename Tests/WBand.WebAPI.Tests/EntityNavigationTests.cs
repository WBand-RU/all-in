using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WBand.WebAPI.Data;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class EntityNavigationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Relationships_WhenIncluded_LoadReferencesAndInverseCollectionsUsingKeycloakIds()
    {
        var now = DateTimeOffset.UtcNow;
        var creator = CreateUser(now);
        var editor = CreateUser(now);
        var band = new Band
        {
            Id = Guid.CreateVersion7(), Name = "Navigation test", CreatedBy = creator.KeycloakId,
            CreatedByUser = creator, CreatedAt = now,
        };
        band.Members.Add(new BandMember
        {
            Id = Guid.CreateVersion7(), BandId = band.Id, UserId = editor.KeycloakId,
            User = editor, Email = editor.Email, Role = BandMemberRole.Member, JoinedAt = now,
        });
        band.Invitations.Add(new BandInvitation
        {
            Id = Guid.CreateVersion7(), BandId = band.Id, InviterId = creator.KeycloakId,
            Inviter = creator, InviterEmail = creator.Email, InviteeEmail = "unregistered@example.com",
            CreatedAt = now, ExpiresAt = now.AddDays(7),
        });
        band.Departures.Add(new BandDeparture
        {
            Id = Guid.CreateVersion7(), BandId = band.Id, UserId = editor.KeycloakId,
            User = editor, Email = editor.Email, Reason = BandDepartureReason.Voluntary, LeftAt = now,
        });
        var song = new Song
        {
            Id = Guid.CreateVersion7(), BandId = band.Id, Title = "Song", CreatedAt = now,
            CreatedBy = creator.KeycloakId, CreatedByUser = creator,
            UpdatedBy = editor.KeycloakId, UpdatedByUser = editor, UpdatedAt = now,
            DeletedBy = editor.KeycloakId, DeletedByUser = editor, DeletedAt = now,
        };
        song.Sections.Add(new SongSection
        {
            Id = Guid.CreateVersion7(), SongId = song.Id, Name = "Verse", CreatedAt = now,
            CreatedBy = creator.KeycloakId, CreatedByUser = creator,
            UpdatedBy = editor.KeycloakId, UpdatedByUser = editor, UpdatedAt = now,
        });
        song.Revisions.Add(new SongRevision
        {
            Id = Guid.CreateVersion7(), SongId = song.Id, Title = song.Title, ContentVersion = 1,
            CreatedAt = now, CreatedBy = creator.KeycloakId, CreatedByUser = creator,
        });
        band.Songs.Add(song);

        await using (var writer = fixture.CreateSession())
        {
            writer.Bands.Add(band);
            await writer.SaveChangesAsync();
        }
        await using var reader = fixture.CreateSession();
        var loaded = await reader.Bands
            .Include(x => x.CreatedByUser)
            .Include(x => x.Members).ThenInclude(x => x.User)
            .Include(x => x.Invitations).ThenInclude(x => x.Inviter)
            .Include(x => x.Departures).ThenInclude(x => x.User)
            .Include(x => x.Songs).ThenInclude(x => x.CreatedByUser)
            .Include(x => x.Songs).ThenInclude(x => x.UpdatedByUser)
            .Include(x => x.Songs).ThenInclude(x => x.DeletedByUser)
            .Include(x => x.Songs).ThenInclude(x => x.Sections).ThenInclude(x => x.CreatedByUser)
            .Include(x => x.Songs).ThenInclude(x => x.Sections).ThenInclude(x => x.UpdatedByUser)
            .Include(x => x.Songs).ThenInclude(x => x.Revisions).ThenInclude(x => x.CreatedByUser)
            .AsSplitQuery().SingleAsync(x => x.Id == band.Id);

        var loadedCreator = loaded.CreatedByUser;
        var loadedMember = Assert.Single(loaded.Members);
        var loadedEditor = loadedMember.User;
        var loadedSong = Assert.Single(loaded.Songs);
        var loadedSection = Assert.Single(loadedSong.Sections);
        var loadedRevision = Assert.Single(loadedSong.Revisions);
        Assert.Equal(creator.Id, loadedCreator.Id);
        Assert.NotEqual(loadedCreator.Id, loaded.CreatedBy);
        Assert.Equal(editor.Id, loadedEditor.Id);
        Assert.Same(loaded, loadedMember.Band);
        Assert.Same(loaded, Assert.Single(loaded.Invitations).Band);
        Assert.Same(loadedCreator, Assert.Single(loaded.Invitations).Inviter);
        Assert.Same(loaded, Assert.Single(loaded.Departures).Band);
        Assert.Same(loadedEditor, Assert.Single(loaded.Departures).User);
        Assert.Same(loaded, loadedSong.Band);
        Assert.Same(loadedCreator, loadedSong.CreatedByUser);
        Assert.Same(loadedEditor, loadedSong.UpdatedByUser);
        Assert.Same(loadedEditor, loadedSong.DeletedByUser);
        Assert.Same(loadedSong, loadedSection.Song);
        Assert.Same(loadedCreator, loadedSection.CreatedByUser);
        Assert.Same(loadedEditor, loadedSection.UpdatedByUser);
        Assert.Same(loadedSong, loadedRevision.Song);
        Assert.Same(loadedCreator, loadedRevision.CreatedByUser);
        Assert.Same(loaded, Assert.Single(loadedCreator.CreatedBands));
        Assert.Same(loadedMember, Assert.Single(loadedEditor.BandMemberships));
        Assert.Same(Assert.Single(loaded.Invitations), Assert.Single(loadedCreator.SentBandInvitations));
        Assert.Same(Assert.Single(loaded.Departures), Assert.Single(loadedEditor.BandDepartures));
        Assert.Same(loadedSong, Assert.Single(loadedCreator.CreatedSongs));
        Assert.Same(loadedSong, Assert.Single(loadedEditor.UpdatedSongs));
        Assert.Same(loadedSong, Assert.Single(loadedEditor.DeletedSongs));
        Assert.Same(loadedSection, Assert.Single(loadedCreator.CreatedSongSections));
        Assert.Same(loadedSection, Assert.Single(loadedEditor.UpdatedSongSections));
        Assert.Same(loadedRevision, Assert.Single(loadedCreator.CreatedSongRevisions));

        // Entities returned by existing HTTP endpoints retain their scalar JSON contracts.
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(loaded));
        Assert.True(json.RootElement.TryGetProperty(nameof(Band.CreatedBy), out _));
        Assert.False(json.RootElement.TryGetProperty(nameof(Band.CreatedByUser), out _));
        Assert.False(json.RootElement.TryGetProperty(nameof(Band.Members), out _));
        Assert.False(json.RootElement.TryGetProperty(nameof(Band.Songs), out _));
    }

    private static User CreateUser(DateTimeOffset now) => new()
    {
        KeycloakId = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@example.com",
        CreatedAt = now, UpdatedAt = now, LastLoginAt = now,
    };
}
