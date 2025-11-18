using MongoDB.Bson.Serialization.Attributes;

namespace PlaybackService.Domain;

/// <summary>
/// Represents an individual track in a multitrack playback
/// </summary>
public sealed class PlaybackTrack
{
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("name")]
    public required string Name { get; set; }

    [BsonElement("instrument")]
    public string? Instrument { get; set; }

    [BsonElement("file_key")]
    public required string FileKey { get; set; }

    [BsonElement("volume")]
    public float Volume { get; set; } = 0.75f;

    [BsonElement("pan")]
    public float Pan { get; set; } = 0.0f;

    [BsonElement("muted")]
    public bool Muted { get; set; } = false;

    [BsonElement("solo")]
    public bool Solo { get; set; } = false;
}
