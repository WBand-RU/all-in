using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;
using Shared.Services;

namespace PlaybackService.Endpoints.MixerPresets.UpdateMixerPreset;

/// <summary>
/// Endpoint for updating a mixer preset
/// </summary>
internal sealed class UpdateMixerPresetEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("{id}", Handle).WithName("UpdateMixerPreset");
    }

    public static async Task<ApiResponse<MixerPreset>> Handle(
        string songId,
        string id,
        [FromBody] UpdateMixerPresetRequest request,
        ICurrentUser currentUser,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var preset = await repository
            .MixerPresets.Find(p => p.Id == id && p.SongId == songId)
            .FirstOrDefaultAsync(cancellationToken);

        if (preset is null)
        {
            return ApiResponse<MixerPreset>.Error(ApiCodes.NotFound);
        }

        // TODO: Verify user has permission to update this preset

        if (request.Name is not null)
            preset.Name = request.Name;

        if (request.Output is not null)
            preset.Output = request.Output;

        if (request.Tracks is not null)
            preset.Tracks = request.Tracks;

        await repository.MixerPresets.ReplaceOneAsync(
            p => p.Id == id,
            preset,
            cancellationToken: cancellationToken
        );

        return ApiResponse<MixerPreset>.Success(preset);
    }
}

public sealed class UpdateMixerPresetRequest
{
    public string? Name { get; set; }
    public TrackPreset? Output { get; set; }
    public List<TrackPreset>? Tracks { get; set; }
}
