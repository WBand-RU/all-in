using BandEvents;
using BandService.Domain;
using BandService.Services;
using CSharpFunctionalExtensions;
using FluentValidation;
using Shared;
using Shared.Services;
using Wolverine.Http;

namespace BandService.Endpoints.Bands;

internal sealed class CreateBandEndpoint
{
    public sealed record CreateBandRequest(string Name);

    public sealed class CreateBandValidator : AbstractValidator<CreateBandRequest>
    {
        public CreateBandValidator()
        {
            _ = this.RuleFor(x => x.Name).NotEmpty().NotNull();
        }
    }

    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost("/bands", Handle).WithName("CreateBand").WithTags("Bands");
    }

    [WolverinePost("/bands", Name = "CreateBand", OperationId = "CreateBand")]
    public static async Task<(Guid, BandCreated)> Handle(
        CreateBandRequest request,
        IValidator<CreateBandRequest> validator,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ApiResponse<Band>.Error(ApiCodes.ValidationFailed);
        }

        var existingBandList = await repository
            .Bands.Find(b => b.Name == request.Name)
            .ToListAsync(cancellationToken);

        if (existingBandList.Count != 0)
        {
            var existingBandIdList = existingBandList.Select(b => b.Id).ToList();
            var isMember = await repository
                .Members.Find(m =>
                    existingBandIdList.Contains(m.BandId) && m.UserId == currentUser.GetUserId
                )
                .FirstOrDefaultAsync(cancellationToken);
            if (isMember is not null)
            {
                return ApiResponse<Band>.Error(ApiCodes.Conflict);
            }
        }

        var band = new Band
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.GetUserId,
        };
        await repository.Bands.InsertOneAsync(band, cancellationToken: cancellationToken);

        await repository.Members.InsertOneAsync(
            new Member
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = currentUser.GetUserId,
                Email = currentUser.GetUserEmail,
                BandId = band.Id,
                Role = MemberRole.Owner,
                JoinedAt = DateTime.UtcNow,
                Permissions = PermissionService.GetDefaultPermissions(MemberRole.Owner),
            },
            cancellationToken: cancellationToken
        );

        return ApiResponse<Band>.Success(band);
    }
}
