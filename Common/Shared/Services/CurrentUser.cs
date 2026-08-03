using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid GetUserId =>
        Guid.Parse(
            httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User not found")
        );

    public string Role =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
        ?? throw new InvalidOperationException("Role not found");

    public string GetUserEmail =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? throw new InvalidOperationException("Email not found");
}
