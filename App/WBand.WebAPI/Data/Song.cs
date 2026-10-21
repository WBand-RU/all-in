using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>The editorial status of a song.</summary>
public enum SongStatus { Draft, Band, Catalog }

/// <summary>Band-owned song metadata; content is a matrix of shared sections and named partitions.</summary>
public sealed class Song
{
    public Guid Id { get; set; }
    public Guid BandId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string[] Authors { get; set; } = [];
    public string? Key { get; set; }
    public int? Bpm { get; set; }
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

    /// <summary>Band that owns the song.</summary>
    [JsonIgnore]
    public Band Band { get; set; } = null!;

    /// <summary>User who created the song.</summary>
    [JsonIgnore]
    public User CreatedByUser { get; set; } = null!;

    /// <summary>User who last updated the song, when recorded.</summary>
    [JsonIgnore]
    public User? UpdatedByUser { get; set; }

    /// <summary>User who deleted the song, when recorded.</summary>
    [JsonIgnore]
    public User? DeletedByUser { get; set; }

    /// <summary>Ordered song sections shared by all partitions.</summary>
    [JsonIgnore]
    public ICollection<SongSection> Sections { get; set; } = [];

    /// <summary>Partitions belonging to the song.</summary>
    [JsonIgnore]
    public ICollection<Partition> Partitions { get; set; } = [];

    /// <summary>Metadata revision history of the song.</summary>
    [JsonIgnore]
    public ICollection<SongRevision> Revisions { get; set; } = [];
}
