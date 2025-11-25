namespace Shared;

public interface IAppSettings
{
    string ConnectionString { get; }

    string AllowedOrigins { get; }

    string JwtAuthority { get; }

    string JwtAudience { get; }

    string JwtMetadata { get; }

    string KeycloakClientId { get; }

    string KeycloakClientSecret { get; }

    string KeycloakIssuer { get; }
    
    string KeycloakUserAdminClientId { get; }

    string KeycloakUserAdminClientSecret { get; }
}
