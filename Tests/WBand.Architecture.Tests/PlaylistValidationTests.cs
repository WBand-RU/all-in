using System.Text.Json;
using WBand.Modules.PlaylistModule.Domain;
using WBand.Modules.PlaylistModule.Endpoints;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class PlaylistValidationTests
{
    [Theory]
    [InlineData("AutoStart", PlaylistTransitionType.AutoStart)]
    [InlineData("Pause", PlaylistTransitionType.Pause)]
    [InlineData("Crossfade", PlaylistTransitionType.Crossfade)]
    public void CreatePlaylist_DeserializesTransitionNames(
        string transition,
        PlaylistTransitionType expected)
    {
        var json = $$"""
            {
              "bandId": "{{Guid.NewGuid()}}",
              "title": "Concert",
              "items": [{
                "songId": "{{Guid.NewGuid()}}",
                "transition": "{{transition}}"
              }]
            }
            """;

        var request = JsonSerializer.Deserialize<CreatePlaylistRequest>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(expected, request!.Items![0].Transition);
    }

    [Fact]
    public void CreatePlaylist_RejectsInvalidSongOverrides()
    {
        var request = new CreatePlaylistRequest(
            Guid.NewGuid(), "Concert", null, DateTimeOffset.UtcNow, null,
            [new PlaylistItemRequest(null, Guid.NewGuid(), "G", 500, null, null,
                PlaylistTransitionType.Crossfade, CrossfadeSeconds: 90)]);

        var result = new CreatePlaylistRequestValidator().Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreatePlaylist_AcceptsIndependentOverrides()
    {
        var request = new CreatePlaylistRequest(
            Guid.NewGuid(), "Concert", "Main set", DateTimeOffset.UtcNow, "Club",
            [new PlaylistItemRequest(null, Guid.NewGuid(), "F#m", 128,
                [new PlaylistSectionOverride("Chorus", 9, 16)], null,
                PlaylistTransitionType.Pause, PauseSeconds: 3)]);

        var result = new CreatePlaylistRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }
}
