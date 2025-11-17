namespace BandService.Domain;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MemberRole
{
    Owner,
    Admin,
    Member,
}
