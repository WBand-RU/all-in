using System.Text.Json.Serialization;

namespace WBand.Modules.UserModule.Infrastructure.Messaging;

/// <summary>
/// Represents a raw event emitted by the Keycloak RabbitMQ event listener plugin.
/// </summary>
public sealed record KeycloakEvent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    [JsonPropertyName("details")]
    public IReadOnlyDictionary<string, string> Details { get; init; } =
        new Dictionary<string, string>();
}
