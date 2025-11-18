using PlaybackService.Endpoints.Playbacks;

namespace PlaybackService.Endpoints;

public static class EndpointDependencyInjection
{
    public static WebApplication MapEndpoints(this WebApplication application)
    {
        new PlaybackMapper().Register(application);
        return application;
    }
}
