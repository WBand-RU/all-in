using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.Bands.Invitations.DeleteInvitation;

/// <summary>
/// Endpoint for deleting an invitation (only if not responded)
/// </summary>
internal sealed class DeleteInvitationEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapDelete("/{invitationId}", Handle)
            .WithName("DeleteBandInvitation")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<bool>> Handle(
        string bandId,
        string invitationId,
        ICurrentUser currentUser,
        Repository repository,
        PermissionService permissionService,
        CancellationToken cancellationToken
    )
    {
        // Find the invitation
        var invitation = await repository
            .Invitations.Find(i => i.Id == invitationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (invitation == null)
        {
            return ApiResponse<bool>.Error(ApiCodes.NotFound);
        }

        // Check if current user is the inviter or has manage members permission
        var isInviter = invitation.InviterId == currentUser.GetUserId;
        var hasPermission = await permissionService.HasPermissionAsync(
            currentUser.GetUserId,
            invitation.BandId,
            Permission.ManageMembers,
            cancellationToken
        );

        if (!isInviter && !hasPermission)
        {
            return ApiResponse<bool>.Error(ApiCodes.Forbidden);
        }

        // Check if invitation is still pending (not responded)
        if (invitation.Status != InvitationStatus.Pending)
        {
            return ApiResponse<bool>.Error(ApiCodes.Conflict);
        }

        // Delete the invitation
        var result = await repository.Invitations.DeleteOneAsync(
            i => i.Id == invitationId,
            cancellationToken
        );

        return ApiResponse<bool>.Success(result.DeletedCount > 0);
    }
}
