using System.Security.Claims;
using Keycloak.AuthServices.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;

namespace Auth;

/// <summary>
/// Registers Keycloak authentication and authorization for the API host.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Adds Keycloak authentication using validated application configuration.
    /// </summary>
    public static IHostApplicationBuilder AddKeycloakAuthentication(
        this IHostApplicationBuilder builder
    )
    {
        var keycloak = KeycloakAuthenticationOptions.FromConfiguration(builder.Configuration);

        builder
            .Services.AddOptions<KeycloakAuthenticationOptions>()
            .Configure(options => Copy(keycloak, options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddKeycloakWebApiAuthentication(
            options =>
            {
                options.AuthServerUrl = keycloak.Url;
                options.Realm = keycloak.Realm;
                options.Resource = keycloak.ClientId;
                options.VerifyTokenAudience = true;
                options.Credentials.Secret = keycloak.ClientSecret;
                options.SslRequired = keycloak.SslRequired ? "external" : "none";
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
                options.AuthServerUrl = keycloak.Url;
                options.Realm = keycloak.Realm;
                options.Resource = keycloak.ClientId;
                options.Credentials.Secret = keycloak.ClientSecret;
                options.SslRequired = keycloak.SslRequired ? "external" : "none";
            });

        return builder;
    }

    private static void Copy(
        KeycloakAuthenticationOptions source,
        KeycloakAuthenticationOptions target
    )
    {
        target.Url = source.Url;
        target.Realm = source.Realm;
        target.ClientId = source.ClientId;
        target.ClientSecret = source.ClientSecret;
        target.SslRequired = source.SslRequired;
    }
}
