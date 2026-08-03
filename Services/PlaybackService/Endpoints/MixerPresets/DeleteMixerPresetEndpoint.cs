using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PlaybackService.Services;
using Shared;
using Shared.Services;

namespace PlaybackService.Endpoints.MixerPresets.DeleteMixerPreset;

/// <summary>
/// Endpoint for deleting a mixer preset
/// </summary>
internal sealed class DeleteMixerPresetEndpoint
{
    public static void Build(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("{id}", Handle).WithName("DeleteMixerPreset");
    }

    public static async Task<ApiResponse<object>> Handle(
        string songId,
        string id,
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
            return ApiResponse<object>.Error(ApiCodes.NotFound);
        }

        // TODO: Verify user has permission to delete this preset

        await repository.MixerPresets.DeleteOneAsync(p => p.Id == id, cancellationToken);

        return ApiResponse<object>.Success(null);
    }
}
