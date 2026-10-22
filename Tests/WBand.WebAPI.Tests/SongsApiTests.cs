using BandMemberRole = WBand.Modules.BandModule.Data.BandMemberRole;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WBand.Modules.SongModule.Contracts;
using WBand.WebAPI.Data;
using WBand.WebAPI.Endpoints.Bands;
using WBand.WebAPI.Endpoints.Songs;
using WBand.WebAPI.Shared;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Web API")]
public sealed class SongsApiTests(BandsApiFixture fixture)
{
    [Fact]
    public async Task Song_WhenPublicationIsUnavailable_PersistsMetadataAndHistory()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        fixture.RejectPublications = true;
        try
        {
            var song = await CreateSong(owner, band.Id);
            (await owner.PutAsJsonAsync($"/song-api/songs/{song.Id}",
                Update(song) with { Title = "Updated" })).EnsureSuccessStatusCode();
            var restoredVersion = await owner.PostAsync($"/song-api/songs/{song.Id}/versions/1/restore", null);
            restoredVersion.EnsureSuccessStatusCode();
            var restored = (await restoredVersion.Content.ReadFromJsonAsync<ApiResponse<SongResponse>>())!.Value;
            Assert.Equal(song.Title, restored.Title);
            Assert.Equal(3, restored.ContentVersion);
            (await owner.DeleteAsync($"/song-api/songs/{song.Id}")).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/song-api/songs/{song.Id}")).StatusCode);
            (await owner.PostAsync($"/song-api/songs/{song.Id}/restore", null)).EnsureSuccessStatusCode();
            await using var db = fixture.CreateContext();
            var stored = (await db.FindAsync<Song>(song.Id))!;
            Assert.Equal(song.Title, stored.Title);
            Assert.Equal(5, stored.ContentVersion);
            Assert.Null(stored.DeletedAt);
            Assert.Equal(5, await db.SingleOrDefaultAsync<int>(
                "select count(*)::int from public.song_revisions where song_id = @id", new { id = song.Id }));
        }
        finally
        {
            fixture.RejectPublications = false;
        }
    }

    [Fact]
    public async Task CreateSong_WhenOwnerCreates_SavesMetadataAndRevision()
    {
        var ownerId = Guid.NewGuid();
        using var owner = fixture.CreateClient(ownerId);
        var band = await CreateBand(owner);
        var song = await CreateSong(owner, band.Id, "  Amazing Grace  ", ["John Newton", "john newton", "  Anna  "]);
        Assert.Equal("Amazing Grace", song.Title);
        Assert.Equal(["John Newton", "Anna"], song.Authors);
        Assert.Equal(1, song.ContentVersion);
        await using var db = fixture.CreateContext();
        var stored = await db.FindAsync<Song>(song.Id);
        Assert.NotNull(stored);
        Assert.Equal(ownerId, stored.CreatedBy);
        Assert.Equal(song.Authors, stored.Authors);
        var revision = Assert.Single(await db.QueryAsync<SongRevision>("select * from public.song_revisions where song_id = @id", new { id = song.Id }));
        Assert.Equal(song.Title, revision.Title);
        Assert.Equal(song.Authors, revision.Authors);
        var references = await WBand.WebAPI.Application.Songs.GetSongReferencesHandler.Handle(new GetSongReferences([song.Id]), db, default);
        Assert.Equal(song.Id, Assert.Single(references).SongId);
    }

    [Fact]
    public async Task ListSongs_WhenFiltering_ReturnsOnlyActiveMembershipsAndPagedMatches()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        using var other = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        var otherBand = await CreateBand(other);
        await CreateSong(owner, band.Id, "Alpha");
        var beta = await CreateSong(owner, band.Id, "Beta");
        await CreateSong(other, otherBand.Id, "Alpha secret");
        var page = await owner.GetFromJsonAsync<ApiResponse<TableResponse<SongResponse>>>("/song-api/songs?page=2&pageSize=1");
        Assert.Equal(2, page!.Value.Total);
        Assert.Equal(beta.Id, Assert.Single(page.Value.Data).Id);
        var matches = await owner.GetFromJsonAsync<ApiResponse<TableResponse<SongResponse>>>($"/song-api/songs?bandId={band.Id}&page=1&pageSize=50&searchTerm=ALPH");
        Assert.Equal("Alpha", Assert.Single(matches!.Value.Data).Title);
        var empty = await owner.GetFromJsonAsync<ApiResponse<TableResponse<SongResponse>>>("/song-api/songs?page=1&pageSize=50&searchTerm=%25");
        Assert.Empty(empty!.Value.Data);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/song-api/songs?bandId={otherBand.Id}&page=1&pageSize=50")).StatusCode);
    }

    [Fact]
    public async Task Song_WhenMemberIsReadOnly_AllowsReadAndDeniesEveryWrite()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var memberId = Guid.NewGuid();
        var email = $"{memberId:N}@example.com";
        using var member = fixture.CreateClient(memberId, email);
        using var outsider = fixture.CreateClient(Guid.NewGuid());
        using var anonymous = fixture.CreateClient();
        var band = await CreateBand(owner);
        var song = await CreateSong(owner, band.Id);
        var section = await CreatePartition(owner, song.Id);
        var sharedSectionResponse = await owner.PostAsJsonAsync($"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Куплет", 0));
        sharedSectionResponse.EnsureSuccessStatusCode();
        var sharedSection = (await sharedSectionResponse.Content.ReadFromJsonAsync<SongSectionResponse>())!;
        var invite = await owner.PostAsJsonAsync($"/band-api/bands/{band.Id}/invitations", new CreateInvitationRequest(email, BandMemberRole.Member));
        invite.EnsureSuccessStatusCode();
        var invitation = (await invite.Content.ReadFromJsonAsync<ApiResponse<BandInvitation>>())!.Value;
        (await member.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/song-api/songs/{song.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/song-api/songs/{song.Id}/partitions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/song-api/songs/{song.Id}/versions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/song-api/songs/{song.Id}/content")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/song-api/songs/{song.Id}/sections")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync($"/song-api/songs/{song.Id}/sections", new CreateSongSectionRequest("Denied", 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/song-api/sections/{sharedSection.Id}", new UpdateSongSectionRequest("Denied", 0, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync($"/song-api/sections/{sharedSection.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/song-api/songs/{song.Id}/sections/{sharedSection.Id}/partitions/{section.Id}", new UpdateSongContentCellRequest("Denied", 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/song-api/songs/{song.Id}/partitions/{section.Id}/content", new UpdatePartitionContentRequest([new(sharedSection.Id, "Denied", 0)]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync("/song-api/songs", new CreateSongRequest(band.Id, "Denied"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/song-api/songs/{song.Id}", Update(song))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync($"/song-api/songs/{song.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync($"/song-api/songs/{song.Id}/partitions", new CreatePartitionRequest("Denied", 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/song-api/partitions/{section.Id}", new UpdatePartitionRequest("Denied", 0, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync($"/song-api/partitions/{section.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsync($"/song-api/songs/{song.Id}/versions/1/restore", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/song-api/songs/{song.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/song-api/songs/{song.Id}/partitions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/song-api/songs/{song.Id}")).StatusCode);
        (await owner.DeleteAsync($"/song-api/songs/{song.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync($"/song-api/songs/trash?bandId={band.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsync($"/song-api/songs/{song.Id}/restore", null)).StatusCode);
    }

    [Fact]
    public async Task Song_WhenAdminEdits_AllowsMetadataAndPartitionChanges()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var adminId = Guid.NewGuid();
        var email = $"{adminId:N}@example.com";
        using var admin = fixture.CreateClient(adminId, email);
        var band = await CreateBand(owner);
        var response = await owner.PostAsJsonAsync($"/band-api/bands/{band.Id}/invitations", new CreateInvitationRequest(email, BandMemberRole.Admin));
        response.EnsureSuccessStatusCode();
        var invitation = (await response.Content.ReadFromJsonAsync<ApiResponse<BandInvitation>>())!.Value;
        (await admin.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).EnsureSuccessStatusCode();
        var song = await CreateSong(admin, band.Id);
        (await admin.PutAsJsonAsync($"/song-api/songs/{song.Id}", Update(song))).EnsureSuccessStatusCode();
        await CreatePartition(admin, song.Id);
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData("   ", null, null)]
    [InlineData("Valid", "H", 120)]
    [InlineData("Valid", "C", 401)]
    [InlineData("Valid", "C", 19)]
    public async Task CreateSong_WhenMetadataIsInvalid_RejectsRequest(string title, string? key, int? bpm)
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/song-api/songs", new CreateSongRequest(band.Id, title, Key: key, Bpm: bpm))).StatusCode);
    }

    [Fact]
    public async Task UpdateSong_WhenTwoEditorsUseSameVersion_OnlyOneSucceeds()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner, (await CreateBand(owner)).Id);
        var results = await Task.WhenAll(owner.PutAsJsonAsync($"/song-api/songs/{song.Id}", Update(song) with { Title = "First" }),
            owner.PutAsJsonAsync($"/song-api/songs/{song.Id}", Update(song) with { Title = "Second" }));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.CreateContext();
        Assert.Equal(2, (await db.FindAsync<Song>(song.Id))!.ContentVersion);
        Assert.Equal(2, await db.SingleOrDefaultAsync<int>("select count(*)::int from public.song_revisions where song_id = @id", new { id = song.Id }));
    }

    [Fact]
    public async Task Partitions_WhenEditedAndDeleted_PreservePlainTextAndDisplayOrder()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner, (await CreateBand(owner)).Id);
        var chorus = await CreatePartition(owner, song.Id, "Chorus", 10);
        var verse = await CreatePartition(owner, song.Id, "Verse", 0);
        var response = await owner.PutAsJsonAsync($"/song-api/partitions/{verse.Id}", new UpdatePartitionRequest("Drum notes", 0, 1));
        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<PartitionResponse>())!;
        Assert.Equal("Drum notes", updated.Name);
        Assert.Equal(2, updated.Revision);
        var sections = (await owner.GetFromJsonAsync<PartitionResponse[]>($"/song-api/songs/{song.Id}/partitions"))!;
        Assert.Equal([verse.Id, chorus.Id], sections.Select(x => x.Id));
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/song-api/partitions/{verse.Id}")).StatusCode);
        Assert.Equal(chorus.Id, Assert.Single((await owner.GetFromJsonAsync<PartitionResponse[]>($"/song-api/songs/{song.Id}/partitions"))!).Id);
        await using var db = fixture.CreateContext();
    }

    [Fact]
    public async Task CreatePartition_WhenNameIsCustom_CreatesEmptyIndependentlyEditablePart()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner, (await CreateBand(owner)).Id);
        var response = await owner.PostAsJsonAsync($"/song-api/songs/{song.Id}/partitions",
            new CreatePartitionRequest("  Заметки клавишника  ", 0));
        response.EnsureSuccessStatusCode();
        var partition = (await response.Content.ReadFromJsonAsync<PartitionResponse>())!;
        Assert.Equal("Заметки клавишника", partition.Name);
        Assert.Equal(1, partition.Revision);
        Assert.Equal(song.Id, partition.SongId);
        var retained = Assert.Single((await owner.GetFromJsonAsync<PartitionResponse[]>($"/song-api/songs/{song.Id}/partitions"))!);
        Assert.Equal(partition.Id, retained.Id);
        Assert.Equal(partition.Name, retained.Name);
        Assert.Equal(partition.Revision, retained.Revision);
    }

    [Fact]
    public async Task UpdatePartition_WhenTwoEditorsUseSameRevision_OnlyOneSucceeds()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner, (await CreateBand(owner)).Id);
        var section = await CreatePartition(owner, song.Id);
        var first = new UpdatePartitionRequest("Verse", 0, 1);
        var results = await Task.WhenAll(owner.PutAsJsonAsync($"/song-api/partitions/{section.Id}", first),
            owner.PutAsJsonAsync($"/song-api/partitions/{section.Id}", first with { Name = "Second" }));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.CreateContext();
        Assert.Equal(2, (await db.FindAsync<Partition>(section.Id))!.Revision);
    }

    [Fact]
    public async Task CreatePartition_WhenNameOrderOrTextIsInvalid_RejectsRequest()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner, (await CreateBand(owner)).Id);
        foreach (var input in new[] { new CreatePartitionRequest(" ", 0), new CreatePartitionRequest("Verse", -1),
            new CreatePartitionRequest(new string('a', 101), 0) })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/song-api/songs/{song.Id}/partitions", input)).StatusCode);
    }

    [Fact]
    public async Task DeletedSong_WhenRestored_RetainsPartitionsAndRecordsVersions()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        var song = await CreateSong(owner, band.Id);
        var section = await CreatePartition(owner, song.Id);
        (await owner.DeleteAsync($"/song-api/songs/{song.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/song-api/songs/{song.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/song-api/songs/{song.Id}/partitions")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PutAsJsonAsync($"/song-api/partitions/{section.Id}", new UpdatePartitionRequest("Denied", 0, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.DeleteAsync($"/song-api/partitions/{section.Id}")).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<ApiResponse<TableResponse<SongResponse>>>("/song-api/songs?page=1&pageSize=50"))!.Value.Data);
        Assert.Equal(song.Id, Assert.Single((await owner.GetFromJsonAsync<ApiResponse<SongResponse[]>>($"/song-api/songs/trash?bandId={band.Id}"))!.Value).Id);
        var restore = await owner.PostAsync($"/song-api/songs/{song.Id}/restore", null);
        restore.EnsureSuccessStatusCode();
        Assert.Equal(3, (await restore.Content.ReadFromJsonAsync<ApiResponse<SongResponse>>())!.Value.ContentVersion);
        Assert.Equal(section.Id, Assert.Single((await owner.GetFromJsonAsync<PartitionResponse[]>($"/song-api/songs/{song.Id}/partitions"))!).Id);
        await using var db = fixture.CreateContext();
        await owner.DeleteAsync($"/song-api/songs/{song.Id}");
        await db.ExecuteAsync("update public.songs set deleted_at = now() - interval '31 days' where id = @id", new { id = song.Id });
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsync($"/song-api/songs/{song.Id}/restore", null)).StatusCode);
    }

    [Fact]
    public async Task Versions_WhenRestoringMetadata_CreatesNewVersionWithoutChangingPartitionContent()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var song = await CreateSong(owner, (await CreateBand(owner)).Id, "Original");
        var section = await CreatePartition(owner, song.Id);
        (await owner.PutAsJsonAsync($"/song-api/songs/{song.Id}", Update(song) with { Title = "Edited", Bpm = 140 })).EnsureSuccessStatusCode();
        var versions = (await owner.GetFromJsonAsync<ApiResponse<SongVersionResponse[]>>($"/song-api/songs/{song.Id}/versions"))!.Value;
        Assert.Equal([2L, 1L], versions.Select(x => x.ContentVersion));
        Assert.All(versions, x => Assert.Equal("Owner", x.ChangedByRole));
        var details = (await owner.GetFromJsonAsync<ApiResponse<SongVersionDetailsResponse>>($"/song-api/songs/{song.Id}/versions/1"))!.Value;
        Assert.Equal("Original", details.Title);
        var response = await owner.PostAsync($"/song-api/songs/{song.Id}/versions/1/restore", null);
        response.EnsureSuccessStatusCode();
        var restored = (await response.Content.ReadFromJsonAsync<ApiResponse<SongResponse>>())!.Value;
        Assert.Equal("Original", restored.Title);
        Assert.Equal(3, restored.ContentVersion);
        var retained = Assert.Single((await owner.GetFromJsonAsync<PartitionResponse[]>($"/song-api/songs/{song.Id}/partitions"))!);
        Assert.Equal(section.Id, retained.Id);
        Assert.Equal(section.Revision, retained.Revision);
    }

    [Fact]
    public async Task CreateSong_WhenRevisionWriteFails_RollsBackSongAndRevision()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        await using var db = fixture.CreateContext();
        // A per-band trigger fails after the song insert, exercising the real request transaction.
        await db.ExecuteAsync($"""
            create function fail_song_revision_{band.Id:N}() returns trigger language plpgsql as $$
            begin
                if exists(select 1 from public.songs where id = new.song_id and band_id = '{band.Id}') then
                    raise exception 'Injected revision write failure';
                end if;
                return new;
            end $$;
            create trigger test_revision_failure_{band.Id:N} before insert on public.song_revisions
                for each row execute function fail_song_revision_{band.Id:N}();
            """);
        var revisionCount = await db.SingleOrDefaultAsync<int>("select count(*)::int from public.song_revisions");
        try
        {
            Assert.Equal(HttpStatusCode.InternalServerError, (await owner.PostAsJsonAsync("/song-api/songs", new CreateSongRequest(band.Id, "Failed"))).StatusCode);
            Assert.Equal(0, await db.SingleOrDefaultAsync<int>("select count(*)::int from public.songs where band_id = @id", new { id = band.Id }));
            Assert.Equal(revisionCount, await db.SingleOrDefaultAsync<int>("select count(*)::int from public.song_revisions"));
        }
        finally
        {
            await db.ExecuteAsync($"drop trigger test_revision_failure_{band.Id:N} on public.song_revisions; drop function fail_song_revision_{band.Id:N}()");
        }
    }

    [Fact]
    public async Task OpenApi_WhenSongsAreDiscovered_DescribesPartitionsWithOneContentField()
    {
        using var client = fixture.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/song-api/songs", out _));
        Assert.True(paths.TryGetProperty("/song-api/songs/{songId}/partitions", out _));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var song = schemas.GetProperty("SongResponse").GetProperty("properties");
        Assert.False(song.TryGetProperty("tempoTrack", out _));
        Assert.False(song.TryGetProperty("timeSignatureTrack", out _));
        Assert.False(song.TryGetProperty("countInBars", out _));
        var section = schemas.GetProperty("PartitionResponse").GetProperty("properties");
        Assert.False(section.TryGetProperty("barCount", out _));
        Assert.False(section.TryGetProperty("content", out _));
        Assert.True(paths.TryGetProperty("/song-api/songs/{songId}/content", out _));
        Assert.True(schemas.GetProperty("SongContentCellResponse").GetProperty("properties").TryGetProperty("content", out _));
        Assert.False(section.TryGetProperty("lyrics", out _));
        Assert.False(section.TryGetProperty("chords", out _));
        if (Environment.GetEnvironmentVariable("WEBAPI_OPENAPI_EXPORT") is { Length: > 0 } path)
            await File.WriteAllTextAsync(path, json);
    }

    private static async Task<Band> CreateBand(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/band-api/bands", new CreateBandRequest("Songs " + Guid.NewGuid().ToString("N")));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<Band>>())!.Value;
    }

    private static async Task<SongResponse> CreateSong(HttpClient client, Guid bandId, string title = "Song", string[]? authors = null)
    {
        var response = await client.PostAsJsonAsync("/song-api/songs", new CreateSongRequest(bandId, title, authors, Key: "Am", Bpm: 120));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<SongResponse>>())!.Value;
    }

    private static async Task<PartitionResponse> CreatePartition(HttpClient client, Guid songId, string name = "Verse", int order = 0)
    {
        var response = await client.PostAsJsonAsync($"/song-api/songs/{songId}/partitions", new CreatePartitionRequest(name, order));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PartitionResponse>())!;
    }

    private static UpdateSongRequest Update(SongResponse song) => new(song.Title, song.Authors, null, song.Key, song.Bpm, song.Status, song.ContentVersion);

}
