using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>An immutable snapshot of song metadata at a particular content version.</summary>
public sealed class SongRevision
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public long ContentVersion { get; set; }
    public string Title { get; set; } = string.Empty;
    public string[] Authors { get; set; } = [];
    public string? Key { get; set; }
    public int? Bpm { get; set; }
    public SongStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }

    /// <summary>Song whose metadata is captured by this revision.</summary>
    [JsonIgnore]
    public Song Song { get; set; } = null!;

    /// <summary>User who created the revision.</summary>
    [JsonIgnore]
    public User CreatedByUser { get; set; } = null!;
}
