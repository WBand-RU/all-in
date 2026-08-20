using System.Diagnostics;
using System.Globalization;

namespace WBand.Modules.MixerModule.Application;

internal static class MixGenerationLock
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await Semaphore.WaitAsync(cancellationToken);
        return new Releaser();
    }

    private sealed class Releaser : IDisposable
    {
        public void Dispose() => Semaphore.Release();
    }
}

public sealed record AudioTrack(Guid StemId, string Path, string Name, string Group);
public enum MixPlanKind { Full, Focus, Minus }
public sealed record MixInput(AudioTrack Track, double GainDb, bool Muted = false);
public sealed record MixPlan(string Name, MixPlanKind Kind, AudioTrack? Target,
    IReadOnlyList<MixInput> Inputs);

public static class MixPlanFactory
{
    public static IReadOnlyList<MixPlan> Create(IReadOnlyList<AudioTrack> tracks,
        double backgroundDb = -40)
    {
        var click = tracks.FirstOrDefault(track => IsSupport(track.Name, "click", "клик"));
        var guide = tracks.FirstOrDefault(track => IsSupport(track.Name, "guide", "гайд"));
        var instruments = tracks.Where(track => track != click && track != guide).ToArray();
        var plans = new List<MixPlan>
        {
            new("full-mix", MixPlanKind.Full, null,
                tracks.Select(track => new MixInput(track, 0)).ToArray())
        };
        foreach (var target in instruments)
        {
            plans.Add(new($"{SafeName(target.Name)}__focus", MixPlanKind.Focus, target,
                tracks.Select(track => new MixInput(track,
                    track == target || track == click || track == guide ? 0 : backgroundDb)).ToArray()));
            plans.Add(new($"{SafeName(target.Name)}__minus", MixPlanKind.Minus, target,
                tracks.Select(track => new MixInput(track, 0, track == target)).ToArray()));
        }
        return plans;
    }

    private static bool IsSupport(string value, params string[] words) => words.Any(word =>
        value.Contains(word, StringComparison.OrdinalIgnoreCase));
    private static string SafeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(result) ? "track" : result.Trim();
    }
}

public static class FfmpegMixRunner
{
    public static async Task RunAsync(MixPlan plan, string waveOutputPath, string mp3OutputPath,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo { FileName = "ffmpeg", RedirectStandardError = true,
            RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in BuildArguments(plan, waveOutputPath, mp3OutputPath))
            startInfo.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var error = await errorTask; _ = await outputTask;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"ffmpeg failed ({process.ExitCode}): {LastLines(error)}");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException("ffmpeg is not installed or is not available through PATH.", exception);
        }
        catch
        {
            TryKill(process);
            throw;
        }
    }

    private static IReadOnlyList<string> BuildArguments(MixPlan plan, string waveOutputPath,
        string mp3OutputPath)
    {
        var arguments = new List<string> { "-hide_banner", "-nostdin", "-y" };
        foreach (var input in plan.Inputs) { arguments.Add("-i"); arguments.Add(input.Track.Path); }
        arguments.Add("-filter_complex"); arguments.Add(BuildFilter(plan.Inputs));
        arguments.AddRange(["-map", "[mixwav]", "-c:a", "pcm_s24le", "-ar", "48000",
            "-ac", "2", "-f", "wav", waveOutputPath, "-map", "[mixmp3]", "-c:a",
            "libmp3lame", "-b:a", "320k", "-ar", "48000", "-ac", "2", "-f", "mp3",
            mp3OutputPath]);
        return arguments;
    }

    private static string BuildFilter(IReadOnlyList<MixInput> inputs)
    {
        var filters = new List<string>();
        for (var index = 0; index < inputs.Count; index++)
        {
            var volume = inputs[index].Muted ? "0" :
                $"{inputs[index].GainDb.ToString("0.###", CultureInfo.InvariantCulture)}dB";
            filters.Add($"[{index}:a:0]aresample=48000,aformat=sample_fmts=fltp:channel_layouts=stereo," +
                $"asetpts=PTS-STARTPTS,volume={volume}[a{index}]");
        }
        var labels = string.Concat(Enumerable.Range(0, inputs.Count).Select(index => $"[a{index}]"));
        filters.Add($"{labels}amix=inputs={inputs.Count}:duration=longest:dropout_transition=0:normalize=0," +
            "alimiter=limit=0.95:attack=5:release=50:latency=1,asplit=2[mixwav][mixmp3]");
        return string.Join(';', filters);
    }

    private static string LastLines(string value) => string.Join(Environment.NewLine,
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).TakeLast(8));

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }
}
