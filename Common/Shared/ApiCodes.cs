using System.Text.Json.Serialization;

namespace Shared;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApiCodes
{
    Success,
    ValidationFailed,
    Forbidden,
    NotFound,
    Conflict,
}
