namespace WBand.Modules.UserModule.Domain;

public sealed class UserProfile
{
    public Guid Id { get; init; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset LastLoginAt { get; set; }
}
