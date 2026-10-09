using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WBand.WebProxy;
using Xunit;

namespace WBand.Authentication.Tests;

public sealed class ProxyAuthenticationTests
{
    [Fact]
    public async Task GetSession_WhenAnonymous_Returns401WithoutRedirect()
    {
        await using var app = await CreateApp();
        var response = await app.GetTestClient().GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task GetSession_WhenSignedIn_ReturnsProfileAndCsrfWithoutTokens()
    {
        await using var app = await CreateApp();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Cookie", await SignIn(client));
        var response = await client.GetAsync("/auth/me");
        var profile = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.Equal("musician", profile!.Username);
        Assert.Equal("musician@example.com", profile.Email);
        Assert.Contains("moderator", profile.Roles);
        Assert.NotEmpty(profile.CsrfToken);
        Assert.DoesNotContain("secret-access-token", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logout_WhenCsrfMissing_RejectsPostAndDisallowsGet()
    {
        await using var app = await CreateApp();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Cookie", await SignIn(client));
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsync("/auth/logout", null)).StatusCode
        );
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/auth/logout")).StatusCode);
    }

    [Fact]
    public async Task Logout_WhenCsrfValid_ClearsCookieAndRedirectsToKeycloak()
    {
        await using var app = await CreateApp();
        var client = app.GetTestClient();
        var sessionCookie = await SignIn(client);
        client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
        var me = await client.GetAsync("/auth/me");
        var profile = await me.Content.ReadFromJsonAsync<SessionResponse>();
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", sessionCookie + "; " + Cookie(me, "WBand.Csrf"));
        var response = await client.PostAsync(
            "/auth/logout",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = profile!.CsrfToken,
                }
            )
        );
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(
            "https://identity.example/logout",
            response.Headers.Location!.AbsoluteUri
        );
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("WBand.Session=;")
        );
    }

    [Theory]
    [InlineData("https://evil.example", "/app")]
    [InlineData("//evil.example", "/app")]
    [InlineData("/\\evil.example", "/app")]
    [InlineData("/app?tab=songs", "/app?tab=songs")]
    public async Task Login_WhenReturnUrlProvided_OnlyUsesLocalRedirects(
        string returnUrl,
        string expected
    )
    {
        await using var app = await CreateApp();
        var response = await app.GetTestClient()
            .GetAsync("/auth/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, AuthenticationExtensions.LocalReturnUrl(returnUrl));
        Assert.Contains("code_challenge=", response.Headers.Location!.Query);
        Assert.Contains("scope=openid", response.Headers.Location.Query);
    }

    private static async Task<WebApplication> CreateApp()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = "Development" }
        );
        builder.WebHost.UseTestServer();
        builder
            .Services.AddAuthentication(AuthenticationExtensions.CookieScheme)
            .AddCookie(AuthenticationExtensions.CookieScheme)
            .AddOpenIdConnect(
                AuthenticationExtensions.OidcScheme,
                options =>
                {
                    options.ClientId = "test-client";
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = "https://identity.example",
                        AuthorizationEndpoint = "https://identity.example/authorize",
                        EndSessionEndpoint = "https://identity.example/logout",
                    };
                }
            );
        builder.Services.AddAuthorization();
        builder.AddWBandAuthentication();
        builder.Services.RemoveAll<TokenRefreshService>();
        // Test real cookies and CSRF without a network-backed token refresh service.
        builder.Services.PostConfigure<CookieAuthenticationOptions>(
            AuthenticationExtensions.CookieScheme,
            options => options.Events.OnValidatePrincipal = _ => Task.CompletedTask
        );
        var app = builder.Build();
        app.UseWBandAuthentication();
        app.MapGet(
            "/test/signin",
            async (Microsoft.AspNetCore.Http.HttpContext context) =>
            {
                var identity = new ClaimsIdentity(
                    [
                        new Claim("sub", Guid.NewGuid().ToString()),
                        new Claim("preferred_username", "musician"),
                        new Claim("email", "musician@example.com"),
                        new Claim("roles", "moderator"),
                    ],
                    AuthenticationExtensions.CookieScheme,
                    "preferred_username",
                    "roles"
                );
                var properties = new AuthenticationProperties();
                properties.StoreTokens([
                    new AuthenticationToken
                    {
                        Name = "access_token",
                        Value = "secret-access-token",
                    },
                ]);
                await context.SignInAsync(
                    AuthenticationExtensions.CookieScheme,
                    new ClaimsPrincipal(identity),
                    properties
                );
            }
        );
        await app.StartAsync();
        return app;
    }

    private static async Task<string> SignIn(HttpClient client)
    {
        var response = await client.GetAsync("/test/signin");
        var cookie = response
            .Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("WBand.Session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        return cookie.Split(';')[0];
    }

    private static string Cookie(HttpResponseMessage response, string name) =>
        response
            .Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(name + "="))
            .Split(';')[0];
}
