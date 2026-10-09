using System.Text.Json.Serialization;

namespace WBand.Modules.BandModule.Domain;

public sealed class Band
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public Guid CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class BandMember
{
    public Guid Id { get; init; }
    public Guid BandId { get; init; }
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public BandMemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BandMemberRole
{
    Owner,
    Admin,
    Member,
}

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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Revoked,
    Expired,
}

public sealed class BandDeparture
{
    public Guid Id { get; init; }
    public Guid BandId { get; init; }
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public BandDepartureReason Reason { get; init; }
    public DateTimeOffset LeftAt { get; init; }
    public DateTimeOffset? RejoinedAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BandDepartureReason
{
    Voluntary,
    RemovedByOwner,
}
