using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlaybackService.Domain;

/// <summary>
/// Represents a mixer preset for a song
/// </summary>
public sealed class MixerPreset
{
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("name")]
    public required string Name { get; set; }

    [BsonElement("song_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string SongId { get; set; }

    [BsonElement("output")]
    public TrackPreset Output { get; set; } = new();

    [BsonElement("tracks")]
    public List<TrackPreset> Tracks { get; set; } = [];
}
