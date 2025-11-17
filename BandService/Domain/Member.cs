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

    [BsonElement("band_id")]
    public required string BandId { get; set; }

    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public required MemberRole Role { get; set; }
}
