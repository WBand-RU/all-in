using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Domain;

/// <summary>
/// Represents a notification for a user
/// </summary>
public sealed class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("user_id")]
    public required string UserId { get; set; }

    [BsonElement("band_id")]
    public string? BandId { get; set; }

    [BsonElement("title")]
    public required string Title { get; set; }

    [BsonElement("content")]
    public required string Content { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public NotificationType Type { get; set; }

    [BsonElement("severity")]
    [BsonRepresentation(BsonType.String)]
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    [BsonElement("read")]
    public bool IsRead { get; set; } = false;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("read_at")]
    public DateTime? ReadAt { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

public enum NotificationType
{
    System,
    BandInvite,
    NewSong,
    PlaylistUpdate,
    PlaybackStart,
    ChatMention,
    EventReminder,
    PermissionChange,
}

public enum NotificationSeverity
{
    Info,
    Warning,
    Error,
    Success,
}
