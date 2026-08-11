using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Shared.Configuration;

namespace WBand.Modules.UserModule.Infrastructure;

/// <summary>
/// Defines the external RabbitMQ source populated by the Keycloak event listener plugin.
/// </summary>
internal sealed class KeycloakEventsOptions
{
    public const string SectionName = "Modules:UserModule:KeycloakEvents";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, ushort.MaxValue)]
    public ushort Port { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string VirtualHost { get; set; } = string.Empty;

    public bool SslEnabled { get; set; }

    [Required]
    public string Realm { get; set; } = string.Empty;

    [Required]
    public string Exchange { get; set; } = string.Empty;

    [Required]
    public string Queue { get; set; } = string.Empty;

    /// <summary>
    /// Gets the topic used by the Keycloak event listener plugin for successful client events.
    /// </summary>
    public string RoutingKey => $"KK.EVENT.CLIENT.{Realm}.SUCCESS.#";

    /// <summary>
    /// Reads development settings first and falls back to production environment variables.
    /// </summary>
    public static KeycloakEventsOptions FromConfiguration(IConfiguration configuration)
    {
        return new KeycloakEventsOptions
        {
            Host = GetRequired(configuration, "Host", "WBAND_KEYCLOAK_EVENTS_HOST"),
            Port = ushort.Parse(
                GetRequired(configuration, "Port", "WBAND_KEYCLOAK_EVENTS_PORT")
            ),
            Username = GetRequired(
                configuration,
                "Username",
                "WBAND_KEYCLOAK_EVENTS_USERNAME"
            ),
            Password = GetRequired(
                configuration,
                "Password",
                "WBAND_KEYCLOAK_EVENTS_PASSWORD"
            ),
            VirtualHost = GetRequired(
                configuration,
                "VirtualHost",
                "WBAND_KEYCLOAK_EVENTS_VIRTUAL_HOST"
            ),
            SslEnabled = bool.Parse(
                GetRequired(configuration, "SslEnabled", "WBAND_KEYCLOAK_EVENTS_SSL_ENABLED")
            ),
            Realm = GetRequired(configuration, "Realm", "WBAND_KEYCLOAK_EVENTS_REALM"),
            Exchange = GetRequired(
                configuration,
                "Exchange",
                "WBAND_KEYCLOAK_EVENTS_EXCHANGE"
            ),
            Queue = GetRequired(configuration, "Queue", "WBAND_KEYCLOAK_EVENTS_QUEUE"),
        };
    }

    private static string GetRequired(
        IConfiguration configuration,
        string optionName,
        string environmentVariable
    ) =>
        ConfigurationValue.GetRequired(
            configuration,
            $"{SectionName}:{optionName}",
            environmentVariable
        );
}
