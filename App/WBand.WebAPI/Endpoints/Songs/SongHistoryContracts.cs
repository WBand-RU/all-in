using WBand.WebAPI.Data;

namespace WBand.WebAPI.Endpoints.Songs;

/// <summary>A historical metadata version and the member who saved it.</summary>
public sealed record SongVersionResponse(long ContentVersion, DateTimeOffset CreatedAt, Guid CreatedBy,
    string? ChangedByEmail, string? ChangedByRole);

/// <summary>An immutable metadata snapshot. Section text has its own revision.</summary>
public sealed record SongVersionDetailsResponse(long ContentVersion, DateTimeOffset CreatedAt, Guid CreatedBy,
    string Title, IReadOnlyList<string> Authors, string? Key, int? Bpm, SongStatus Status);
