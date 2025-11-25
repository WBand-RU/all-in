using Auth.Utils.ClaimTransformations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Auth;

public static class HostExtensions
{
    public static IHostApplicationBuilder AddKeycloakAuthentication(
        this IHostApplicationBuilder builder
    )
    {
        builder
            .Services.AddAuthentication()
            .AddKeycloakJwtBearer(
                serviceName: "keycloak",
                realm: "wband",
                options =>
                {
                    Console.WriteLine($"Audience: {options.Audience}");

                    options.MetadataAddress =
                        options.Authority + "/.well-known/openid-configuration";

                    options.Audience = "account";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudiences = [options.Audience],
                        NameClaimType = "preferred_username",
                    };

                    if (builder.Environment.IsDevelopment())
                    {
                        options.RequireHttpsMetadata = false;
                    }
                    else
                    {
                        options.Authority = "https://your-keycloak-server.com/realms/MyRealm";
                    }
                }
            );

        builder.Services.AddAuthorizationBuilder();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSingleton<IClaimsTransformation, RoleClaimsTransformation>();

        return builder;
    }
}
