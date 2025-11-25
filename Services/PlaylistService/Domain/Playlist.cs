using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlaylistService.Domain;

/// <summary>
/// Represents a setlist/playlist of songs for worship services
/// </summary>
public sealed class Playlist
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("band_id")]
    public required string BandId { get; set; }

    [BsonElement("name")]
    public required string Name { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("items")]
    public List<PlaylistItem> Items { get; set; } = new();

    [BsonElement("duration_minutes")]
    public int DurationMinutes { get; set; }

    [BsonElement("planned_date")]
    public DateTime? PlannedDate { get; set; }

    [BsonElement("created_at")]
    public required DateTime CreatedAt { get; set; }

    [BsonElement("created_by")]
    public required string CreatedBy { get; set; }

    [BsonElement("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("updated_by")]
    public string? UpdatedBy { get; set; }
}
