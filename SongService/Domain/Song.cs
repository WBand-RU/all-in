using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SongService.Domain;

/// <summary>
/// Represents a worship song with its metadata
/// </summary>
public sealed class Song
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("band_id")]
    public required string BandId { get; set; }

    [BsonElement("title")]
    public required string Title { get; set; }

    [BsonElement("author")]
    public string? Author { get; set; }

    [BsonElement("lyrics")]
    public string? Lyrics { get; set; }

    [BsonElement("chords")]
    public string? Chords { get; set; }

    [BsonElement("key")]
    public string? Key { get; set; }

    [BsonElement("bpm")]
    public int? Bpm { get; set; }

    [BsonElement("created_at")]
    public required DateTime CreatedAt { get; set; }

    [BsonElement("created_by")]
    public required string CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("updated_by")]
    public string? UpdatedBy { get; set; }
}
