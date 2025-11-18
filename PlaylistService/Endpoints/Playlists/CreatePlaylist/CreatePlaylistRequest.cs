using PlaylistService.Domain;

namespace PlaylistService.Endpoints.Playlists.CreatePlaylist;

public sealed record CreatePlaylistRequest(
    string BandId,
    string Name,
    string? Description,
    DateTime? PlannedDate,
    List<PlaylistItemRequest>? Items
);

public sealed record PlaylistItemRequest(
    PlaylistItemType Type,
    string? SongId,
    string? CustomKey,
    string? Notes,
    int DurationMinutes,
    string? BlockTitle
);
