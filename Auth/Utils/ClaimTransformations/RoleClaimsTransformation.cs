using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Auth.Utils.ClaimTransformations;

internal class RoleClaimsTransformation : IClaimsTransformation
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        // when decoding the token with jwt.io roles is present under realm_access
        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim is null)
        {
            return Task.FromResult(principal);
        }

        // Deserialize the realm_access JSON to extract the roles
        var realmAccess = JsonSerializer.Deserialize<RealmAccess>(
            realmAccessClaim.Value,
            JsonSerializerOptions
        );

        if (realmAccess?.Roles != null)
        {
            foreach (var role in realmAccess.Roles)
            {
                // Add each role as a Claim of type ClaimTypes.Role
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return Task.FromResult(principal);
    }

    public class RealmAccess
    {
        public List<string>? Roles { get; set; }
    }
}
