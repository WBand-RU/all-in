using PlaybackService.Endpoints.Tracks.Download;
using PlaybackService.Endpoints.Tracks.GetTracks;
using PlaybackService.Endpoints.Tracks.Upload;

namespace PlaybackService.Endpoints.Tracks;

/// <summary>
/// Mapper for Track endpoints registration
/// </summary>
public sealed class TrackMapper
{
    public void Register(IEndpointRouteBuilder builder)
    {
        // Group under /songs for nested track routes
        var songsGroup = builder
            .MapGroup("songs/{songId}/tracks")
            .WithTags("Tracks")
            .WithDisplayName("Song Tracks")
            .RequireAuthorization();

        UploadTrackEndpoint.Build(songsGroup);
        GetTracksEndpoint.Build(songsGroup);
        DownloadTrackEndpoint.Build(songsGroup);
    }
}
