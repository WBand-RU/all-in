using PlaybackService.Endpoints.MixerPresets.CreateMixerPreset;
using PlaybackService.Endpoints.MixerPresets.DeleteMixerPreset;
using PlaybackService.Endpoints.MixerPresets.GetMixerPreset;
using PlaybackService.Endpoints.MixerPresets.GetMixerPresets;
using PlaybackService.Endpoints.MixerPresets.UpdateMixerPreset;

namespace PlaybackService.Endpoints.MixerPresets;

/// <summary>
/// Mapper for MixerPreset endpoints registration
/// </summary>
public sealed class MixerPresetsMapper
{
    public void Register(IEndpointRouteBuilder builder)
    {
        // Group under /songs for nested mixer preset routes
        var songsGroup = builder
            .MapGroup("songs/{songId}/mixer-presets")
            .WithTags("MixerPresets")
            .WithDisplayName("Song Mixer Presets")
            .RequireAuthorization();

        CreateMixerPresetEndpoint.Build(songsGroup);
        GetMixerPresetsEndpoint.Build(songsGroup);
        GetMixerPresetEndpoint.Build(songsGroup);
        UpdateMixerPresetEndpoint.Build(songsGroup);
        DeleteMixerPresetEndpoint.Build(songsGroup);
    }
}
