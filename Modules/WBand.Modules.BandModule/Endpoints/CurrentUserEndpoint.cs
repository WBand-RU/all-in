using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using WBand.Modules.BandModule.Application;
using Wolverine;
using WBand.Modules.BandModule.Data;
using WBand.Contracts;
using Wolverine.Http;

namespace WBand.Modules.BandModule.Endpoints;

public static class CurrentUserEndpoint
{
    public sealed record CurrentUserResponse(
        Guid Id,
        string Email,
        string? DisplayName,
        IReadOnlyCollection<string> PlatformRoles
    );

    [Authorize]
    [Tags("WBand.WebAPI.Endpoints.CurrentUserEndpoint")]
    [WolverineGet("/api/user/me")]
    public static async Task<CurrentUserResponse> Get(
        ICurrentUser currentUser,
        [Microsoft.AspNetCore.Mvc.FromServices] BandDataContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var user = await UserProfileSynchronization.EnsureProfile(currentUser, db, bus, cancellationToken);

        var roles = new List<string>();
        if (currentUser.IsInRole(Roles.SuperAdmin))
        {
            roles.Add(Roles.SuperAdmin);
        }

        if (currentUser.IsInRole(Roles.Moderator))
        {
            roles.Add(Roles.Moderator);
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
