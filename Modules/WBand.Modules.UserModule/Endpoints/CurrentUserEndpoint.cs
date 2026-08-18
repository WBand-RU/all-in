using Marten;
using Microsoft.AspNetCore.Authorization;
using Shared.Services;
using WBand.Modules.UserModule.Domain;
using Wolverine.Http;

namespace WBand.Modules.UserModule.Endpoints;

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
        IDocumentSession session,
        CancellationToken cancellationToken
    )
    {
        var now = DateTimeOffset.UtcNow;
        var profile = await session.LoadAsync<UserProfile>(currentUser.GetUserId, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile
            {
                Id = currentUser.GetUserId,
                Email = currentUser.GetUserEmail.Trim().ToLowerInvariant(),
                DisplayName = currentUser.DisplayName,
                CreatedAt = now,
                UpdatedAt = now,
                LastLoginAt = now,
            };
        }
        else
        {
            profile.Email = currentUser.GetUserEmail.Trim().ToLowerInvariant();
            profile.DisplayName = currentUser.DisplayName;
            profile.UpdatedAt = now;
            profile.LastLoginAt = now;
        }

        session.Store(profile);
        await session.SaveChangesAsync(cancellationToken);

        var roles = new List<string>();
        if (currentUser.IsInRole(Shared.Roles.SuperAdmin)) roles.Add(Shared.Roles.SuperAdmin);
        if (currentUser.IsInRole(Shared.Roles.Moderator)) roles.Add(Shared.Roles.Moderator);

        return new(profile.Id, profile.Email, profile.DisplayName, roles);
    }
}
