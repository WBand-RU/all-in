using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>
/// A band whose memberships define access to its content.
/// </summary>
public sealed class Band
{
    public Guid Id { get; init; }
    public required string Name { get; set; }
    public Guid CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>User who created the band.</summary>
    [JsonIgnore]
    public User CreatedByUser { get; set; } = null!;

    /// <summary>Active members of the band.</summary>
    [JsonIgnore]
    public ICollection<BandMember> Members { get; set; } = [];

    /// <summary>Invitations issued for the band.</summary>
    [JsonIgnore]
    public ICollection<BandInvitation> Invitations { get; set; } = [];

    /// <summary>Recorded departures from the band.</summary>
    [JsonIgnore]
    public ICollection<BandDeparture> Departures { get; set; } = [];

    /// <summary>Songs owned by the band.</summary>
    [JsonIgnore]
    public ICollection<Song> Songs { get; set; } = [];
}
