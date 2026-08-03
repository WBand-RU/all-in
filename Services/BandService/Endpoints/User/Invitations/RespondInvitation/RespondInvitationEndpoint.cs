using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.User.Invitations.RespondInvitation;

/// <summary>
/// Endpoint for responding to an invitation (accept or decline)
/// </summary>
internal sealed class RespondInvitationEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapPut("invitations/{invitationId}", Handle)
            .WithName("RespondInvitation")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<Invitation>> Handle(
        string invitationId,
        RespondInvitationRequest request,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        // Find the invitation
        var invitation = await repository
            .Invitations.Find(i => i.Id == invitationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (invitation == null)
        {
            return ApiResponse<Invitation>.Error(ApiCodes.NotFound);
        }

        // Check if current user is the invitee
        if (invitation.InviteeEmail != currentUser.GetUserEmail)
        {
            return ApiResponse<Invitation>.Error(ApiCodes.Forbidden);
        }

        // Check if invitation is still pending
        if (invitation.Status != InvitationStatus.Pending)
        {
            return ApiResponse<Invitation>.Error(ApiCodes.Conflict);
        }

        // Update invitation status
        var newStatus = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        var update = Builders<Invitation>.Update.Set(i => i.Status, newStatus);

        await repository.Invitations.UpdateOneAsync(
            i => i.Id == invitationId,
            update,
            cancellationToken: cancellationToken
        );

        // If accepted, create member record
        if (request.Accept)
        {
            var member = new Member
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserId = currentUser.GetUserId,
                Email = invitation.InviteeEmail,
                BandId = invitation.BandId,
                Role = invitation.Role,
                JoinedAt = DateTime.UtcNow,
                Permissions =
                    invitation.Role == MemberRole.Owner ? Permission.All
                    : invitation.Role == MemberRole.Admin
                        ? Permission.ManageMembers
                            | Permission.EditSongs
                            | Permission.EditPlaylists
                            | Permission.ManagePlaybacks
                            | Permission.ManageAgents
                            | Permission.SendMessages
                    : Permission.SendMessages,
            };

            await repository.Members.InsertOneAsync(member, cancellationToken: cancellationToken);
        }

        // Update invitation in memory for response
        invitation.Status = newStatus;

        return ApiResponse<Invitation>.Success(invitation);
    }
}
