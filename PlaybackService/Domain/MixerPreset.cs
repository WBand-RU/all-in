using MongoDB.Bson.Serialization.Attributes;

namespace PlaybackService.Domain;

/// <summary>
/// Represents a mixer preset for a playback
/// </summary>
public sealed class MixerPreset
{
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("name")]
    public required string Name { get; set; }

    [BsonElement("track_settings")]
    public Dictionary<string, TrackSettings> TrackSettings { get; set; } = new();
}

public sealed class TrackSettings
{
    [BsonElement("volume")]
    public float Volume { get; set; }

    [BsonElement("pan")]
    public float Pan { get; set; }

    [BsonElement("muted")]
    public bool Muted { get; set; }

    [BsonElement("solo")]
    public bool Solo { get; set; }
}
