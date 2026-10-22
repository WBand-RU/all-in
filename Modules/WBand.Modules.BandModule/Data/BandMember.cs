namespace WBand.Modules.BandModule.Data;

/// <summary>
/// An active membership identified by the user's Keycloak subject.
/// </summary>
public sealed class BandMember
{
    public Guid Id { get; init; }
    public Guid BandId { get; init; }
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public BandMemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; init; }
}
