using BandService.Domain;
using BandService.Services;
using FluentValidation;
using Shared;
using Shared.Services;

namespace BandService.Endpoints.Bands.Members.GetMembers;

/// <summary>
/// Endpoint for getting all members of a band
/// </summary>
internal sealed class GetMembersEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapGet("", Handle)
            .WithName("GetMembers")
            .RequireAuthorization(x => x.RequireRole(Roles.User));
    }

    public static async Task<ApiResponse<List<Member>>> Handle(
        string bandId,
        ICurrentUser currentUser,
        Repository repository,
        PermissionService permissionService,
        CancellationToken cancellationToken
    )
    {
        // Check if user has permission to view members
        var hasPermission = await permissionService.HasPermissionAsync(
            currentUser.GetUserId,
            bandId,
            Permission.ManageMembers,
            cancellationToken
        );

        if (!hasPermission)
        {
            return ApiResponse<List<Member>>.Error(ApiCodes.Forbidden);
        }

        // Get all members of the band
        var members = await repository
            .Members.Find(m => m.BandId == bandId)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<Member>>.Success(members);
    }
}
