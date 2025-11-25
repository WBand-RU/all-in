using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PlaybackService.Domain;
using PlaybackService.Services;
using Shared;

namespace PlaybackService.Endpoints.MixerPresets.GetMixerPresets;

/// <summary>
/// Endpoint for getting mixer presets for a song
/// </summary>
internal sealed class GetMixerPresetsEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", Handle).WithName("GetMixerPresets");
    }

    public static async Task<ApiResponse<List<MixerPreset>>> Handle(
        string songId,
        Repository repository,
        CancellationToken cancellationToken
    )
    {
        var presets = await repository
            .MixerPresets.Find(p => p.SongId == songId)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<MixerPreset>>.Success(presets);
    }
}
