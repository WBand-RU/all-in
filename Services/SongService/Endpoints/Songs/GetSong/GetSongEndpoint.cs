using MongoDB.Driver;
using Shared;
using SongService.Domain;
using SongService.Services;

namespace SongService.Endpoints.Songs.GetSong;

/// <summary>
/// Endpoint for retrieving a single song by ID
/// </summary>
internal sealed class GetSongEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{songId}", Handle).WithName("GetSong");
    }

    public static async Task<ApiResponse<Song>> Handle(
        string songId,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var song = await repository
            .Songs.Find(s => s.Id == songId)
            .FirstOrDefaultAsync(cancellationToken);

        if (song == null)
        {
            return ApiResponse<Song>.Error(ApiCodes.NotFound);
        }

        // TODO: Verify user has permission to view songs in this band
        // This will be implemented with the permission system

        return ApiResponse<Song>.Success(song);
    }
}
