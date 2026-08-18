using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WBand.Dev.Services;

internal sealed class ProcessManager(ILogger<ProcessManager> logger) : IDisposable
{
    private readonly Dictionary<string, Process> processes = [];
    private readonly Dictionary<string, Queue<string>> outputs = [];
    private readonly Lock syncRoot = new();
    private bool disposed;

    public void Dispose()
    {
        List<KeyValuePair<string, Process>> processesToStop;

        lock (this.syncRoot)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            processesToStop = [.. this.processes];
            this.processes.Clear();
            this.outputs.Clear();
        }

        foreach (var (serviceName, process) in processesToStop)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    _ = process.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unable to stop process {ServiceName}", serviceName);
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    public void Start(ServiceInfo info)
    {
        lock (this.syncRoot)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            if (this.processes.ContainsKey(info.ServiceName))
            {
                return;
            }

            var startInfo = new ProcessStartInfo(info.Filename, info.Arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,

                //UseShellExecute = true,
            };

            var process =
                Process.Start(startInfo)
                ?? throw new InvalidProgramException("Process is not started: " + info.ServiceName);

            this.outputs.Add(info.ServiceName, new Queue<string>());

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    this.AddOutput(info.ServiceName, e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    this.AddOutput(info.ServiceName, e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            this.processes.Add(info.ServiceName, process);
        }
    }

    internal bool HasProcess(string serviceName)
    {
        lock (this.syncRoot)
        {
            return this.processes.ContainsKey(serviceName);
        }
    }

    internal IReadOnlyDictionary<string, ManagedProcessStatus> GetStatuses()
    {
        lock (this.syncRoot)
        {
            return this.processes.ToDictionary(
                pair => pair.Key,
                pair => CreateStatus(pair.Value, this.outputs.GetValueOrDefault(pair.Key))
            );
        }
    }

    private void AddOutput(string serviceName, string line)
    {
        lock (this.syncRoot)
        {
            if (!this.outputs.TryGetValue(serviceName, out var output))
            {
                return;
            }

            output.Enqueue(line);

            while (output.Count > 20)
            {
                _ = output.Dequeue();
            }
        }
    }

    private static ManagedProcessStatus CreateStatus(Process process, Queue<string>? output)
    {
        try
        {
            var isRunning = !process.HasExited;
            int? exitCode = isRunning ? null : process.ExitCode;
            return new(process.Id, isRunning, exitCode, output?.ToArray() ?? []);
        }
        catch (InvalidOperationException)
        {
            return new(null, false, null, output?.ToArray() ?? []);
        }
    }
}

internal sealed record ManagedProcessStatus(
    int? ProcessId,
    bool IsRunning,
    int? ExitCode,
    IReadOnlyList<string> Output
);
