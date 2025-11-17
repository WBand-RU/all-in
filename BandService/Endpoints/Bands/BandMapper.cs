namespace BandService.Endpoints.Bands;

public sealed class BandMapper
{
    public void Register(IEndpointRouteBuilder builder)
    {
        var group = builder
            .MapGroup("/bands")
            .WithTags("Bands")
            .WithDisplayName("Bands")
            .RequireAuthorization();

        CreateBand.CreateBandEndpoint.Build(group);
        UpdateBand.UpdateBandEndpoint.Build(group);
        DeleteBand.DeleteBandEndpoint.Build(group);
        GetBand.GetBandEndpoint.Build(group);
        GetListOfBands.GetListOfBandsEndpoint.Build(group);
    }
}
