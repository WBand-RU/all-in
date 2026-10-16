using System.ComponentModel.DataAnnotations;

namespace WBand.WebAPI.Configuration;

public sealed class WebApiConfiguration
{
    [ConfigurationKeyName("RABBITMQ_HOST"), Required]
    public required string RABBITMQ_HOST { get; init; }

    [ConfigurationKeyName("RABBITMQ_PORT"), Required]
    public required string RABBITMQ_PORT { get; init; }

    [ConfigurationKeyName("RABBITMQ_USERNAME"), Required]
    public required string RABBITMQ_USERNAME { get; init; }

    [ConfigurationKeyName("RABBITMQ_PASSWORD"), Required]
    public required string RABBITMQ_PASSWORD { get; init; }

    [ConfigurationKeyName("RABBITMQ_VIRTUAL_HOST"), Required]
    public required string RABBITMQ_VIRTUAL_HOST { get; init; }

    [ConfigurationKeyName("RABBITMQ_SSL_ENABLED"), Required]
    public required string RABBITMQ_SSL_ENABLED { get; init; }

    [ConfigurationKeyName("KEYCLOAK_EVENTS_EXCHANGE"), Required]
    public required string KEYCLOAK_EVENTS_EXCHANGE { get; init; }

    [ConfigurationKeyName("KEYCLOAK_EVENTS_QUEUE"), Required]
    public required string KEYCLOAK_EVENTS_QUEUE { get; init; }

    [ConfigurationKeyName("KEYCLOAK_URI"), Required]
    public required string KEYCLOAK_URI { get; init; }

    [ConfigurationKeyName("KEYCLOAK_REALM"), Required]
    public required string KEYCLOAK_REALM { get; init; }

    [ConfigurationKeyName("KEYCLOAK_CLIENT_ID"), Required]
    public required string KEYCLOAK_CLIENT_ID { get; init; }

    [ConfigurationKeyName("KEYCLOAK_CLIENT_SECRET"), Required]
    public required string KEYCLOAK_CLIENT_SECRET { get; init; }

    [ConfigurationKeyName("KEYCLOAK_SSL_REQUIRED"), Required]
    public required string KEYCLOAK_SSL_REQUIRED { get; init; }

    [ConfigurationKeyName("DATABASE_CONNECTION_STRING"), Required]
    public required string DatabaseConnectionString { get; init; }
}
