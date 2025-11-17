namespace BandService.Endpoints;

using BandService.Endpoints.Bands;

public static class EndpointDependencyInjection
{
    public static WebApplication MapEndpoints(this WebApplication application)
    {
        new BandMapper().Register(application);
        return application;
    }
}
