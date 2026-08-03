namespace BandService.Domain;

public sealed class Invitation
{
    public required Guid Id { get; set; }

    public required Guid InviterId { get; set; }

    public required string InviterEmail { get; set; }

    public required string InviteeEmail { get; set; }

    public required Guid BandId { get; set; }
    public Band Band { get; set; } = null!;

    public required MemberRole Role { get; set; }

    public required InvitationStatus Status { get; set; }
}
