using MassTransit;
using MongoDB.Driver;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;
using Shared.Messages;
using Wolverine;

namespace PlaybackService.Endpoints.Tracks.Download;

/// <summary>
/// Endpoint for downloading a track file with an authorized MinIO link
/// </summary>
internal sealed class DownloadTrackEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("{trackId}/download", Handle).WithName("DownloadTrack");
    }

    public static async Task<ApiResponse<string>> Handle(
        string songId,
        string trackId,
        ICurrentUser currentUser,
        Repository repository,
        MinioStorageService minioStorage,
        IMessageBus messageBus,
        CancellationToken cancellationToken
    )
    {
        // Find the track
        var filter = Builders<Track>.Filter.And(
            Builders<Track>.Filter.Eq(t => t.Id, trackId),
            Builders<Track>.Filter.Eq(t => t.SongId, songId)
        );

        var track = await repository.Tracks.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (track is null)
        {
            return ApiResponse<string>.Error(ApiCodes.NotFound);
        }

        // Get the song's band ID to verify user access
        var songResponse = await messageBus.InvokeAsync<GetSongResponse>(
            new GetSongRequest(songId),
            cancellationToken
        );

        if (!songResponse.Success || songResponse.BandId is null)
        {
            return ApiResponse<string>.Error(ApiCodes.NotFound);
        }

        // Check if the user is a member of the band that owns the song
        var membershipResponse = await messageBus.InvokeAsync<CheckBandMembershipResponse>(
            new CheckBandMembershipRequest(currentUser.GetUserId, songResponse.BandId),
            cancellationToken
        );

        if (!membershipResponse.IsMember)
        {
            return ApiResponse<string>.Error(ApiCodes.Forbidden);
        }

        try
        {
            // Generate pre-signed URL for download (expires in 1 hour)
            var presignedUrl = await minioStorage.GetPresignedUrlAsync(track.FileKey, 3600);

            return ApiResponse<string>.Success(presignedUrl);
        }
        catch (Exception)
        {
            // For exceptions, we still need to return ApiResponse
            // Since we can't return a proper error response with exception details,
            // we'll return a generic error code. In a real implementation,
            // you might want to log the exception and return a proper error code.
            return ApiResponse<string>.Error(ApiCodes.ValidationFailed);
        }
    }
}
