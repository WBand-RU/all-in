namespace WBand.Modules.SongModule.Domain;

public enum SongStatus { Draft, Band, Catalog }

public sealed record TempoChange(int Bar, int Bpm);
public sealed record TimeSignatureChange(int Bar, int Beats, int BeatUnit);

public sealed class Song
{
    public Guid Id { get; set; }
    public Guid BandId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<string> Authors { get; set; } = [];
    public string? Key { get; set; }
    public int? Bpm { get; set; }
    public List<TempoChange> TempoTrack { get; set; } = [];
    public List<TimeSignatureChange> TimeSignatureTrack { get; set; } = [];
    public int CountInBars { get; set; } = 2;
    public SongStatus Status { get; set; } = SongStatus.Draft;
    public long ContentVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public bool IsDeleted => DeletedAt is not null;
    public DateTimeOffset? PurgeAfter => DeletedAt?.AddDays(30);
}

/// <summary>
/// A shared structural block for lyrics and chords. Sections have an independent
/// lifecycle so the song document contains metadata only.
/// </summary>
public sealed class SongSection
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Guid BandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int BarCount { get; set; } = 4;
    public string? Lyrics { get; set; }
    public string? Chords { get; set; }
    public long Revision { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public sealed class SongRevision
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public long ContentVersion { get; set; }
    public SongSnapshot Snapshot { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public sealed record SongSnapshot(
    string Title,
    List<string> Authors,
    string? Key,
    int? Bpm,
    List<TempoChange> TempoTrack,
    List<TimeSignatureChange> TimeSignatureTrack,
    int CountInBars,
    SongStatus Status);
