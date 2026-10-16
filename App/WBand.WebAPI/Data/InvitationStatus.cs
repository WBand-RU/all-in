using System.Text.Json.Serialization;

namespace WBand.WebAPI.Data;

/// <summary>
/// Tracks the lifecycle of an invitation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Revoked,
    Expired,
}
