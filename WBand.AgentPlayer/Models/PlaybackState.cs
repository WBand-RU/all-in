namespace WBand.AgentPlayer.Models;

/// <summary>
/// Represents the current state of playback
/// </summary>
public sealed class PlaybackState
{
    public bool IsPlaying { get; set; }
    public bool IsPaused { get; set; }
    public bool IsLoading { get; set; }
    public double CurrentPosition { get; set; }
    public double Duration { get; set; }
    public float Volume { get; set; } = 1.0f;
    public string? CurrentTrackId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Command to control playback
/// </summary>
public sealed class PlaybackCommand
{
    public required string Command { get; set; } // play, pause, stop, seek, volume
    public string? TrackId { get; set; }
    public double? Position { get; set; }
    public float? Volume { get; set; }
    public Dictionary<string, TrackMixerSettings>? MixerSettings { get; set; }
}

/// <summary>
/// Individual track mixer settings
/// </summary>
public sealed class TrackMixerSettings
{
    public float Volume { get; set; }
    public float Pan { get; set; }
    public bool Muted { get; set; }
    public bool Solo { get; set; }
}
