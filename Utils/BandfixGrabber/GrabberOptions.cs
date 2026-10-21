namespace BandfixGrabber;

internal sealed record GrabberOptions
{
    internal string Band { get; init; } = "MBV Youth";
    internal string Output { get; init; } = Path.Combine(ProjectDirectory(), "songs");
    internal string Profile { get; init; } = Path.Combine(ProjectDirectory(), "chrome-profile");
    internal string? Chrome { get; init; }
    internal string? Cdp { get; init; }
    internal string Site { get; init; } = "https://bandfix.by";
    internal string ApiBase { get; init; } = "https://prod-bandfix-gateway.azurewebsites.net/api";
    internal TimeSpan LoginTimeout { get; init; } = TimeSpan.FromMinutes(10);
    internal bool Help { get; init; }
    internal bool SelfTest { get; init; }

    internal static GrabberOptions Parse(string[] args)
    {
        var result = new GrabberOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var flag = args[i];
            if (flag == "--help") { result = result with { Help = true }; continue; }
            if (flag == "--self-test") { result = result with { SelfTest = true }; continue; }
            if (flag is not ("--band" or "--output" or "--profile" or "--chrome" or "--cdp" or "--site" or "--timeout"))
                throw new ArgumentException($"Неизвестный параметр: {flag}. См. --help.");
            if (++i == args.Length || args[i].StartsWith("--")) throw new ArgumentException($"Укажите значение {flag}.");
            var value = args[i];
            result = flag switch
            {
                "--band" => result with { Band = value },
                "--output" => result with { Output = ExpandPath(value) },
                "--profile" => result with { Profile = ExpandPath(value) },
                "--chrome" => result with { Chrome = ExpandPath(value) },
                "--cdp" => result with { Cdp = value },
                "--site" => result with { Site = value.TrimEnd('/') },
                "--timeout" when double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var minutes)
                    && double.IsFinite(minutes) && minutes > 0 && minutes <= 1440 => result with { LoginTimeout = TimeSpan.FromMinutes(minutes) },
                _ => throw new ArgumentException($"Некорректное значение {flag}.")
            };
        }
        if (result.Site is not ("https://bandfix.by" or "https://bandfix.app"))
            throw new ArgumentException("--site: поддерживаются https://bandfix.by и https://bandfix.app.");
        if (result.Cdp is not null && (!Uri.TryCreate(result.Cdp, UriKind.Absolute, out var uri)
            || !uri.IsLoopback || uri.Scheme is not ("http" or "https")))
            throw new ArgumentException("--cdp должен быть HTTP-адресом на localhost.");
        if (string.IsNullOrWhiteSpace(result.Band)) throw new ArgumentException("Имя группы не может быть пустым.");
        return result;
    }

    private static string ExpandPath(string path) => Path.GetFullPath(path.StartsWith("~/")
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]) : path);

    private static string ProjectDirectory()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "BandfixGrabber.csproj"))) return dir.FullName;
                var nested = Path.Combine(dir.FullName, "Utils", "BandfixGrabber");
                if (File.Exists(Path.Combine(nested, "BandfixGrabber.csproj"))) return nested;
            }
        return AppContext.BaseDirectory;
    }
}
