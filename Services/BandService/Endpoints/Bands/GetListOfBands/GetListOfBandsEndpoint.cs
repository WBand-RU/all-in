using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.Bands.GetListOfBands;

internal sealed class GetListOfBandsEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapGet("/bands", Handle).WithName("GetListOfBands").WithTags("Bands");
    }

    public static async Task<ApiResponse<List<Band>>> Handle(
        ICurrentUser currentUser,
        Repository repository
    )
    {
        var userId = currentUser.GetUserId;
        var allowedBandIdList = await repository
            .Members.Find(m => m.UserId == userId)
            .Project(m => m.BandId)
            .ToListAsync();

        var bands = await repository
            .Bands.Find(b => allowedBandIdList.Contains(b.Id))
            .SortBy(b => b.Name)
            .ToListAsync();

        return ApiResponse<List<Band>>.Success(bands);
    }
}
