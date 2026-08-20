using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MixerApp;

namespace Mixer.Tests;

public sealed partial class FfmpegIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Runner_CreatesAllMixesWithExpectedFormatsDurationAndLevels()
    {
        if (!await ExternalTools.IsAvailableAsync("ffmpeg", CancellationToken.None) ||
            !await ExternalTools.IsAvailableAsync("ffprobe", CancellationToken.None))
            return;

        using var directory = new TemporaryDirectory();
        var input = Path.Combine(directory.Path, "Исходники с пробелом");
        var output = Path.Combine(directory.Path, "Результат");
        Directory.CreateDirectory(input);
        var targetPath = Path.Combine(input, "guitar.wav");
        var otherPath = Path.Combine(input, "bass.wav");
        await RunToolAsync("ffmpeg", ["-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i",
            "sine=frequency=440:duration=1.2:sample_rate=48000", targetPath]);
        await RunToolAsync("ffmpeg", ["-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i",
            "anullsrc=r=48000:cl=stereo", "-t", "0.5", otherPath]);

        var target = new AudioTrack(targetPath, "guitar.wav", "guitar");
        var other = new AudioTrack(otherPath, "bass.wav", "bass");
        var jobs = MixJobFactory.Create(new TrackAssignments([target, other], null, null), 0, -40);
        var runner = new FfmpegRunner(Path.Combine(output, "ffmpeg.log"));
        foreach (var job in jobs)
        {
            var command = FfmpegCommandBuilder.Build(job, output, Guid.NewGuid().ToString("N"));
            var result = await runner.RunAsync(command, CancellationToken.None);
            Assert.Equal(0, result.ExitCode);
        }

        Assert.Equal(5, Directory.GetFiles(Path.Combine(output, "wav"), "*.wav").Length);
        Assert.Equal(5, Directory.GetFiles(Path.Combine(output, "mp3"), "*.mp3").Length);
        var minusPath = Path.Combine(output, "wav", "guitar__minus.wav");
        var waveInfo = await ProbeAsync(minusPath);
        var mp3Info = await ProbeAsync(Path.Combine(output, "mp3", "full-mix.mp3"));
        Assert.Equal("pcm_s24le", waveInfo.Codec);
        Assert.InRange(waveInfo.Duration, 1.18, 1.25);
        Assert.Equal("mp3", mp3Info.Codec);
        Assert.InRange(mp3Info.BitRate, 315_000, 325_000);
        var minusDb = await MeasureMeanVolumeAsync(minusPath);
        Assert.True(double.IsNegativeInfinity(minusDb) || minusDb < -90, $"Minus mix noise floor was {minusDb} dB.");

        var loudJob = new MixJob("level-0", MixKind.Full, null, [new(target, 0)]);
        var quietJob = new MixJob("level-minus-40", MixKind.Focus, target, [new(target, -40)]);
        foreach (var job in new[] { loudJob, quietJob })
        {
            var result = await runner.RunAsync(
                FfmpegCommandBuilder.Build(job, output, Guid.NewGuid().ToString("N")), CancellationToken.None);
            Assert.Equal(0, result.ExitCode);
        }
        var loudDb = await MeasureMeanVolumeAsync(Path.Combine(output, "wav", "level-0.wav"));
        var quietDb = await MeasureMeanVolumeAsync(Path.Combine(output, "wav", "level-minus-40.wav"));
        Assert.InRange(loudDb - quietDb, 39.5, 40.5);
    }

    private static async Task<(string Codec, double Duration, long BitRate)> ProbeAsync(string path)
    {
        var result = await RunToolAsync("ffprobe", ["-v", "error", "-show_entries",
            "stream=codec_name,bit_rate:format=duration,bit_rate", "-of", "json", path]);
        using var json = JsonDocument.Parse(result.Output);
        var stream = json.RootElement.GetProperty("streams")[0];
        var format = json.RootElement.GetProperty("format");
        var codec = stream.GetProperty("codec_name").GetString()!;
        var duration = double.Parse(format.GetProperty("duration").GetString()!, CultureInfo.InvariantCulture);
        var bitRateElement = stream.TryGetProperty("bit_rate", out var streamRate)
            ? streamRate
            : format.GetProperty("bit_rate");
        var bitRate = long.Parse(bitRateElement.GetString()!, CultureInfo.InvariantCulture);
        return (codec, duration, bitRate);
    }

    private static async Task<double> MeasureMeanVolumeAsync(string path)
    {
        var result = await RunToolAsync("ffmpeg", ["-hide_banner", "-i", path, "-af", "volumedetect", "-f", "null", "-"]);
        var match = MeanVolume().Match(result.Error);
        Assert.True(match.Success, result.Error);
        return match.Groups[1].Value == "-inf"
            ? double.NegativeInfinity
            : double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static async Task<(string Output, string Error)> RunToolAsync(string fileName, IReadOnlyList<string> arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName, RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            }
        };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;
        Assert.True(process.ExitCode == 0, error);
        return (output, error);
    }

    [GeneratedRegex(@"mean_volume:\s+(-inf|-?[0-9.]+) dB", RegexOptions.CultureInvariant)]
    private static partial Regex MeanVolume();
}
