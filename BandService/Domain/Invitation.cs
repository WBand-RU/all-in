using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BandService.Domain;

public sealed class Invitation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("inviter_id")]
    public required string InviterId { get; set; }

    [BsonElement("inviter_email")]
    public required string InviterEmail { get; set; }

    [BsonElement("invitee_email")]
    public required string InviteeEmail { get; set; }

    [BsonElement("band_id")]
    public required string BandId { get; set; }

    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public required MemberRole Role { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public required InvitationStatus Status { get; set; }
}
