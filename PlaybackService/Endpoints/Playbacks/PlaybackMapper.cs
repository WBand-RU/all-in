using PlaybackService.Endpoints.Playbacks.UploadPlayback;

namespace PlaybackService.Endpoints.Playbacks;

/// <summary>
/// Mapper for Playback endpoints registration
/// </summary>
public sealed class PlaybackMapper
{
    public void Register(WebApplication app)
    {
        UploadPlaybackEndpoint.Build(app);
        // Additional endpoints can be added here
    }
}
