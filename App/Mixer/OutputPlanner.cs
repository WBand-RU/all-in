namespace MixerApp;

public static class OutputPlanner
{
    public static bool Exists(OutputPaths paths) => File.Exists(paths.WavePath) || File.Exists(paths.Mp3Path);

    public static IReadOnlyList<FfmpegCommand> Select(
        IEnumerable<FfmpegCommand> commands,
        ExistingFilesAction action) => action switch
        {
            ExistingFilesAction.Cancel => [],
            ExistingFilesAction.Skip => commands.Where(command => !Exists(command.FinalPaths)).ToArray(),
            _ => commands.ToArray()
        };
}
