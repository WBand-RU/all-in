namespace WBand.Modules.SongModule.Domain;

public enum SongStatus { Draft, Band, Catalog }

public sealed record TempoChange(int Bar, int Bpm);
public sealed record TimeSignatureChange(int Bar, int Beats, int BeatUnit);
public sealed record SongSection(string Name, int StartBar, int EndBar);

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
    public List<SongSection> Sections { get; set; } = [];
    public string? Lyrics { get; set; }
    public string? Chords { get; set; }
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
    List<SongSection> Sections,
    string? Lyrics,
    string? Chords,
    SongStatus Status);
