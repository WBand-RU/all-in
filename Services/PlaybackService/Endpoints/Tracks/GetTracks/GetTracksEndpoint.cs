using MongoDB.Driver;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;

namespace PlaybackService.Endpoints.Tracks.GetTracks;

/// <summary>
/// Endpoint for retrieving all tracks for a specific song
/// </summary>
internal sealed class GetTracksEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", Handle).WithName("GetTracks");
    }

    public static async Task<ApiResponse<List<Track>>> Handle(
        string songId,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        // TODO: Verify user has permission to view tracks for this song
        // This will be implemented with the permission system

        var filter = Builders<Track>.Filter.Eq(t => t.SongId, songId);

        var tracks = await repository
            .Tracks.Find(filter)
            .Sort(Builders<Track>.Sort.Descending(t => t.CreatedAt))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<Track>>.Success(tracks);
    }
}
