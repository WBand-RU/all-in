using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlaybackService.Domain;

/// <summary>
/// Represents a multitrack playback for a song
/// </summary>
public sealed class Playback
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("band_id")]
    public required string BandId { get; set; }

    [BsonElement("song_id")]
    public required string SongId { get; set; }

    [BsonElement("name")]
    public required string Name { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("tracks")]
    public List<PlaybackTrack> Tracks { get; set; } = new();

    [BsonElement("presets")]
    public List<MixerPreset> Presets { get; set; } = new();

    [BsonElement("duration_seconds")]
    public double DurationSeconds { get; set; }

    [BsonElement("created_at")]
    public required DateTime CreatedAt { get; set; }

    [BsonElement("created_by")]
    public required string CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("updated_by")]
    public string? UpdatedBy { get; set; }
}
