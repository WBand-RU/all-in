using PlaybackService.Endpoints.MixerPresets;
using PlaybackService.Endpoints.Tracks;

namespace PlaybackService.Endpoints;

public static class EndpointDependencyInjection
{
    public static WebApplication MapEndpoints(this WebApplication application)
    {
        new TrackMapper().Register(application);
        new MixerPresetsMapper().Register(application);

        return application;
    }
}
