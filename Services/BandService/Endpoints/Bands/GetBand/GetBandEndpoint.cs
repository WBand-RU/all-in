using BandService.Domain;
using BandService.Endpoints.Bands.CreateBand;
using BandService.Services;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared;

namespace BandService.Endpoints.Bands.GetBand;

internal sealed class GetBandEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/bands/{bandId}", Handle).WithName("GetBand").WithTags("Bands");
    }

    public static async Task<ApiResponse<Band>> Handle(
        string bandId,
        ICurrentUser currentUser,
        Repository repository
    )
    {
        var allowedBandIdList = await repository
            .Members.Find(m => m.UserId == currentUser.GetUserId)
            .Project(m => m.BandId)
            .ToListAsync();

        var band = await repository
            .Bands.Find(b => allowedBandIdList.Contains(b.Id))
            .SortBy(b => b.Name)
            .FirstOrDefaultAsync();

        if (band is null)
        {
            return ApiResponse<Band>.Error(ApiCodes.NotFound);
        }

        return ApiResponse<Band>.Success(band);
    }
}
