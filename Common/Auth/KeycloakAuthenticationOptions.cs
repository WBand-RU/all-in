using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Auth;

/// <summary>
/// Defines the Keycloak realm used to authenticate WBand API requests.
/// </summary>
public sealed class KeycloakAuthenticationOptions
{
    public const string SectionName = "Keycloak";

    [Required, Url]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string Realm { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool SslRequired { get; set; }

    /// <summary>
    /// Reads production environment variables first and falls back to application settings.
    /// </summary>
    public static KeycloakAuthenticationOptions FromConfiguration(IConfiguration configuration)
    {
        return new KeycloakAuthenticationOptions
        {
            Url = GetRequired(configuration, "Url", "KEYCLOAK_URL"),
            Realm = GetRequired(configuration, "Realm", "KEYCLOAK_REALM"),
            ClientId = GetRequired(configuration, "ClientId", "KEYCLOAK_CLIENT_ID"),
            ClientSecret =
                configuration["KEYCLOAK_CLIENT_SECRET"]
                ?? configuration[$"{SectionName}:ClientSecret"]
                ?? string.Empty,
            SslRequired = bool.Parse(
                GetRequired(configuration, "SslRequired", "KEYCLOAK_SSL_REQUIRED")
            ),
        };
    }

    private static string GetRequired(
        IConfiguration configuration,
        string optionName,
        string environmentVariable
    )
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        value = configuration[$"{SectionName}:{optionName}"];
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Configuration '{SectionName}:{optionName}' or environment variable "
                    + $"'{environmentVariable}' is required."
            );
    }
}
