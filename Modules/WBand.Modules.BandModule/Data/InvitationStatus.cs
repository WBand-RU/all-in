using System.Text.Json.Serialization;

namespace WBand.Modules.BandModule.Data;

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
