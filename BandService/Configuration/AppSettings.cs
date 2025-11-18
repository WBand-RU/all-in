using Shared;

namespace BandService.Configuration;

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
}
