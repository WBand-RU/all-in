using SongService.Endpoints.Songs.CreateSong;
using SongService.Endpoints.Songs.DeleteSong;
using SongService.Endpoints.Songs.GetListOfSongs;
using SongService.Endpoints.Songs.GetSong;
using SongService.Endpoints.Songs.UpdateSong;

namespace SongService.Endpoints.Songs;

/// <summary>
/// Mapper for Song endpoints registration
/// </summary>
public sealed class SongMapper
{
    public void Register(IEndpointRouteBuilder builder)
    {
        var group = builder
            .MapGroup("songs")
            .WithTags("Songs")
            .WithDisplayName("Songs")
            .RequireAuthorization();

        CreateSongEndpoint.Build(group);
        GetSongEndpoint.Build(group);
        GetListOfSongsEndpoint.Build(group);
        UpdateSongEndpoint.Build(group);
        DeleteSongEndpoint.Build(group);
    }
}
