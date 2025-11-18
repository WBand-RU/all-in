using Microsoft.AspNetCore.Mvc;

using MongoDB.Bson;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;

namespace PlaybackService.Endpoints.Playbacks.UploadPlayback;

/// <summary>
/// Endpoint for uploading a playback with tracks
/// </summary>
internal sealed class UploadPlaybackEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/playbacks/upload", Handle)
            .WithName("UploadPlayback")
            .WithTags("Playbacks")
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    public static async Task<ApiResponse<Playback>> Handle(
        IFormFileCollection files,
        [FromForm] string bandId,
        [FromForm] string songId,
        [FromForm] string name,
        [FromForm] string? description,
        ICurrentUser currentUser,
        Repository repository,
        MinioStorageService storageService,
        CancellationToken cancellationToken
    )
    {
        if (files.Count == 0)
        {
            return ApiResponse<Playback>.Error(ApiCodes.ValidationFailed);
        }

        // TODO: Verify user has permission to upload playbacks for this band

        var playback = new Playback
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BandId = bandId,
            SongId = songId,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.GetUserId,
        };

        // Upload each track file to MinIO
        foreach (var file in files)
        {
            using var stream = file.OpenReadStream();
            var fileKey = await storageService.UploadFileAsync(
                stream,
                file.FileName,
                file.ContentType ?? "audio/mpeg"
            );

            var track = new PlaybackTrack
            {
                Id = Guid.NewGuid().ToString(),
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FileKey = fileKey,
            };

            playback.Tracks.Add(track);
        }

        // Add default preset with initial mixer settings
        var defaultPreset = new MixerPreset { Id = Guid.NewGuid().ToString(), Name = "Default" };

        foreach (var track in playback.Tracks)
        {
            defaultPreset.TrackSettings[track.Id] = new TrackSettings
            {
                Volume = 0.75f,
                Pan = 0.0f,
                Muted = false,
                Solo = false,
            };
        }

        playback.Presets.Add(defaultPreset);

        await repository.Playbacks.InsertOneAsync(playback, cancellationToken: cancellationToken);

        return ApiResponse<Playback>.Success(playback);
    }
}
