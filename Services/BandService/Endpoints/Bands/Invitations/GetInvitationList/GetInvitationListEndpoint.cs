using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.Bands.Invitations.GetInvitationList;

/// <summary>
/// Endpoint for getting a list of invitations for a band
/// </summary>
internal sealed class GetInvitationListEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapGet("", Handle).WithName("GetBandInvitationList").RequireAuthorization();
    }

    public static async Task<ApiResponse<List<Invitation>>> Handle(
        string bandId,
        ICurrentUser currentUser,
        Repository repository,
        PermissionService permissionService,
        CancellationToken cancellationToken
    )
    {
        // Check if user has permission to view invitations for this band
        var hasPermission = await permissionService.HasPermissionAsync(
            currentUser.GetUserId,
            bandId,
            Permission.ManageMembers,
            cancellationToken
        );

        if (!hasPermission)
        {
            return ApiResponse<List<Invitation>>.Error(ApiCodes.Forbidden);
        }

        // Get invitations where current user is either inviter or invitee
        var invitations = await repository
            .Invitations.Find(i =>
                i.BandId == bandId
                && (i.InviterId == currentUser.GetUserId || i.InviteeEmail == currentUser.GetUserId)
            )
            .ToListAsync(cancellationToken);

        return ApiResponse<List<Invitation>>.Success(invitations);
    }
}
