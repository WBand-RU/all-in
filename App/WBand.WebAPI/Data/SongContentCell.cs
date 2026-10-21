using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>Text at the intersection of one shared song section and one partition.</summary>
public sealed class SongContentCell
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Guid SectionId { get; set; }
    public Guid PartitionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long Revision { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>The shared row in the song matrix.</summary>
    [JsonIgnore]
    public SongSection Section { get; set; } = null!;

    /// <summary>The column in the song matrix.</summary>
    [JsonIgnore]
    public Partition Partition { get; set; } = null!;
}
