using PlaylistService.Endpoints.Playlists.CreatePlaylist;
using PlaylistService.Endpoints.Playlists.GetPlaylist;

namespace PlaylistService.Endpoints.Playlists;

/// <summary>
/// Mapper for Playlist endpoints registration
/// </summary>
public sealed class PlaylistMapper
{
    public void Register(WebApplication app)
    {
        CreatePlaylistEndpoint.Build(app);
        GetPlaylistEndpoint.Build(app);
        // Additional endpoints can be added here
    }
}
