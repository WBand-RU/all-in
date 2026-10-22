using System.Security.Claims;
using Keycloak.AuthServices.Authorization;
using Microsoft.Extensions.Hosting;
using WBand.WebAPI.Services;
using WBand.WebAPI.Services.Implementations;
using WBand.WebAPI.Shared;

namespace WBand.WebAPI.Configuration;

/// <summary>Configures bearer validation and WBand identity requirements for the API.</summary>
public static class AuthenticationSetup
{
    /// <summary>Registers the API authentication and authorization policies.</summary>
    public static IHostApplicationBuilder AddWBandApiAuthentication(
        this IHostApplicationBuilder builder,
        WebApiConfiguration configuration
    )
    {
        builder.Services.AddKeycloakWebApiAuthentication(
            options =>
            {
                options.AuthServerUrl = configuration.KEYCLOAK_URI;
                options.Realm = configuration.KEYCLOAK_REALM;
                options.Resource = configuration.KEYCLOAK_CLIENT_ID;
                options.VerifyTokenAudience = true;
                options.Credentials.Secret = configuration.KEYCLOAK_CLIENT_SECRET;
                options.SslRequired = configuration.KEYCLOAK_SSL_REQUIRED.Equals(
                    "true",
                    StringComparison.CurrentCultureIgnoreCase
                )
                    ? "external"
                    : "none";
            },
            options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
                options.Events.OnTokenValidated = context =>
                {
                    var subject =
                        context.Principal?.FindFirstValue("sub")
                        ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var email =
                        context.Principal?.FindFirstValue("email")
                        ?? context.Principal?.FindFirstValue(ClaimTypes.Email);
                    if (!Guid.TryParse(subject, out _) || string.IsNullOrWhiteSpace(email))
                        context.Fail(
                            "The access token must identify a WBand user and contain an email."
                        );
                    return Task.CompletedTask;
                };
            }
        );

        builder
            .Services.AddAuthorization(options =>
            {
                options.FallbackPolicy =
                    new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                options.AddPolicy(Roles.SuperAdmin, policy => policy.RequireRole(Roles.SuperAdmin));
                options.AddPolicy(
                    Roles.Moderator,
                    policy => policy.RequireRole(Roles.SuperAdmin, Roles.Moderator)
                );
            })
            .AddKeycloakAuthorization(options =>
            {
                options.EnableRolesMapping = RolesClaimTransformationSource.Realm;
                options.AuthServerUrl = configuration.KEYCLOAK_URI;
                options.Realm = configuration.KEYCLOAK_REALM;
                options.Resource = configuration.KEYCLOAK_CLIENT_ID;
                options.Credentials.Secret = configuration.KEYCLOAK_CLIENT_SECRET;
                options.SslRequired = configuration.KEYCLOAK_SSL_REQUIRED.Equals(
                    "true",
                    StringComparison.InvariantCultureIgnoreCase
                )
                    ? "external"
                    : "none";
            });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        return builder;
    }
}
