using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;

namespace PlaybackService.Endpoints.MixerPresets.GetMixerPreset;

/// <summary>
/// Endpoint for getting a mixer preset by id
/// </summary>
internal sealed class GetMixerPresetEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("{id}", Handle).WithName("GetMixerPreset");
    }

    public static async Task<ApiResponse<MixerPreset>> Handle(
        string songId,
        string id,
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

        return ApiResponse<MixerPreset>.Success(preset);
    }
}
