using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.Bands.Invitations.CreateInvitation;

/// <summary>
/// Endpoint for creating a band invitation
/// </summary>
internal sealed class CreateInvitationEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost("", Handle).WithName("CreateBandInvitation").RequireAuthorization();
    }

    public static async Task<ApiResponse<Invitation>> Handle(
        string bandId,
        CreateInvitationRequest request,
        ICurrentUser currentUser,
        Repository repository,
        PermissionService permissionService,
        CancellationToken cancellationToken
    )
    {
        // Check if user has permission to invite members
        var hasPermission = await permissionService.HasPermissionAsync(
            currentUser.GetUserId,
            bandId,
            Permission.ManageMembers,
            cancellationToken
        );

        if (!hasPermission)
        {
            return ApiResponse<Invitation>.Error(ApiCodes.Forbidden);
        }

        // Check if user is already a member
        var existingMember = await repository
            .Members.Find(m => m.BandId == bandId && m.UserId == request.InviteeEmail)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingMember != null)
        {
            return ApiResponse<Invitation>.Error(ApiCodes.Conflict);
        }

        // Check if there's already a pending invitation
        var existingInvitation = await repository
            .Invitations.Find(i =>
                i.BandId == bandId
                && i.InviteeEmail == request.InviteeEmail
                && i.Status == InvitationStatus.Pending
            )
            .FirstOrDefaultAsync(cancellationToken);

        if (existingInvitation != null)
        {
            return ApiResponse<Invitation>.Error(ApiCodes.Conflict);
        }

        var invitation = new Invitation
        {
            Id = ObjectId.GenerateNewId().ToString(),
            InviterId = currentUser.GetUserId,
            InviterEmail = currentUser.GetUserEmail,
            InviteeEmail = request.InviteeEmail,
            BandId = bandId,
            Role = request.Role,
            Status = InvitationStatus.Pending,
        };

        await repository.Invitations.InsertOneAsync(
            invitation,
            cancellationToken: cancellationToken
        );

        // TODO: Send email notification to invitee

        return ApiResponse<Invitation>.Success(invitation);
    }
}
