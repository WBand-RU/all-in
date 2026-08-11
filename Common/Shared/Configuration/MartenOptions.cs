using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Shared.Configuration;

/// <summary>
/// Defines the Marten storage used by Wolverine inbox and outbox persistence.
/// </summary>
public sealed class MartenOptions
{
    public const string SectionName = "Marten";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Creates Marten options from application settings with environment fallbacks.
    /// </summary>
    public static MartenOptions FromConfiguration(IConfiguration configuration)
    {
        return new MartenOptions
        {
            ConnectionString = ConfigurationValue.GetRequired(
                configuration,
                $"{SectionName}:ConnectionString",
                "MARTEN_DATABASE_CONNECTION_STRING"
            ),
            SchemaName = ConfigurationValue.GetRequired(
                configuration,
                $"{SectionName}:SchemaName",
                "MARTEN_DATABASE_SCHEMA_NAME"
            ),
        };
    }
}
