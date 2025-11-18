namespace BandService.Endpoints.User.Invitations.GetMyInvitationsCount;

using BandService.Domain;
using BandService.Services;
using MongoDB.Driver;
using Shared;

/// <summary>
/// Endpoint for getting the count of pending invitations for the current user
/// </summary>
internal sealed class GetMyInvitationsCountEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("invitations/count", Handle)
            .WithName("GetMyInvitationsCount")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<int>> Handle(
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        // Get count of pending invitations where current user is the invitee
        var count = await repository.Invitations.CountDocumentsAsync(
            i => i.InviteeEmail == currentUser.GetUserEmail && i.Status == InvitationStatus.Pending,
            cancellationToken: cancellationToken
        );

        return ApiResponse<int>.Success((int)count);
    }
}
