using System.Text.Json.Serialization;

namespace BandService.Contracts;

public sealed record KeycloakRole
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}
