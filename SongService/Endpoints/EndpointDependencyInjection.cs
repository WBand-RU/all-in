using Microsoft.AspNetCore.Builder;
using SongService.Endpoints.Songs;

namespace SongService.Endpoints;

public static class EndpointDependencyInjection
{
    public static WebApplication MapEndpoints(this WebApplication application)
    {
        new SongMapper().Register(application);
        return application;
    }
}
