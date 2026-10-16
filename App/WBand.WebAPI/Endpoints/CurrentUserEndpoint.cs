using Microsoft.AspNetCore.Authorization;
using WBand.WebAPI.Data;
using WBand.WebAPI.Services;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints;

public static class CurrentUserEndpoint
{
    public sealed record CurrentUserResponse(
        Guid Id,
        string Email,
        string? DisplayName,
        IReadOnlyCollection<string> PlatformRoles
    );

    [Authorize]
    [WolverineGet("/api/user/me")]
    public static async Task<CurrentUserResponse> Get(
        ICurrentUser currentUser,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken cancellationToken
    )
    {
        var user =
            await db.SingleOrDefaultAsync<User>("select * from public.users where keycloak_id = @id", new { id = currentUser.Id }, cancellationToken) ?? throw new InvalidOperationException("User is not found in database");

        var roles = new List<string>();
        if (currentUser.IsInRole(Shared.Roles.SuperAdmin))
        {
            roles.Add(Shared.Roles.SuperAdmin);
        }

        if (currentUser.IsInRole(Shared.Roles.Moderator))
        {
            roles.Add(Shared.Roles.Moderator);
        }

        string displayName = user.Email;
        if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
        {
            displayName = user.FirstName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = user.LastName;
            }
            else
            {
                displayName = $"{user.FirstName} {user.LastName}";
            }
        }

        return new(user.Id, user.Email, displayName, roles);
    }
}
