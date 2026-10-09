using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace WBand.WebProxy;

internal static class SessionClaims
{
    // Only read tokens received directly from the trusted OIDC token endpoint, never request headers.
    internal static void UpdateRoles(ClaimsPrincipal principal, string accessToken)
    {
        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var claim in identity.FindAll("roles").ToArray())
            identity.RemoveClaim(claim);
        var token = new JsonWebToken(accessToken);
        if (
            !token.TryGetPayloadValue<JsonElement>("realm_access", out var realm)
            || !realm.TryGetProperty("roles", out var roles)
        )
            return;
        foreach (
            var role in roles
                .EnumerateArray()
                .Select(x => x.GetString())
                .OfType<string>()
                .Distinct()
        )
            identity.AddClaim(new Claim("roles", role));
    }
}
