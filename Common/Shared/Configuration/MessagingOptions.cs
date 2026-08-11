using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Shared.Configuration;

/// <summary>
/// Defines the primary RabbitMQ connection used by Wolverine.
/// </summary>
public sealed class MessagingOptions
{
    public const string SectionName = "RabbitMQ";

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

    /// <summary>
    /// Creates validated messaging options from application settings with environment fallbacks.
    /// </summary>
    public static MessagingOptions FromConfiguration(IConfiguration configuration)
    {
        return new MessagingOptions
        {
            Host = ConfigurationValue.GetRequired(
                configuration,
                $"{SectionName}:Host",
                "MESSAGING_HOST"
            ),
            Port = ushort.Parse(
                ConfigurationValue.GetRequired(
                    configuration,
                    $"{SectionName}:Port",
                    "MESSAGING_PORT"
                )
            ),
            Username = ConfigurationValue.GetRequired(
                configuration,
                $"{SectionName}:Username",
                "MESSAGING_USERNAME"
            ),
            Password = ConfigurationValue.GetRequired(
                configuration,
                $"{SectionName}:Password",
                "MESSAGING_PASSWORD"
            ),
            VirtualHost = ConfigurationValue.GetRequired(
                configuration,
                $"{SectionName}:VirtualHost",
                "MESSAGING_VIRTUAL_HOST"
            ),
        };
    }
}
