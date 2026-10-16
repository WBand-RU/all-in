using System.Text.Json.Serialization;

namespace WBand.WebAPI.Shared;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApiCodes
{
    Success,
    ValidationFailed,
    Forbidden,
    NotFound,
    Conflict,
}
