using MongoDB.Bson.Serialization.Attributes;

namespace PlaylistService.Domain;

/// <summary>
/// Represents an item in a playlist (could be a song or a block/pause)
/// </summary>
public sealed class PlaylistItem
{
    [BsonElement("order")]
    public required int Order { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public required PlaylistItemType Type { get; set; }

    [BsonElement("song_id")]
    public string? SongId { get; set; }

    [BsonElement("custom_key")]
    public string? CustomKey { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("duration_minutes")]
    public int DurationMinutes { get; set; }

    [BsonElement("block_title")]
    public string? BlockTitle { get; set; }
}

public enum PlaylistItemType
{
    Song,
    Block,
    Pause,
}
