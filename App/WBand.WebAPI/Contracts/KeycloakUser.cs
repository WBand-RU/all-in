using System.Text.Json.Serialization;

namespace BandService.Contracts;

public sealed record KeycloakUser
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = "";

    [JsonPropertyName("email")]
    public string Email { get; init; } = "";

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("emailVerified")]
    public bool? EmailVerified { get; init; }

    [JsonPropertyName("credentials")]
    public List<KeycloakCredential>? Credentials { get; init; }
}
