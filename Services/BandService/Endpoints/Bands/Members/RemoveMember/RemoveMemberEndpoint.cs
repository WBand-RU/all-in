using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.Bands.Members.RemoveMember;

/// <summary>
/// Endpoint for removing a member from a band
/// </summary>
internal sealed class RemoveMemberEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapDelete("{memberId}", Handle)
            .WithName("RemoveMember")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<bool>> Handle(
        string bandId,
        string memberId,
        ICurrentUser currentUser,
        Repository repository,
        PermissionService permissionService,
        CancellationToken cancellationToken
    )
    {
        // Check if user has permission to manage members
        var hasPermission = await permissionService.HasPermissionAsync(
            currentUser.GetUserId,
            bandId,
            Permission.ManageMembers,
            cancellationToken
        );

        if (!hasPermission)
        {
            return ApiResponse<bool>.Error(ApiCodes.Forbidden);
        }

        // Find the member
        var member = await repository
            .Members.Find(m => m.Id == memberId && m.BandId == bandId)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null)
        {
            return ApiResponse<bool>.Error(ApiCodes.NotFound);
        }

        // Cannot remove owners
        if (member.Role == MemberRole.Owner)
        {
            return ApiResponse<bool>.Error(ApiCodes.Forbidden);
        }

        // Cannot remove yourself (for safety)
        if (member.UserId == currentUser.GetUserId)
        {
            return ApiResponse<bool>.Error(ApiCodes.Forbidden);
        }

        // Remove the member
        var result = await repository.Members.DeleteOneAsync(
            m => m.Id == memberId && m.BandId == bandId,
            cancellationToken
        );

        return ApiResponse<bool>.Success(result.DeletedCount > 0);
    }
}
