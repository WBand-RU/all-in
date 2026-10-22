using System.Text.Json.Serialization;

namespace WBand.Modules.BandModule.Data;

/// <summary>
/// Defines the member's permissions within a band.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BandMemberRole
{
    Owner,
    Admin,
    Member,
}
