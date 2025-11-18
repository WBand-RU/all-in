using PlaylistService.Endpoints.Playlists.CreatePlaylist;

namespace PlaylistService.Endpoints.Playlists.UpdatePlaylist;

public sealed record UpdatePlaylistRequest(
    string Name,
    string? Description,
    DateTime? PlannedDate,
    List<PlaylistItemRequest>? Items
);
