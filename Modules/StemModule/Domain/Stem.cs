using System.Text.Json.Serialization;

namespace WBand.Modules.StemModule.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StemKind
{
    Backing,
    Vocal,
    Guitar,
    Bass,
    Drums,
    Keys,
    Click,
    Guide,
    Other,
}

public sealed class Stem
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Guid BandId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid FileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public StemKind Kind { get; set; }
    public int Order { get; set; }
    public int Version { get; set; } = 1;
    public long Revision { get; set; } = 1;
    public int? DurationMilliseconds { get; set; }
    public int? SampleRateHz { get; set; }
    public int? Channels { get; set; }
    public int? BitDepth { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public sealed class StemGroup
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Guid BandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public sealed class StemCollection
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public Guid BandId { get; set; }
    public long ContentVersion { get; set; } = 1;
}
