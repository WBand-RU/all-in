using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WBand.WebAPI.Services;

namespace WBand.WebAPI.Services.Implementations;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id =>
        Guid.Parse(
            httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User not found")
        );

    public string Email =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("email")
        ?? throw new InvalidOperationException("Email not found");

    public string FirstName =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue("firstName")
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("given_name")
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.GivenName)
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("preferred_firstname")
        ?? string.Empty;

    public string LastName =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue("lastName")
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("family_name")
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Surname)
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("preferred_lastname")
        ?? string.Empty;

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User?.IsInRole(role) == true;
}
