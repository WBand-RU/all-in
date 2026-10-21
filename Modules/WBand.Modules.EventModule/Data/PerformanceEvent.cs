namespace WBand.Modules.EventModule.Data;

/// <summary>A band's performance or other event with an ordered setlist.</summary>
public sealed class PerformanceEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid BandId { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public string? Venue { get; set; }
    public string? Description { get; set; }
    public Guid[] SongIds { get; set; } = [];
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
