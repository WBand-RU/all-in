namespace WBand.Modules.BandModule.Data;

/// <summary>
/// An invitation addressed to a normalized email, valid until its expiry.
/// </summary>
public sealed class BandInvitation
{
    public Guid Id { get; init; }
    public Guid BandId { get; init; }
    public Guid InviterId { get; init; }
    public required string InviterEmail { get; init; }
    public required string InviteeEmail { get; init; }
    public BandMemberRole Role { get; init; } = BandMemberRole.Member;
    public InvitationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RespondedAt { get; set; }
}
