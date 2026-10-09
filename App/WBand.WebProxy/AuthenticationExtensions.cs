using System.Security.Claims;
using AuthorizedProxy;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace WBand.WebProxy;

internal static class AuthenticationExtensions
{
    internal const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    internal const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

    public static void AddWBandAuthentication(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment();
        builder.Services.AddSingleton<TokenRefreshService>();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = development ? "WBand.Csrf" : "__Host-WBand.Csrf";
            options.Cookie.SecurePolicy = development
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        builder.Services.Configure<AuthenticationOptions>(options =>
            options.DefaultChallengeScheme = CookieScheme
        );
        builder.Services.PostConfigure<CookieAuthenticationOptions>(
            CookieScheme,
            options =>
            {
                options.Cookie.Name = development ? "WBand.Session" : "__Host-WBand.Session";
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = development
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = context =>
                    context
                        .HttpContext.RequestServices.GetRequiredService<TokenRefreshService>()
                        .ValidateAsync(context);
            }
        );
        builder.Services.PostConfigure<OpenIdConnectOptions>(
            OidcScheme,
            options =>
            {
                options.RequireHttpsMetadata = !development;
                options.UsePkce = true;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.MapInboundClaims = false;
                options.Scope.Clear();
                foreach (var scope in new[] { "openid", "profile", "email", "roles" })
                    options.Scope.Add(scope);
                options.TokenValidationParameters.ValidIssuers = null;
                options.TokenValidationParameters.ValidIssuer = options.Authority;
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = "roles";
                options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
                options.ClaimActions.MapUniqueJsonKey("picture", "picture");
                options.Events.OnTicketReceived = context =>
                {
                    SessionClaims.UpdateRoles(
                        context.Principal!,
                        context.Properties!.GetTokenValue("access_token")!
                    );
                    return Task.CompletedTask;
                };
            }
        );
    }

    public static void UseWBandAuthentication(this WebApplication app)
    {
        var origin = PublicOrigin.Parse(app.Configuration["AUTHORIZED_PROXY_PUBLIC_ORIGIN"]);
        if (origin is not null)
            app.Use(
                (context, next) =>
                {
                    PublicOrigin.Apply(context.Request, origin);
                    return next(context);
                }
            );
        app.Use(
            async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (Exception exception)
                    when (!context.Response.HasStarted
                        && (
                            exception
                                is HttpRequestException
                                    or TimeoutException
                                    or StackExchange.Redis.RedisException
                            || exception is TaskCanceledException
                                && !context.RequestAborted.IsCancellationRequested
                        )
                    )
                {
                    app.Logger.LogWarning("The authentication or upstream service is unavailable.");
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                }
            }
        );
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/auth"))
                    context.Response.Headers.CacheControl = "no-store";
                if (
                    context
                        .GetEndpoint()
                        ?.Metadata.GetMetadata<Yarp.ReverseProxy.Model.RouteModel>()
                        ?.Config.ClusterId == "api"
                    || context.Request.Path == "/auth/logout"
                )
                {
                    context.Response.Headers.CacheControl = "no-store";
                    if (
                        !HttpMethods.IsGet(context.Request.Method)
                        && !HttpMethods.IsHead(context.Request.Method)
                        && !HttpMethods.IsOptions(context.Request.Method)
                    )
                    {
                        try
                        {
                            await context
                                .RequestServices.GetRequiredService<IAntiforgery>()
                                .ValidateRequestAsync(context);
                        }
                        catch (AntiforgeryValidationException)
                        {
                            context.Response.StatusCode = 403;
                            return;
                        }
                    }
                }
                await next(context);
            }
        );
        app.MapGet(
            "/auth/login",
            (HttpContext context, string? returnUrl) =>
                Results.Challenge(
                    new AuthenticationProperties { RedirectUri = LocalReturnUrl(returnUrl) },
                    [OidcScheme]
                )
        );
        app.MapGet(
                "/auth/me",
                (HttpContext context, IAntiforgery antiforgery) =>
                {
                    if (context.User.Identity?.IsAuthenticated != true)
                        return Results.Unauthorized();
                    var user = context.User;
                    return Results.Ok(
                        new SessionResponse(
                            user.FindFirstValue("sub")!,
                            user.FindFirstValue("preferred_username"),
                            user.FindFirstValue("email"),
                            user.FindFirstValue("name"),
                            user.FindFirstValue("picture"),
                            user.FindAll("roles").Select(x => x.Value).Distinct().Order().ToArray(),
                            antiforgery.GetAndStoreTokens(context).RequestToken!
                        )
                    );
                }
            )
            .Produces<SessionResponse>()
            .Produces(StatusCodes.Status401Unauthorized);
        app.MapPost(
                "/auth/logout",
                () =>
                    Results.SignOut(
                        new AuthenticationProperties { RedirectUri = "/" },
                        [CookieScheme, OidcScheme]
                    )
            )
            .RequireAuthorization();
        // Reserve the auth namespace, including the old, unsafe GET logout URL.
        app.Map("/auth/{**path}", () => Results.NotFound());
    }

    internal static string LocalReturnUrl(string? value) =>
        !string.IsNullOrEmpty(value)
        && value[0] == '/'
        && !value.StartsWith("//")
        && !value.Contains('\\')
        && !value.Any(char.IsControl)
            ? value
            : "/app";
}

/// <summary>Public session profile; OAuth tokens are kept exclusively on the server.</summary>
public sealed record SessionResponse(
    string Id,
    string? Username,
    string? Email,
    string? Name,
    string? Avatar,
    string[] Roles,
    string CsrfToken
);
