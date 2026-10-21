using System.Net;
using System.Text.RegularExpressions;

namespace BandfixGrabber;

internal static class ChromeDiscovery
{
    internal static IEnumerable<string> Endpoints(string profile)
    {
        var marker = Path.Combine(profile, ".grabber-endpoint");
        if (File.Exists(marker) && LocalEndpoint(ReadFile(marker).Trim()) is { } saved) yield return saved;

        // Chrome writes this file when started with --remote-debugging-port=0.
        if (ActivePortEndpoint(ReadFile(Path.Combine(profile, "DevToolsActivePort"))) is { } active) yield return active;

        // Older runs launched Chrome with a fixed port and did not create either marker.
        // The profile lock identifies the owning process; check its profile before using its port.
        if (OperatingSystem.IsLinux())
        {
            var target = new FileInfo(Path.Combine(profile, "SingletonLock")).LinkTarget;
            if (target is not null && int.TryParse(target[(target.LastIndexOf('-') + 1)..], out var pid) && pid > 0)
            {
                var commandLine = ReadFile($"/proc/{pid}/cmdline");
                if (EndpointFromCommandLine(commandLine, profile) is { } endpoint)
                    yield return endpoint;
            }
        }
    }

    internal static string? ActivePortEndpoint(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.TrimEntries);
        return lines.Length >= 2 && int.TryParse(lines[0], out var port) && port is > 0 and <= 65535
            && lines[1].StartsWith("/devtools/browser/", StringComparison.Ordinal)
            && !lines[1].Any(c => char.IsWhiteSpace(c) || c is '?' or '#')
            ? $"ws://127.0.0.1:{port}{lines[1]}" : null;
    }

    internal static string? EndpointFromCommandLine(string commandLine, string profile)
    {
        var arguments = commandLine.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var endpoint = EndpointFromArguments(arguments, profile);
        if (endpoint is not null) return endpoint;
        // Chrome can replace argv with one process-title string in /proc/<pid>/cmdline.
        // Recover only the three flags we need; never execute or shell-evaluate this text.
        var title = commandLine.Replace('\0', ' ').Trim();
        var recovered = Regex.Matches(title,
            @"(?:^|\s)(--(?:user-data-dir|remote-debugging-port|remote-debugging-address))(?:=|\s+)(.*?)(?=\s+--|\s+https?://|\s+about:|$)")
            .Select(m => m.Groups[1].Value + "=" + m.Groups[2].Value.Trim().Trim('"', '\''))
            .ToArray();
        return EndpointFromArguments(recovered, profile);
    }

    internal static string? EndpointFromArguments(string[] arguments, string profile)
    {
        var userData = Flag(arguments, "--user-data-dir");
        if (userData is null || !Path.IsPathFullyQualified(userData)) return null;
        if (!string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(userData)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(profile)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return null;
        var address = Flag(arguments, "--remote-debugging-address");
        if (address is not null && address != "localhost" && (!IPAddress.TryParse(address, out var ip) || !IPAddress.IsLoopback(ip))) return null;
        return int.TryParse(Flag(arguments, "--remote-debugging-port"), out var port) && port is > 0 and <= 65535
            ? $"http://127.0.0.1:{port}" : null;
    }

    private static string? Flag(string[] arguments, string name)
    {
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i].StartsWith(name + "=", StringComparison.Ordinal)) return arguments[i][(name.Length + 1)..];
            if (arguments[i] == name && i + 1 < arguments.Length) return arguments[i + 1];
        }
        return null;
    }

    private static string? LocalEndpoint(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.IsLoopback && uri.Scheme is "http" or "https" or "ws" or "wss" ? value : null;

    private static string ReadFile(string path)
    {
        try { return File.ReadAllText(path); }
        catch (IOException) { return ""; }
        catch (UnauthorizedAccessException) { return ""; }
    }
}
