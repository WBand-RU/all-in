using BandService.Domain;
using BandService.Endpoints.Bands.CreateBand;
using BandService.Services;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared;

namespace BandService.Endpoints.Bands.UpdateBand;

internal sealed class UpdateBandEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/bands/{bandId}", Handle).WithName("UpdateBand").WithTags("Bands");
    }

    public static async Task<ApiResponse> Handle(
        string bandId,
        UpdateBandRequest request,
        IValidator<UpdateBandRequest> validator,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ApiResponse.Error(ApiCodes.ValidationFailed);
        }

        var member = await repository
            .Members.Find(m => m.UserId == currentUser.GetUserId && m.BandId == bandId)
            .FirstOrDefaultAsync(cancellationToken);
        if (member is null || member.Role is not MemberRole.Owner)
        {
            return ApiResponse.Error(ApiCodes.Forbidden);
        }

        var existingBand = await repository
            .Bands.Find(b => b.Id != bandId && b.Name == request.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingBand is not null)
        {
            return ApiResponse.Error(ApiCodes.Conflict);
        }

        var update = Builders<Band>.Update.Set(b => b.Name, request.Name);
        var result = await repository.Bands.UpdateOneAsync(
            b => b.Id == bandId,
            update,
            cancellationToken: cancellationToken
        );
        if (result.ModifiedCount == 0)
        {
            return ApiResponse.Error(ApiCodes.NotFound);
        }

        return ApiResponse.Success();
    }
}
