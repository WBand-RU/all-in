namespace WBand.Dev;

public sealed record ServiceInfo(
    string ServiceName,
    string Filename,
    IEnumerable<string> Arguments,
    string? HealthUri = null,
    bool Health = false
);
