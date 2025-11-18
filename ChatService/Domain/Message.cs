using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatService.Domain;

/// <summary>
/// Represents a chat message in a band
/// </summary>
public sealed class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("band_id")]
    public required string BandId { get; set; }

    [BsonElement("sender_id")]
    public required string SenderId { get; set; }

    [BsonElement("sender_name")]
    public required string SenderName { get; set; }

    [BsonElement("content")]
    public required string Content { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public MessageType Type { get; set; } = MessageType.Text;

    [BsonElement("attachments")]
    public List<MessageAttachment>? Attachments { get; set; }

    [BsonElement("reply_to")]
    public string? ReplyToId { get; set; }

    [BsonElement("edited")]
    public bool IsEdited { get; set; } = false;

    [BsonElement("deleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("edited_at")]
    public DateTime? EditedAt { get; set; }

    [BsonElement("reactions")]
    public List<MessageReaction>? Reactions { get; set; }
}

public enum MessageType
{
    Text,
    Voice,
    File,
    System,
}

public sealed class MessageAttachment
{
    [BsonElement("file_url")]
    public required string FileUrl { get; set; }

    [BsonElement("file_name")]
    public required string FileName { get; set; }

    [BsonElement("file_size")]
    public long FileSize { get; set; }

    [BsonElement("content_type")]
    public string? ContentType { get; set; }
}

public sealed class MessageReaction
{
    [BsonElement("emoji")]
    public required string Emoji { get; set; }

    [BsonElement("user_id")]
    public required string UserId { get; set; }

    [BsonElement("user_name")]
    public required string UserName { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
