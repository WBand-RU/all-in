using Keycloak.AuthServices.Authorization;
using Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        builder.Services.AddKeycloakWebApiAuthentication(options =>
        {
            options.AuthServerUrl = keycloak.Url;
            options.Realm = keycloak.Realm;
            options.Resource = keycloak.ClientId;
            options.Credentials.Secret = keycloak.ClientSecret;
            options.SslRequired = keycloak.SslRequired ? "external" : "none";
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(Roles.SuperAdmin, policy => policy.RequireRole(Roles.SuperAdmin));
            options.AddPolicy(
                Roles.Moderator,
                policy => policy.RequireRole(Roles.SuperAdmin, Roles.Moderator)
            );
        })
            .AddKeycloakAuthorization(options =>
            {
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
