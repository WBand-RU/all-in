using Microsoft.Extensions.Logging;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using WBand.AgentPlayer.Models;

using PlaybackState = WBand.AgentPlayer.Models.PlaybackState;

namespace WBand.AgentPlayer.Services;

/// <summary>
/// Service for handling audio playback
/// </summary>
public sealed class AudioPlaybackService : IDisposable
{
    private readonly ILogger<AudioPlaybackService> logger;
    private readonly Dictionary<string, AudioFileReader> audioReaders;
    private readonly Dictionary<string, VolumeSampleProvider> volumeProviders;
    private WaveOutEvent? waveOut;
    private MixingSampleProvider? mixer;
    private readonly PlaybackState currentState;
    private string? currentTrackId;

    public AudioPlaybackService(ILogger<AudioPlaybackService> logger)
    {
        this.logger = logger;
        this.audioReaders = [];
        this.volumeProviders = [];
        this.currentState = new PlaybackState();
    }

    public async Task LoadTrackAsync(string trackId)
    {
        try
        {
            this.currentState.IsLoading = true;

            // TODO: Download track from server using trackId
            // For now, this is a placeholder
            var filePath = $"tracks/{trackId}.mp3";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Track file not found: {filePath}");
            }

            // Clean up previous track
            this.ClearTracks();

            // Load new track
            var reader = new AudioFileReader(filePath);
            this.audioReaders[trackId] = reader;

            var volumeProvider = new VolumeSampleProvider(reader);
            this.volumeProviders[trackId] = volumeProvider;

            // Initialize mixer with single track for now
            // TODO: Support multitrack mixing
            this.mixer = new MixingSampleProvider(reader.WaveFormat);
            this.mixer.AddMixerInput(volumeProvider);

            // Initialize output
            this.waveOut?.Dispose();
            this.waveOut = new WaveOutEvent();
            this.waveOut.Init(this.mixer);

            this.currentTrackId = trackId;
            this.currentState.IsLoading = false;
            this.currentState.Duration = reader.TotalTime.TotalSeconds;

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to load track: {TrackId}", trackId);
            this.currentState.IsLoading = false;
            this.currentState.ErrorMessage = ex.Message;
            throw;
        }
    }

    public void Play()
    {
        if (this.waveOut == null || this.mixer == null)
        {
            this.logger.LogWarning("No track loaded");
            return;
        }

        this.waveOut.Play();
        this.currentState.IsPlaying = true;
        this.currentState.IsPaused = false;
    }

    public void Pause()
    {
        if (this.waveOut == null)
        {
            return;
        }

        this.waveOut.Pause();
        this.currentState.IsPlaying = false;
        this.currentState.IsPaused = true;
    }

    public void Stop()
    {
        if (this.waveOut == null)
        {
            return;
        }

        this.waveOut.Stop();
        this.currentState.IsPlaying = false;
        this.currentState.IsPaused = false;
        this.currentState.CurrentPosition = 0;

        // Reset position
        foreach (var reader in this.audioReaders.Values)
        {
            reader.Position = 0;
        }
    }

    public void Seek(double positionSeconds)
    {
        foreach (var reader in this.audioReaders.Values)
        {
            var position = TimeSpan.FromSeconds(positionSeconds);
            if (position <= reader.TotalTime)
            {
                reader.CurrentTime = position;
            }
        }

        this.currentState.CurrentPosition = positionSeconds;
    }

    public void SetVolume(float volume)
    {
        volume = Math.Max(0f, Math.Min(1f, volume));

        foreach (var volumeProvider in this.volumeProviders.Values)
        {
            volumeProvider.Volume = volume;
        }

        this.currentState.Volume = volume;
    }

    public void UpdateMixerSettings(Dictionary<string, TrackMixerSettings> settings)
    {
        foreach (var (trackId, mixerSettings) in settings)
        {
            if (this.volumeProviders.TryGetValue(trackId, out var volumeProvider))
            {
                volumeProvider.Volume = mixerSettings.Muted ? 0f : mixerSettings.Volume;
                // TODO: Implement pan and solo functionality
            }
        }
    }

    public PlaybackState GetCurrentState()
    {
        if (this.waveOut != null && this.audioReaders.Count > 0)
        {
            var reader = this.audioReaders.Values.First();
            this.currentState.CurrentPosition = reader.CurrentTime.TotalSeconds;
        }

        this.currentState.CurrentTrackId = this.currentTrackId;
        return this.currentState;
    }

    private void ClearTracks()
    {
        foreach (var reader in this.audioReaders.Values)
        {
            reader.Dispose();
        }

        this.audioReaders.Clear();
        this.volumeProviders.Clear();
    }

    public void Dispose()
    {
        this.ClearTracks();
        this.waveOut?.Dispose();
    }
}
