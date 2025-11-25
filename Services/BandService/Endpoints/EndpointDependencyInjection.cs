namespace BandService.Endpoints;

public static class EndpointDependencyInjection
{
    public static WebApplication MapEndpoints(this WebApplication application)
    {
        new Bands.BandMapper().Register(application);
        new User.UserMapper().Register(application);

        return application;
    }
}
