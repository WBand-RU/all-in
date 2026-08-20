using System.Diagnostics;
using System.Text;

namespace MixerApp;

public static class ExternalTools
{
    public static async Task<bool> IsAvailableAsync(string executable, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable, RedirectStandardOutput = true, RedirectStandardError = true,
                    UseShellExecute = false, CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-version");
            process.Start();
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}

public sealed record ProcessResult(int ExitCode, string StandardError);

public sealed class FfmpegRunner
{
    private readonly string _logPath;
    public FfmpegRunner(string logPath) => _logPath = logPath;

    public async Task<ProcessResult> RunAsync(FfmpegCommand command, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(command.FinalPaths.WavePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(command.FinalPaths.Mp3Path)!);
        DeleteTemporaryFiles(command);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg", RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            }
        };
        foreach (var argument in command.Arguments) process.StartInfo.ArgumentList.Add(argument);

        var diagnostic = new StringBuilder()
            .AppendLine($"[{DateTimeOffset.Now:O}] ffmpeg {string.Join(' ', command.Arguments.Select(QuoteForLog))}");
        try
        {
            process.Start();
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var error = await errorTask;
            _ = await outputTask;
            diagnostic.AppendLine(error).AppendLine($"Exit code: {process.ExitCode}");
            await AppendLogAsync(diagnostic.ToString());

            if (process.ExitCode != 0)
            {
                DeleteTemporaryFiles(command);
                return new ProcessResult(process.ExitCode, error);
            }

            File.Move(command.TemporaryPaths.WavePath, command.FinalPaths.WavePath, overwrite: true);
            File.Move(command.TemporaryPaths.Mp3Path, command.FinalPaths.Mp3Path, overwrite: true);
            return new ProcessResult(0, error);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            DeleteTemporaryFiles(command);
            await AppendLogAsync(diagnostic.AppendLine("Cancelled.").ToString());
            throw;
        }
        catch
        {
            TryKill(process);
            DeleteTemporaryFiles(command);
            throw;
        }
    }

    private async Task AppendLogAsync(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        await File.AppendAllTextAsync(_logPath, text + Environment.NewLine, CancellationToken.None);
    }

    private static string QuoteForLog(string value) =>
        value.Any(char.IsWhiteSpace) ? $"\"{value.Replace("\"", "\\\"")}\"" : value;

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }

    private static void DeleteTemporaryFiles(FfmpegCommand command)
    {
        File.Delete(command.TemporaryPaths.WavePath);
        File.Delete(command.TemporaryPaths.Mp3Path);
    }
}
