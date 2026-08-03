using MongoDB.Driver;
using PlaylistService.Domain;
using PlaylistService.Services;
using Shared;
using Shared.Services;

namespace PlaylistService.Endpoints.Playlists.GetPlaylist;

/// <summary>
/// Endpoint for retrieving a single playlist by ID
/// </summary>
internal sealed class GetPlaylistEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/playlists/{playlistId}", Handle)
            .WithName("GetPlaylist")
            .WithTags("Playlists")
            .RequireAuthorization();
    }

    public static async Task<ApiResponse<Playlist>> Handle(
        string playlistId,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var playlist = await repository
            .Playlists.Find(p => p.Id == playlistId)
            .FirstOrDefaultAsync(cancellationToken);

        if (playlist == null)
        {
            return ApiResponse<Playlist>.Error(ApiCodes.NotFound);
        }

        // TODO: Verify user has permission to view playlists in this band

        return ApiResponse<Playlist>.Success(playlist);
    }
}
