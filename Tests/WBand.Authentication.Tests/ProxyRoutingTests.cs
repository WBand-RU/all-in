using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WBand.WebProxy;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace WBand.Authentication.Tests;

public sealed class ProxyRoutingTests
{
    [Theory]
    [InlineData("/api/user/me")]
    [InlineData("/band-api/bands")]
    [InlineData("/song-api/songs")]
    [InlineData("/playlist-api/playlists")]
    [InlineData("/stem-api/stems")]
    [InlineData("/mixer-api/mixes")]
    [InlineData("/playback-api/playback")]
    [InlineData("/files/presign-upload")]
    public async Task Proxy_WhenAnonymous_ProtectsEveryApiPrefix(string path)
    {
        using var factory = Factory("http://localhost:1");
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Proxy_WhenCookieAuthenticated_PreservesPathsAndOnlyForwardsSessionBearerToApi()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var upstream = builder.Build();
        upstream.Map(
            "/{**path}",
            (HttpContext context) =>
                Results.Json(
                    new
                    {
                        path = context.Request.Path.Value,
                        cookie = context.Request.Headers.Cookie.ToString(),
                        authorization = context.Request.Headers.Authorization.ToString(),
                        csrf = context.Request.Headers["X-CSRF-TOKEN"].ToString(),
                    }
                )
        );
        await upstream.StartAsync();
        var address = upstream
            .Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using var factory = Factory(address);
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/test/signin");
        client.DefaultRequestHeaders.Authorization = new("Bearer", "caller-supplied-token");
        var profile = (await client.GetFromJsonAsync<SessionResponse>("/auth/me"))!;
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsync("/band-api/bands", null)).StatusCode
        );
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", profile.CsrfToken);
        var api = await client.PostAsync("/band-api/bands", null);
        Assert.Equal(HttpStatusCode.OK, api.StatusCode);
        var apiRequest = (await api.Content.ReadFromJsonAsync<ForwardedRequest>())!;
        Assert.Equal("/band-api/bands", apiRequest.Path);
        Assert.Equal("Bearer session-access-token", apiRequest.Authorization);
        Assert.Empty(apiRequest.Cookie);
        Assert.Empty(apiRequest.Csrf);
        var user = (await client.GetFromJsonAsync<ForwardedRequest>("/api/user/me"))!;
        Assert.Equal("/api/user/me", user.Path);
        var web = (await client.GetFromJsonAsync<ForwardedRequest>("/app"))!;
        Assert.Empty(web.Cookie);
        Assert.Empty(web.Authorization);
    }

    private static WebApplicationFactory<Program> Factory(string upstream) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(
                (_, configuration) =>
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["WEB_COMMA_SPLIT_ADDRESSES"] = upstream,
                            ["API_COMMA_SPLIT_ADDRESSES"] = upstream,
                            ["AUTHORIZED_PROXY_OAUTH_AUTHORITY"] =
                                "https://identity.example/realms/test",
                            ["AUTHORIZED_PROXY_OAUTH_CLIENT_ID"] = "bff",
                            ["AUTHORIZED_PROXY_OAUTH_CLIENT_SECRET"] = "test-only",
                            ["AUTHORIZED_PROXY_REDIS_CONNECTION_STRING"] = "localhost:1",
                            ["AUTHORIZED_PROXY_REDIS_KEY_PREFIX"] = "test:",
                        }
                    )
            );
            builder.ConfigureServices(services =>
            {
                var originalProvider = services.Last(x =>
                    x.ServiceType == typeof(IProxyConfigProvider)
                );
                services.RemoveAll<IProxyConfigProvider>();
                services.AddSingleton<IProxyConfigProvider>(provider =>
                {
                    var original = (IProxyConfigProvider)(
                        originalProvider.ImplementationInstance
                        ?? originalProvider.ImplementationFactory!(provider)
                    );
                    var config = original.GetConfig();
                    return new InMemoryConfigProvider(
                        config.Routes,
                        config
                            .Clusters.Select(cluster =>
                                cluster with
                                {
                                    Destinations = new Dictionary<string, DestinationConfig>
                                    {
                                        ["test"] = new() { Address = upstream },
                                    },
                                }
                            )
                            .ToArray()
                    );
                });
                services.RemoveAll<TokenRefreshService>();
                services.RemoveAll<IConnectionMultiplexer>();
                services.RemoveAll<IConfigureOptions<KeyManagementOptions>>();
                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();
                services.AddSingleton<IDataProtectionProvider>(
                    new EphemeralDataProtectionProvider()
                );
                services.PostConfigure<CookieAuthenticationOptions>(
                    AuthenticationExtensions.CookieScheme,
                    options => options.Events.OnValidatePrincipal = _ => Task.CompletedTask
                );
                services.AddSingleton<IStartupFilter, SignInFilter>();
            });
        });

    private sealed class SignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(
                    async (context, continuation) =>
                    {
                        if (context.Request.Path != "/test/signin")
                        {
                            await continuation();
                            return;
                        }
                        var identity = new ClaimsIdentity(
                            [new Claim("sub", Guid.NewGuid().ToString())],
                            AuthenticationExtensions.CookieScheme,
                            "preferred_username",
                            "roles"
                        );
                        var properties = new AuthenticationProperties();
                        properties.StoreTokens([
                            new AuthenticationToken
                            {
                                Name = "access_token",
                                Value = "session-access-token",
                            },
                        ]);
                        await context.SignInAsync(
                            AuthenticationExtensions.CookieScheme,
                            new ClaimsPrincipal(identity),
                            properties
                        );
                    }
                );
                next(app);
            };
    }

    public sealed record ForwardedRequest(
        string Path,
        string Cookie,
        string Authorization,
        string Csrf
    );
}
