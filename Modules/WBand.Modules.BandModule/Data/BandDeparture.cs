namespace WBand.Modules.BandModule.Data;

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
}
