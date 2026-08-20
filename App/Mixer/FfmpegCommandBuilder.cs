using System.Globalization;

namespace MixerApp;

public static class FfmpegCommandBuilder
{
    public static FfmpegCommand Build(MixJob job, string outputDirectory, string temporaryId)
    {
        var waveDirectory = Path.Combine(outputDirectory, "wav");
        var mp3Directory = Path.Combine(outputDirectory, "mp3");
        var finalPaths = new OutputPaths(Path.Combine(waveDirectory, $"{job.Name}.wav"),
            Path.Combine(mp3Directory, $"{job.Name}.mp3"));
        var temporaryPaths = new OutputPaths(Path.Combine(waveDirectory, $".{job.Name}.{temporaryId}.tmp.wav"),
            Path.Combine(mp3Directory, $".{job.Name}.{temporaryId}.tmp.mp3"));
        var arguments = new List<string> { "-hide_banner", "-nostdin", "-y" };

        foreach (var input in job.Inputs) { arguments.Add("-i"); arguments.Add(input.Track.Path); }
        arguments.Add("-filter_complex");
        arguments.Add(BuildFilter(job.Inputs));
        arguments.AddRange([
            "-map", "[mixwav]", "-c:a", "pcm_s24le", "-ar", "48000", "-ac", "2", "-f", "wav", temporaryPaths.WavePath,
            "-map", "[mixmp3]", "-c:a", "libmp3lame", "-b:a", "320k", "-ar", "48000", "-ac", "2", "-f", "mp3", temporaryPaths.Mp3Path
        ]);
        return new FfmpegCommand(arguments, finalPaths, temporaryPaths);
    }

    private static string BuildFilter(IReadOnlyList<TrackMixInput> inputs)
    {
        var filters = new List<string>(inputs.Count + 1);
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            var volume = input.Muted ? "0" : $"{input.GainDb.ToString("0.###", CultureInfo.InvariantCulture)}dB";
            filters.Add($"[{index}:a:0]aresample=48000,aformat=sample_fmts=fltp:channel_layouts=stereo," +
                        $"asetpts=PTS-STARTPTS,volume={volume}[a{index}]");
        }
        var labels = string.Concat(Enumerable.Range(0, inputs.Count).Select(index => $"[a{index}]"));
        filters.Add($"{labels}amix=inputs={inputs.Count}:duration=longest:dropout_transition=0:normalize=0," +
                    "alimiter=limit=0.95:attack=5:release=50:latency=1,asplit=2[mixwav][mixmp3]");
        return string.Join(';', filters);
    }
}
