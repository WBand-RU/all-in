using MixerApp;

namespace Mixer.Tests;

public sealed class OutputPlannerTests
{
    [Fact]
    public void Select_HandlesOverwriteSkipAndCancel()
    {
        using var directory = new TemporaryDirectory();
        var existingPath = Path.Combine(directory.Path, "existing.wav");
        File.WriteAllText(existingPath, "old");
        var existing = Command(existingPath, Path.Combine(directory.Path, "existing.mp3"));
        var fresh = Command(Path.Combine(directory.Path, "fresh.wav"), Path.Combine(directory.Path, "fresh.mp3"));

        Assert.Equal(2, OutputPlanner.Select([existing, fresh], ExistingFilesAction.Overwrite).Count);
        Assert.Same(fresh, Assert.Single(OutputPlanner.Select([existing, fresh], ExistingFilesAction.Skip)));
        Assert.Empty(OutputPlanner.Select([existing, fresh], ExistingFilesAction.Cancel));
    }

    private static FfmpegCommand Command(string wave, string mp3) =>
        new([], new OutputPaths(wave, mp3), new OutputPaths(wave + ".tmp", mp3 + ".tmp"));
}
