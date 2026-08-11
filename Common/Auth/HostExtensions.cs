using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Auth;

public static class HostExtensions
{
    public static IHostApplicationBuilder AddKeycloakAuthentication(
        this IHostApplicationBuilder builder,
        string url,
        string realm,
        string clientId,
        string clientSecret,
        bool sslRequired
    )
    {
        builder.Services.AddKeycloakWebApiAuthentication(x =>
        {
            x.AuthServerUrl = url;
            x.Realm = realm;
            x.Resource = clientId;
            x.Credentials.Secret = clientSecret;
            x.SslRequired = sslRequired ? "external" : "none";
        });

        builder
            .Services.AddAuthorization()
            .AddKeycloakAuthorization(x =>
            {
                x.AuthServerUrl = url;
                x.Realm = realm;
                x.Resource = clientId;
                x.Credentials.Secret = clientSecret;
                x.SslRequired = sslRequired ? "external" : "none";
            });

        return builder;
    }

    public static void UseKeycloakAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
