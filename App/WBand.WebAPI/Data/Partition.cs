using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>
/// A song-wide matrix column, such as lyrics, chords or musician notes.
/// </summary>
public sealed class Partition
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public long Revision { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Content at intersections with shared song sections.</summary>
    [JsonIgnore]
    public ICollection<SongContentCell> Cells { get; set; } = [];

    /// <summary>Song this partition belongs to.</summary>
    [JsonIgnore]
    public Song Song { get; set; } = null!;
}
