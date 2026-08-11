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

    public string GetUserEmail =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? throw new InvalidOperationException("Email not found");

    public string? DisplayName =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User?.IsInRole(role) == true;
}
