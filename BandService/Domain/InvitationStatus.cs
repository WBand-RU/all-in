using System.Text.Json.Serialization;

namespace BandService.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
}
