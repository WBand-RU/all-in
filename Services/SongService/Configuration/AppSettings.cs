using WBand.ServiceDefaults;

namespace SongService.Configuration;

public sealed class AppSettings
{
    [ConfigurationKeyName("KEYCLOAK_CLIENT_ID")]
    public required string KeycloakClientId { get; init; }

    [ConfigurationKeyName("KEYCLOAK_CLIENT_SECRET")]
    public required string KeycloakClientSecret { get; init; }

    [ConfigurationKeyName("KEYCLOAK_URL")]
    public required string KeycloakUrl { get; init; }

    [ConfigurationKeyName("KEYCLOAK_REALM")]
    public required string KeycloakRealm { get; init; }

    [ConfigurationKeyName(EnvironmentVariablesConstants.MessageQueueHost)]
    public required string MessageQueueHost { get; init; }

    [ConfigurationKeyName(EnvironmentVariablesConstants.MessageQueuePort)]
    public required string MessageQueuePort { get; init; }

    [ConfigurationKeyName(EnvironmentVariablesConstants.MessageQueueUsername)]
    public required string MessageQueueUsername { get; init; }

    [ConfigurationKeyName(EnvironmentVariablesConstants.MessageQueuePassword)]
    public required string MessageQueuePassword { get; init; }

    [ConfigurationKeyName(EnvironmentVariablesConstants.MartenDatabaseConnectionString)]
    public required string MartenDatabaseConnectionString { get; init; }

    [ConfigurationKeyName(EnvironmentVariablesConstants.MartenDatabaseSchemaName)]
    public required string MartenDatabaseSchemaName { get; init; }
}
