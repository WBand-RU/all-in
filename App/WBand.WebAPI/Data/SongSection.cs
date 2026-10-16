using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>
/// A named text section for lyrics and chords. Sections have an independent
/// lifecycle so the song row contains metadata only.
/// </summary>
public sealed class SongSection
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Lyrics { get; set; }
    public string? Chords { get; set; }
    public long Revision { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Song this text section belongs to.</summary>
    [JsonIgnore]
    public Song Song { get; set; } = null!;

    /// <summary>User who created the section.</summary>
    [JsonIgnore]
    public User CreatedByUser { get; set; } = null!;

    /// <summary>User who last updated the section, when recorded.</summary>
    [JsonIgnore]
    public User? UpdatedByUser { get; set; }
}
