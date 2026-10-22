using System.Text.Json.Serialization;

namespace WBand.Modules.BandModule.Data;

/// <summary>
/// Distinguishes voluntary departures from removal by an owner.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BandDepartureReason
{
    Voluntary,
    RemovedByOwner,
}
