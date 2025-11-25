using BandService.Domain;
using BandService.Endpoints.Bands.CreateBand;
using BandService.Services;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared;

namespace BandService.Endpoints.Bands.DeleteBand;

internal sealed class DeleteBandEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/bands/{bandId}", Handle).WithName("DeleteBand").WithTags("Bands");
    }

    public static async Task<ApiResponse> Handle(
        string bandId,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var member = await repository
            .Members.Find(m => m.UserId == currentUser.GetUserId && m.BandId == bandId)
            .FirstOrDefaultAsync(cancellationToken);
        if (member is null || member.Role is not MemberRole.Owner)
        {
            return ApiResponse.Error(ApiCodes.Forbidden);
        }

        var result = await repository.Bands.DeleteOneAsync(b => b.Id == bandId, cancellationToken);
        if (result.DeletedCount == 0)
        {
            return ApiResponse.Error(ApiCodes.NotFound);
        }

        return ApiResponse.Success();
    }
}
