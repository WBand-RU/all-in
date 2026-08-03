using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.Bands.Members.UpdateMemberRole;

/// <summary>
/// Endpoint for updating a member's role in a band
/// </summary>
internal sealed class UpdateMemberRoleEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapPut("{memberId}/role", Handle)
            .WithName("UpdateMemberRole")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<Member>> Handle(
        string bandId,
        string memberId,
        UpdateMemberRoleRequest request,
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
            return ApiResponse<Member>.Error(ApiCodes.Forbidden);
        }

        // Find the member
        var member = await repository
            .Members.Find(m => m.Id == memberId && m.BandId == bandId)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null)
        {
            return ApiResponse<Member>.Error(ApiCodes.NotFound);
        }

        // Cannot change owner's role
        if (member.Role == MemberRole.Owner)
        {
            return ApiResponse<Member>.Error(ApiCodes.Forbidden);
        }

        // Cannot change to owner role
        if (request.NewRole == MemberRole.Owner)
        {
            return ApiResponse<Member>.Error(ApiCodes.Forbidden);
        }

        // Update member role and permissions
        var permissions = request.NewRole switch
        {
            MemberRole.Owner => Permission.All,
            MemberRole.Admin => Permission.ManageMembers
                | Permission.EditSongs
                | Permission.EditPlaylists
                | Permission.ManagePlaybacks
                | Permission.ManageAgents
                | Permission.SendMessages,
            MemberRole.Member => Permission.SendMessages,
            _ => Permission.None,
        };

        var update = Builders<Member>
            .Update.Set(m => m.Role, request.NewRole)
            .Set(m => m.Permissions, permissions);

        await repository.Members.UpdateOneAsync(
            m => m.Id == memberId,
            update,
            cancellationToken: cancellationToken
        );

        // Update member in memory for response
        member.Role = request.NewRole;
        member.Permissions = permissions;

        return ApiResponse<Member>.Success(member);
    }
}
