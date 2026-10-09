using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Shared.Services;
using Xunit;

namespace WBand.Authentication.Tests;

public sealed class ApiAuthenticationTests
{
    [Theory]
    [InlineData("https://identity.example/realms/wband", "webapi", false, 200)]
    [InlineData("https://evil.example/realms/wband", "webapi", false, 401)]
    [InlineData("https://identity.example/realms/wband", "other-api", false, 401)]
    [InlineData("https://identity.example/realms/wband", "webapi", true, 401)]
    public async Task Api_WhenBearerProvided_ValidatesIssuerAudienceAndLifetime(
        string issuer,
        string audience,
        bool expired,
        int expected
    )
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        await using var app = await CreateApp(key);
        var client = app.GetTestClient();
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("email", "musician@example.com"),
                new Claim("realm_access", "{\"roles\":[\"moderator\"]}", JsonClaimValueTypes.Json),
            ],
            DateTime.UtcNow.AddMinutes(-10),
            expired ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        );
        client.DefaultRequestHeaders.Authorization = new(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token)
        );
        var response = await client.GetAsync("/protected");
        Assert.Equal((HttpStatusCode)expected, response.StatusCode);
        if (expected == 200)
        {
            Assert.Equal("musician@example.com", await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/moderation")).StatusCode);
        }
    }

    [Fact]
    public async Task Api_WhenOnlyBrowserCookieProvided_Returns401()
    {
        using var rsa = RSA.Create(2048);
        await using var app = await CreateApp(new RsaSecurityKey(rsa));
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Cookie", "WBand.Session=fake-cookie");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("email")]
    public async Task Api_WhenRequiredUserClaimMissing_Returns401(string missingClaim)
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        await using var app = await CreateApp(key);
        var claims = new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("email", "musician@example.com"),
        }.Where(claim => claim.Type != missingClaim);
        var token = new JwtSecurityToken(
            "https://identity.example/realms/wband",
            "webapi",
            claims,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        );
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/protected")).StatusCode);
    }

    private static async Task<WebApplication> CreateApp(SecurityKey key)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Keycloak:Url"] = "https://identity.example";
        builder.Configuration["Keycloak:Realm"] = "wband";
        builder.Configuration["Keycloak:ClientId"] = "webapi";
        builder.Configuration["Keycloak:SslRequired"] = "true";
        builder.AddKeycloakAuthentication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    Issuer = "https://identity.example/realms/wband",
                };
                configuration.SigningKeys.Add(key);
                options.ConfigurationManager =
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }
        );
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet(
            "/protected",
            (ICurrentUser user) =>
            {
                _ = user.GetUserId;
                return user.GetUserEmail;
            }
        );
        app.MapGet("/moderation", () => "ok").RequireAuthorization(Shared.Roles.Moderator);
        app.MapGet("/health", () => "ok").AllowAnonymous();
        await app.StartAsync();
        return app;
    }
}
