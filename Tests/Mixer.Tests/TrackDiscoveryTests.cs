using MixerApp;

namespace Mixer.Tests;

public sealed class TrackDiscoveryTests
{
    [Fact]
    public void CandidateSearch_UsesSeparateWordsAndBothLanguages()
    {
        var tracks = new[]
        {
            Track("01 CLICK.wav"), Track("мой-гайд.flac"), Track("clicktrack.wav"), Track("guitar.wav")
        };

        Assert.Equal("01 CLICK.wav", Assert.Single(TrackDiscovery.FindClickCandidates(tracks)).DisplayName);
        Assert.Equal("мой-гайд.flac", Assert.Single(TrackDiscovery.FindGuideCandidates(tracks)).DisplayName);
    }

    [Fact]
    public void Discover_OnlyReadsTopLevelSupportedFilesAndMakesDuplicateStemsUnique()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "guitar.wav"), "");
        File.WriteAllText(Path.Combine(directory.Path, "guitar.mp3"), "");
        File.WriteAllText(Path.Combine(directory.Path, "notes.txt"), "");
        Directory.CreateDirectory(Path.Combine(directory.Path, "nested"));
        File.WriteAllText(Path.Combine(directory.Path, "nested", "bass.wav"), "");

        var tracks = TrackDiscovery.Discover(directory.Path, Path.Combine(directory.Path, "mix-output"));

        Assert.Equal(2, tracks.Count);
        Assert.Equal(2, tracks.Select(track => track.OutputStem).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(tracks, track => Assert.StartsWith("guitar_", track.OutputStem));
    }

    private static AudioTrack Track(string name) => new(name, name, Path.GetFileNameWithoutExtension(name));
}
