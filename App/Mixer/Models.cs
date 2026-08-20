namespace MixerApp;

public sealed record MixerOptions(string? InputDirectory, string? OutputDirectory,
    double ForegroundDb = 0, double BackgroundDb = -40, bool ShowHelp = false);

public sealed record AudioTrack(string Path, string DisplayName, string OutputStem);

public sealed record TrackAssignments(IReadOnlyList<AudioTrack> Tracks, AudioTrack? ClickTrack, AudioTrack? GuideTrack)
{
    public IReadOnlyList<AudioTrack> Instruments => Tracks
        .Where(track => track != ClickTrack && track != GuideTrack).ToArray();
}

public enum MixKind { Full, Focus, Minus }
public sealed record TrackMixInput(AudioTrack Track, double GainDb, bool Muted = false);
public sealed record MixJob(string Name, MixKind Kind, AudioTrack? TargetTrack, IReadOnlyList<TrackMixInput> Inputs);
public enum ExistingFilesAction { Overwrite, Skip, Cancel }
public sealed record OutputPaths(string WavePath, string Mp3Path);
public sealed record FfmpegCommand(IReadOnlyList<string> Arguments, OutputPaths FinalPaths, OutputPaths TemporaryPaths);
