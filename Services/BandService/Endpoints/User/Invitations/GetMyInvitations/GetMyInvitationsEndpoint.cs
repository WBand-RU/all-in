using BandService.Domain;
using BandService.Services;
using FluentValidation;
using MongoDB.Driver;
using Shared;

namespace BandService.Endpoints.User.Invitations.GetMyInvitations;

/// <summary>
/// Endpoint for getting invitations for the current user
/// </summary>
internal sealed class GetMyInvitationsEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("invitations", Handle).WithName("GetMyInvitations").RequireAuthorization();
    }

    public static async Task<ApiResponse<List<GetMyInvitationsResponse>>> Handle(
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        // Get all pending invitations where current user is the invitee
        var invitations = await repository
            .Invitations.Find(i =>
                i.InviteeEmail == currentUser.GetUserEmail && i.Status == InvitationStatus.Pending
            )
            .ToListAsync(cancellationToken);

        // Enrich with band names
        var enrichedInvitations = new List<GetMyInvitationsResponse>();

        foreach (var invitation in invitations)
        {
            // Get band name
            var band = await repository
                .Bands.Find(b => b.Id == invitation.BandId)
                .FirstOrDefaultAsync(cancellationToken);
            var bandName = band?.Name ?? "Unknown Band";

            enrichedInvitations.Add(
                new GetMyInvitationsResponse(
                    invitation.Id,
                    invitation.InviteeEmail,
                    invitation.BandId,
                    bandName,
                    invitation.InviterEmail ?? invitation.InviterId, // Fallback to ID if email not set
                    invitation.Role,
                    invitation.Status
                )
            );
        }

        return ApiResponse<List<GetMyInvitationsResponse>>.Success(enrichedInvitations);
    }
}
