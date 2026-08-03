using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;
using Shared.Services;

namespace PlaybackService.Endpoints.MixerPresets.CreateMixerPreset;

/// <summary>
/// Endpoint for creating a mixer preset
/// </summary>
internal sealed class CreateMixerPresetEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("", Handle).WithName("CreateMixerPreset");
    }

    public static async Task<ApiResponse<MixerPreset>> Handle(
        string songId,
        [FromBody] CreateMixerPresetRequest request,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        // TODO: Verify user has permission to create presets for this song

        var preset = new MixerPreset
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = request.Name,
            SongId = songId,
            Output = request.Output ?? new TrackPreset(),
            Tracks = request.Tracks ?? new List<TrackPreset>(),
        };

        await repository.MixerPresets.InsertOneAsync(preset, cancellationToken: cancellationToken);

        return ApiResponse<MixerPreset>.Success(preset);
    }
}

public sealed class CreateMixerPresetRequest
{
    public required string Name { get; set; }
    public TrackPreset? Output { get; set; }
    public List<TrackPreset>? Tracks { get; set; }
}
