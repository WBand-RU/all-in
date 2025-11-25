using MongoDB.Bson.Serialization.Attributes;

namespace PlaybackService.Domain;

/// <summary>
/// Mixer settings for a track preset
/// </summary>
public sealed class TrackPreset
{
    [BsonElement("volume")]
    public float Volume { get; set; } = 1.0f;

    [BsonElement("pan")]
    public float Pan { get; set; } = 0.0f;

    [BsonElement("muted")]
    public bool Muted { get; set; } = false;

    [BsonElement("solo")]
    public bool Solo { get; set; } = false;
}
