using System.Text.Json.Serialization;

namespace BandService.Contracts;

public sealed record KeycloakCredential
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("value")]
    public string Value { get; init; } = "";

    [JsonPropertyName("temporary")]
    public bool Temporary { get; init; }
}
