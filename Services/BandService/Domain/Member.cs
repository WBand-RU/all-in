using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BandService.Domain;

public sealed class Member
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("user_id")]
    public required string UserId { get; set; }

    [BsonElement("email")]
    public required string Email { get; set; }

    [BsonElement("band_id")]
    public required string BandId { get; set; }

    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public required MemberRole Role { get; set; }

    [BsonElement("joined_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public required DateTime JoinedAt { get; set; }

    [BsonElement("permissions")]
    public Permission Permissions { get; set; } = Permission.None;
}
