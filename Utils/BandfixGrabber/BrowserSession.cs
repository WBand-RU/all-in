using System.Text.Json.Nodes;
using System.Diagnostics;
using Microsoft.Playwright;

namespace BandfixGrabber;

internal sealed class BrowserSession(IPlaywright playwright, IBrowser browser) : IAsyncDisposable
{
    private readonly SessionTracker tracker = new();

    internal static async Task<BrowserSession> OpenAsync(GrabberOptions options, CancellationToken cancellation)
    {
        var playwright = await Playwright.CreateAsync();
        try
        {
            IBrowser? browser = null;
            var endpoint = options.Cdp;
            var marker = Path.Combine(options.Profile, ".grabber-endpoint");
            if (endpoint is null)
            {
                foreach (var candidate in ChromeDiscovery.Endpoints(options.Profile).Distinct())
                {
                    cancellation.ThrowIfCancellationRequested();
                    try
                    {
                        browser = await playwright.Chromium.ConnectOverCDPAsync(candidate, new() { Timeout = 2_000 });
                        Console.WriteLine("Подключение к уже открытому Chrome с профилем граббера.");
                        break;
                    }
                    catch (PlaywrightException) { /* Ignore stale endpoints from previous runs. */ }
                }
            }
            if (browser is null && endpoint is null)
            {
                var executable = options.Chrome ?? FindChrome();
                Directory.CreateDirectory(options.Profile);
                var start = new ProcessStartInfo(executable)
                {
                    UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
                };
                foreach (var argument in new[]
                {
                    "--user-data-dir=" + options.Profile, "--remote-debugging-address=127.0.0.1",
                    "--remote-debugging-port=0", "--no-first-run", "--no-default-browser-check", "about:blank"
                }) start.ArgumentList.Add(argument);
                // Chrome chooses its own free port and writes DevToolsActivePort in this profile.
                using var process = Process.Start(start) ?? throw new InvalidOperationException("Не удалось запустить Chrome.");
                var diagnostics = new System.Collections.Concurrent.ConcurrentQueue<string>();
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data is not null)
                    {
                        diagnostics.Enqueue(e.Data);
                        while (diagnostics.Count > 8) diagnostics.TryDequeue(out _);
                    }
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
                do
                {
                    cancellation.ThrowIfCancellationRequested();
                    foreach (var candidate in ChromeDiscovery.Endpoints(options.Profile).Distinct())
                    {
                        try
                        {
                            browser = await playwright.Chromium.ConnectOverCDPAsync(candidate, new() { Timeout = 1_000 });
                            endpoint = candidate;
                            break;
                        }
                        catch (PlaywrightException) { }
                    }
                    if (browser is not null) break;
                    if (process.HasExited)
                    {
                        await process.WaitForExitAsync(cancellation);
                        break;
                    }
                    await Task.Delay(250, cancellation);
                } while (DateTimeOffset.UtcNow < deadline);
                if (browser is null)
                {
                    var detail = string.Join(Environment.NewLine, diagnostics);
                    var exit = process.HasExited ? $" Код выхода: {process.ExitCode}." : "";
                    throw new InvalidOperationException($"Chrome не открыл порт подключения.{exit} Профиль: {options.Profile}. " +
                        "Если этот отдельный профиль открыт без remote debugging, закройте только его окна и повторите запуск." +
                        (detail.Length == 0 ? "" : Environment.NewLine + detail));
                }
                await File.WriteAllTextAsync(marker, endpoint!, cancellation);
            }
            browser ??= await playwright.Chromium.ConnectOverCDPAsync(endpoint!);
            var session = new BrowserSession(playwright, browser);
            foreach (var context in browser.Contexts) context.Request += session.OnRequest;
            var pages = browser.Contexts.SelectMany(c => c.Pages);
            var page = pages.FirstOrDefault(p => IsBandfixPage(p.Url));
            if (page is null)
            {
                page = await browser.Contexts[0].NewPageAsync();
                await page.GotoAsync(options.Site + "/app/songs", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
            else
            {
                // Keep the origin of an existing authenticated tab, including bandfix.app OAuth redirects.
                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
            Console.WriteLine("Открыт обычный Chrome. Если нужен вход, войдите через Google в igorock4jesus@gmail.com.");
            Console.WriteLine("Не закрывайте окно: после входа выгрузка продолжится автоматически.");
            return session;
        }
        catch { playwright.Dispose(); throw; }
    }

    internal Task<(BandfixApi Api, JsonNode Bands)> WaitForBandAsync(HttpClient http, GrabberOptions options, CancellationToken cancellation)
        => tracker.WaitForBandAsync(http, options, cancellation);

    private void OnRequest(object? sender, IRequest request) => tracker.Observe(request.Url, request.Headers);

    private static bool IsBandfixPage(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && uri.Host is "bandfix.by" or "bandfix.app";

    private static string FindChrome()
    {
        var names = OperatingSystem.IsWindows() ? new[] { "chrome.exe" } : new[] { "google-chrome", "google-chrome-stable", "chromium", "chromium-browser" };
        var directories = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).ToList();
        if (OperatingSystem.IsMacOS()) directories.Add("/Applications/Google Chrome.app/Contents/MacOS");
        if (OperatingSystem.IsWindows())
            foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) })
                directories.Add(Path.Combine(root, "Google", "Chrome", "Application"));
        foreach (var directory in directories)
            foreach (var name in names.Concat(OperatingSystem.IsMacOS() ? ["Google Chrome"] : Array.Empty<string>()))
                if (File.Exists(Path.Combine(directory, name))) return Path.Combine(directory, name);
        throw new FileNotFoundException("Не найден установленный Chrome. Укажите --chrome /путь/к/chrome.");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var context in browser.Contexts) context.Request -= OnRequest;
        await browser.CloseAsync(); // Disconnect CDP; do not close the externally launched Chrome.
        playwright.Dispose();
    }
}
