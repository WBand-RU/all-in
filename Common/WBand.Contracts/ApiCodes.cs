using System.Text.Json.Serialization;

namespace WBand.Contracts;

/// <summary>Shared application status values serialized by name in HTTP responses.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApiCodes
{
    Success,
    ValidationFailed,
    Forbidden,
    NotFound,
    Conflict,
}
