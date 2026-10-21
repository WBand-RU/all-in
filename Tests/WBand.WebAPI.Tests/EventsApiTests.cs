using System.Net;
using System.Net.Http.Json;
using WBand.Modules.EventModule.Endpoints;
using WBand.WebAPI.Data;
using WBand.WebAPI.Endpoints.Bands;
using WBand.WebAPI.Endpoints.Songs;
using WBand.WebAPI.Shared;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Web API")]
public sealed class EventsApiTests(BandsApiFixture fixture)
{
    [Fact]
    public async Task Event_WhenOwnerSaves_PersistsOrderDetectsStaleEditsAndDeletesWithoutDeletingSongs()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await Band(owner);
        var first = await Song(owner, band.Id, "First");
        var second = await Song(owner, band.Id, "Second");
        var request = Request(band.Id, [second.Id, first.Id]);
        var created = await owner.PostAsJsonAsync("/api/events", request);
        created.EnsureSuccessStatusCode();
        var item = (await created.Content.ReadFromJsonAsync<EventResponse>())!;
        Assert.Equal(request.SongIds, item.SongIds);
        var loaded = await owner.GetFromJsonAsync<EventResponse>($"/api/events/{item.Id}");
        Assert.Equal(item.Id, loaded!.Id);
        Assert.Equal(item.Title, loaded.Title);
        Assert.Equal(item.SongIds, loaded.SongIds);
        Assert.True((item.StartsAt - loaded.StartsAt).Duration() < TimeSpan.FromMilliseconds(1));
        var changed = request with { SongIds = [first.Id, second.Id], ExpectedVersion = item.Version };
        (await owner.PutAsJsonAsync($"/api/events/{item.Id}", changed)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/api/events/{item.Id}", changed)).StatusCode);
        Assert.Equal([first.Id, second.Id], (await owner.GetFromJsonAsync<EventResponse>($"/api/events/{item.Id}"))!.SongIds);
        (await owner.DeleteAsync($"/api/events/{item.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/events/{item.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/song-api/songs/{first.Id}")).StatusCode);
    }

    [Fact]
    public async Task Event_WhenMemberOrOutsiderRequests_EnforcesPermissionsAndFiltersLists()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var memberId = Guid.NewGuid();
        var email = $"{memberId:N}@example.com";
        using var member = fixture.CreateClient(memberId, email);
        using var outsider = fixture.CreateClient(Guid.NewGuid());
        using var anonymous = fixture.CreateClient();
        var band = await Band(owner);
        var inviteResponse = await owner.PostAsJsonAsync($"/band-api/bands/{band.Id}/invitations", new CreateInvitationRequest(email, BandMemberRole.Member));
        var invite = (await inviteResponse.Content.ReadFromJsonAsync<ApiResponse<BandInvitation>>())!.Value;
        (await member.PutAsJsonAsync($"/band-api/user/invitations/{invite.Id}", new RespondInvitationRequest(true))).EnsureSuccessStatusCode();
        var response = await owner.PostAsJsonAsync("/api/events", Request(band.Id, []));
        var item = (await response.Content.ReadFromJsonAsync<EventResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/api/events/{item.Id}")).StatusCode);
        Assert.Contains((await member.GetFromJsonAsync<EventResponse[]>("/api/events"))!, x => x.Id == item.Id);
        Assert.Empty((await outsider.GetFromJsonAsync<EventResponse[]>("/api/events"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/events/{item.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync("/api/events", Request(band.Id, []))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/api/events/{item.Id}", Request(band.Id, []) with { ExpectedVersion = item.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync($"/api/events/{item.Id}")).StatusCode);
    }

    [Fact]
    public async Task Create_WhenSongsAreForeignDeletedRepeatedOrMissing_RejectsSetlist()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await Band(owner);
        var otherBand = await Band(owner);
        var foreign = await Song(owner, otherBand.Id, "Other band");
        var song = await Song(owner, band.Id, "This band");
        foreach (var ids in new[] { new[] { foreign.Id }, new[] { Guid.NewGuid() }, new[] { song.Id, song.Id } })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/events", Request(band.Id, ids))).StatusCode);
        (await owner.DeleteAsync($"/song-api/songs/{song.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/events", Request(band.Id, [song.Id]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/events", Request(band.Id, []) with { Title = "  " })).StatusCode);
    }

    private static SaveEventRequest Request(Guid bandId, Guid[] songs) =>
        new(bandId, "Концерт", DateTimeOffset.UtcNow.AddDays(7), "Зал", "Описание", songs, null);
    private static async Task<Band> Band(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/band-api/bands", new CreateBandRequest("Events test"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<Band>>())!.Value;
    }
    private static async Task<SongResponse> Song(HttpClient client, Guid bandId, string title)
    {
        var response = await client.PostAsJsonAsync("/song-api/songs", new CreateSongRequest(bandId, title));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<SongResponse>>())!.Value;
    }
}
