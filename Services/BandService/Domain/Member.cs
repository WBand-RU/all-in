namespace BandService.Domain;

public sealed class Member
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required string Email { get; set; }

    public required Guid BandId { get; set; }
    public Band Band { get; set; } = null!;

    public required MemberRole Role { get; set; }

    public required DateTime JoinedAt { get; set; }

    public Permission Permissions { get; set; } = Permission.None;
}
