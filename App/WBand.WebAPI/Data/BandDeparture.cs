using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>
/// Records a departure and whether the member has subsequently rejoined.
/// </summary>
public sealed class BandDeparture
{
    public Guid Id { get; init; }
    public Guid BandId { get; init; }
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public BandDepartureReason Reason { get; init; }
    public DateTimeOffset LeftAt { get; init; }
    public DateTimeOffset? RejoinedAt { get; set; }

    /// <summary>Band the user departed from.</summary>
    [JsonIgnore]
    public Band Band { get; set; } = null!;

    /// <summary>User whose departure was recorded.</summary>
    [JsonIgnore]
    public User User { get; set; } = null!;
}
