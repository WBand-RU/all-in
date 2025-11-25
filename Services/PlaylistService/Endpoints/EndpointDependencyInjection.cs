using PlaylistService.Endpoints.Playlists;

namespace PlaylistService.Endpoints;

public static class EndpointDependencyInjection
{
    public static WebApplication MapEndpoints(this WebApplication application)
    {
        new PlaylistMapper().Register(application);
        return application;
    }
}
