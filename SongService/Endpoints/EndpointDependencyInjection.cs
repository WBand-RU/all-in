using Microsoft.AspNetCore.Builder;

namespace SongService.Endpoints;

public static class EndpointDependencyInjection
{
    public static WebApplication MapEndpoints(this WebApplication application)
    {
        return application;
    }
}
