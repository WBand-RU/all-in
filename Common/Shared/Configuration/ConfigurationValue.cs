using Microsoft.Extensions.Configuration;

namespace Shared.Configuration;

/// <summary>
/// Reads a production environment variable and falls back to application configuration.
/// </summary>
public static class ConfigurationValue
{
    /// <summary>
    /// Returns an environment variable or a configured fallback value.
    /// </summary>
    public static string GetRequired(
        IConfiguration configuration,
        string configurationKey,
        string environmentVariable
    )
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        value = configuration[configurationKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"Configuration '{configurationKey}' or environment variable "
                + $"'{environmentVariable}' is required."
        );
    }

    /// <summary>
    /// Returns an optional environment variable or a configured fallback value.
    /// </summary>
    public static string? GetOptional(
        IConfiguration configuration,
        string configurationKey,
        string environmentVariable
    )
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : configuration[configurationKey];
    }
}
