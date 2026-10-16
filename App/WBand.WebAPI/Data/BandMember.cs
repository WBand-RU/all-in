using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

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

    /// <summary>Band this membership belongs to.</summary>
    [JsonIgnore]
    public Band Band { get; set; } = null!;

    /// <summary>User identified by the membership Keycloak subject.</summary>
    [JsonIgnore]
    public User User { get; set; } = null!;
}
