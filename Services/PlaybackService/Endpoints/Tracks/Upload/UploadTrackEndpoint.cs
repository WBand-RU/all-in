using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;
using Shared.Services;

namespace PlaybackService.Endpoints.Tracks.Upload;

/// <summary>
/// Endpoint for uploading tracks for a song
/// </summary>
internal sealed class UploadTrackEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("", Handle).WithName("UploadTrack").DisableAntiforgery();
    }

    public static async Task<ApiResponse<List<Track>>> Handle(
        string songId,
        IFormFileCollection files,
        ICurrentUser currentUser,
        Repository repository,
        MinioStorageService storageService,
        CancellationToken cancellationToken
    )
    {
        if (files.Count == 0)
        {
            return ApiResponse<List<Track>>.Error(ApiCodes.ValidationFailed);
        }

        // TODO: Verify user has permission to upload tracks for this song

        var tracks = new List<Track>();

        // Upload each file as a separate Track document
        foreach (var file in files)
        {
            using var stream = file.OpenReadStream();
            var fileKey = await storageService.UploadFileAsync(
                stream,
                file.FileName,
                file.ContentType ?? "audio/mpeg"
            );

            var track = new Track
            {
                Id = ObjectId.GenerateNewId().ToString(),
                SongId = songId,
                Instrument = InferInstrumentFromFilename(file.FileName),
                FileKey = fileKey,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser.GetUserId,
            };

            tracks.Add(track);
        }

        await repository.Tracks.InsertManyAsync(tracks, cancellationToken: cancellationToken);

        return ApiResponse<List<Track>>.Success(tracks);
    }

    private static string InferInstrumentFromFilename(string filename)
    {
        var name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();

        if (
            name.Contains("drum")
            || name.Contains("kick")
            || name.Contains("snare")
            || name.Contains("cymbal")
            || name.Contains("perc")
        )
            return Track.Instruments.Drums;
        if (name.Contains("bass"))
            return Track.Instruments.Bass;
        if (name.Contains("guitar") || name.Contains("gtr"))
            return Track.Instruments.Guitar;
        if (name.Contains("key") || name.Contains("synth") || name.Contains("organ"))
            return Track.Instruments.Keys;
        if (name.Contains("vocal") || name.Contains("voice") || name.Contains("sing"))
            return Track.Instruments.Vocals;
        if (name.Contains("piano"))
            return Track.Instruments.Piano;
        if (name.Contains("string") || name.Contains("violin") || name.Contains("cello"))
            return Track.Instruments.Strings;
        if (name.Contains("brass") || name.Contains("trumpet") || name.Contains("horn"))
            return Track.Instruments.Brass;
        if (name.Contains("percussion"))
            return Track.Instruments.Percussion;

        return Track.Instruments.Other;
    }
}
