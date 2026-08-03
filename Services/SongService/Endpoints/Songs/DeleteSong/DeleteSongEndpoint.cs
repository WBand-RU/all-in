using MongoDB.Driver;
using Shared;
using Shared.Services;
using SongService.Services;

namespace SongService.Endpoints.Songs.DeleteSong;

/// <summary>
/// Endpoint for deleting a song
/// </summary>
internal sealed class DeleteSongEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/{songId}", Handle).WithName("DeleteSong");
    }

    public static async Task<ApiResponse<bool>> Handle(
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
            return ApiResponse<bool>.Error(ApiCodes.NotFound);
        }

        // TODO: Verify user has permission to delete songs in this band
        // This will be implemented with the permission system

        await repository.Songs.DeleteOneAsync(s => s.Id == songId, cancellationToken);

        return ApiResponse<bool>.Success(true);
    }
}
