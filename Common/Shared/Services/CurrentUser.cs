using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid GetUserId =>
        Guid.Parse(
            httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User not found")
        );

    public string GetUserEmail =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("email")
        ?? throw new InvalidOperationException("Email not found");

    public string? DisplayName =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("preferred_username");

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User?.IsInRole(role) == true;
}
