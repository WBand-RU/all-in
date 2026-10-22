namespace WBand.Modules.BandModule.Data;

/// <summary>A local profile identified by the Keycloak subject.</summary>
public sealed class User
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public required Guid KeycloakId { get; init; }
    public required string Email { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required DateTimeOffset LastLoginAt { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
