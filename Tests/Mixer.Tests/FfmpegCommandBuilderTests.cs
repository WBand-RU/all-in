using MixerApp;

namespace Mixer.Tests;

public sealed class FfmpegCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDualOutputAndExpectedFilter()
    {
        var target = new AudioTrack("C:\\Музыка с пробелом\\guitar.wav", "guitar.wav", "guitar");
        var background = new AudioTrack("bass.wav", "bass.wav", "bass");
        var job = new MixJob("guitar__focus", MixKind.Focus, target,
            [new(target, 0), new(background, -40)]);

        var command = FfmpegCommandBuilder.Build(job, "out", "test");
        var filter = command.Arguments[FindArgument(command.Arguments, "-filter_complex") + 1];

        Assert.Contains(target.Path, command.Arguments);
        Assert.Contains("volume=0dB", filter);
        Assert.Contains("volume=-40dB", filter);
        Assert.Contains("normalize=0", filter);
        Assert.Contains("alimiter=", filter);
        Assert.Contains("pcm_s24le", command.Arguments);
        Assert.Contains("libmp3lame", command.Arguments);
        Assert.EndsWith(".tmp.wav", command.TemporaryPaths.WavePath);
        Assert.EndsWith(".tmp.mp3", command.TemporaryPaths.Mp3Path);
    }

    [Fact]
    public void Build_UsesExactSilenceForMutedMinusTrack()
    {
        var track = new AudioTrack("solo.wav", "solo.wav", "solo");
        var job = new MixJob("solo__minus", MixKind.Minus, track, [new(track, 0, Muted: true)]);

        var command = FfmpegCommandBuilder.Build(job, "out", "test");
        var filter = command.Arguments[FindArgument(command.Arguments, "-filter_complex") + 1];

        Assert.Contains("volume=0[", filter);
    }

    private static int FindArgument(IReadOnlyList<string> arguments, string value)
    {
        for (var index = 0; index < arguments.Count; index++)
            if (arguments[index] == value) return index;
        return -1;
    }
}
