using System.Net;
using System.Text.Json.Nodes;

namespace BandfixGrabber;

// Executable contract tests: no Google account, browser, network or extra test packages.
internal static class GrabberSelfTest
{
    internal static async Task<int> RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "bandfix-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await RunCase("pagination", root, failure: false, repeatedCursor: false);
            await RunCase("partial-failure", root, failure: true, repeatedCursor: false);
            await RunCase("repeated-cursor", root, failure: false, repeatedCursor: true);
            await RunRetryCase();
            await RunLoginCase();
            RunChromeDiscoveryCases(root);
            Expect(SongExporter.SafeFileName("../song") != SongExporter.SafeFileName("..%2Fsong"), "Разные ID не должны перезаписывать друг друга.");
            Expect(SongExporter.SameBand("MBV_Youth", "MBV Youth"), "Поддерживается разделитель underscore.");
            Expect(!SongExporter.SameBand("MBV Worship", "MBV Youth"), "Другая группа не подходит.");
            try { GrabberOptions.Parse(["--unknown"]); throw new Exception("Неизвестный параметр принят."); }
            catch (ArgumentException) { }
            Console.WriteLine("Self-test: все проверки пройдены (пагинация, дубликаты, удалённые песни, Unicode, частичный сбой, повтор курсора, retry, гостевая сессия, истёкший токен, вход и обнаружение Chrome).");
            return 0;
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static async Task RunCase(string name, string root, bool failure, bool repeatedCursor)
    {
        var requests = new List<string>();
        using var http = new HttpClient(new StubHandler(request =>
        {
            var path = request.RequestUri!.PathAndQuery;
            requests.Add(path);
            Expect(request.Headers.Authorization?.ToString() == "Bearer secret", "Токен передан только как Bearer.");
            return path switch
            {
                "/api/bands" => Json("""{"items":[{"id":"other","name":"MBV Worship"},{"id":"youth","name":"MBV_Youth"}]}"""),
                "/api/songs/band/youth" => Json("""{"items":[{"id":"a"},{"id":"deleted","isDeleted":true}]}""", "page+2/="),
                "/api/songs/band/youth?cursor=page%2B2%2F%3D" => Json("""{"items":[{"id":"a"},{"id":"b"}]}""", repeatedCursor ? "page+2/=" : null),
                "/api/songs/a" => Json("""{"id":"a","bandId":"youth","content":{"name":"Песня","text":"[C]Слава\\nБогу"}}"""),
                "/api/songs/b" when failure => new(HttpStatusCode.Forbidden),
                "/api/songs/b" => Json("""{"id":"b","bandId":"youth","content":{"name":"Песня","text":"Другой текст"}}"""),
                _ => throw new Exception("Неожиданный запрос: " + path)
            };
        }));
        var options = new GrabberOptions { Output = Path.Combine(root, name) };
        var exporter = new SongExporter(new BandfixApi(http, "https://fixture.invalid/api", () => "secret"));
        if (repeatedCursor)
        {
            try { await exporter.ExportAsync(options, default); throw new Exception("Повтор курсора не обнаружен."); }
            catch (InvalidDataException) { }
        }
        else
        {
            var exit = await exporter.ExportAsync(options, default);
            Expect(exit == (failure ? 1 : 0), "Код завершения отражает полноту выгрузки.");
        }
        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(options.Output, "_manifest.json")))!;
        Expect(manifest["complete"]!.GetValue<bool>() == (!failure && !repeatedCursor), "Manifest не сообщает ложный успех.");
        Expect(manifest["discovered"]!.GetValue<int>() == 2, "Все страницы прочитаны без удалённых песен и дубликатов.");
        Expect(!requests.Any(p => p.Contains("/other") || p.Contains("/deleted")), "Не скачиваются другая группа и удалённые песни.");
        if (!repeatedCursor)
        {
            var song = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(options.Output, SongExporter.SafeFileName("a"))))!;
            Expect(song["content"]!["name"]!.ToString() == "Песня", "Unicode сохранён.");
            Expect(Directory.GetFiles(options.Output, "*.json").Length == (failure ? 2 : 3), "Одна песня — один JSON плюс manifest.");
        }
    }

    private static async Task RunRetryCase()
    {
        var attempts = 0;
        var token = "old";
        using var http = new HttpClient(new StubHandler(request =>
        {
            attempts++;
            if (attempts == 1)
            {
                Expect(request.Headers.Authorization!.Parameter == "old", "Первый токен.");
                token = "new";
                var retry = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                retry.Headers.RetryAfter = new(System.TimeSpan.Zero);
                return retry;
            }
            Expect(request.Headers.Authorization!.Parameter == "new", "Повтор использует обновлённый токен.");
            return Json("""{"items":[]}""");
        }));
        await new BandfixApi(http, "https://fixture.invalid/api", () => token).GetAsync("/bands", default);
        Expect(attempts == 2, "429 повторяется.");
    }

    private static async Task RunLoginCase()
    {
        var tracker = new SessionTracker();
        var calls = 0;
        void Observe(string token) => tracker.Observe("https://prod-bandfix-gateway.azurewebsites.net/api/users/42",
            new Dictionary<string, string> { ["authorization"] = "Bearer " + token });
        Observe("guest");
        using var http = new HttpClient(new StubHandler(request =>
        {
            calls++;
            if (request.Headers.Authorization!.Parameter == "guest")
            {
                Observe("stale");
                return Json("""{"items":[]}""");
            }
            if (request.Headers.Authorization.Parameter == "stale")
            {
                Observe("signed-in");
                return new(HttpStatusCode.Unauthorized);
            }
            Expect(request.Headers.Authorization.Parameter == "signed-in", "Используется токен после входа.");
            return Json("""{"items":[{"id":"youth","name":"MBV_Youth"}]}""");
        }));
        var session = await tracker.WaitForBandAsync(http, new GrabberOptions { LoginTimeout = TimeSpan.FromSeconds(5) }, default);
        Expect(calls == 3 && SongExporter.Items(session.Bands).Count == 1, "Гостевой и просроченный токены не завершают ожидание входа.");
    }

    private static void RunChromeDiscoveryCases(string root)
    {
        var profile = Path.Combine(root, "chrome profile");
        var args = new[] { "/usr/bin/google-chrome", "--user-data-dir=" + profile, "--remote-debugging-port=9223" };
        Expect(ChromeDiscovery.EndpointFromArguments(args, profile) == "http://127.0.0.1:9223", "Находится порт Chrome старого запуска без marker-файла.");
        Expect(ChromeDiscovery.EndpointFromCommandLine(string.Join(' ', args) + " --no-first-run https://bandfix.by/app/songs\0", profile)
            == "http://127.0.0.1:9223", "Chrome с argv, заменённым одной строкой заголовка, обнаруживается даже с пробелами в пути.");
        Expect(ChromeDiscovery.EndpointFromArguments(args, profile + "-other") is null, "Не подключаемся к чужому профилю.");
        Expect(ChromeDiscovery.EndpointFromArguments(["--user-data-dir", profile, "--remote-debugging-port", "9224"], profile)
            == "http://127.0.0.1:9224", "Поддерживаются параметры с пробелами.");
        Expect(ChromeDiscovery.EndpointFromArguments(["--user-data-dir=" + profile], profile) is null, "Обычное окно без CDP не считается доступным для подключения.");
        Expect(ChromeDiscovery.EndpointFromArguments([.. args, "--remote-debugging-address=0.0.0.0"], profile) is null, "Подключение ограничено loopback.");
        Expect(ChromeDiscovery.ActivePortEndpoint("43567\n/devtools/browser/test-id\n") == "ws://127.0.0.1:43567/devtools/browser/test-id", "Читается динамический порт Chrome.");
        Expect(ChromeDiscovery.ActivePortEndpoint("0\n/devtools/browser/test-id") is null, "Нулевой порт не готов.");
        Expect(ChromeDiscovery.ActivePortEndpoint("99999\n/devtools/browser/test-id") is null, "Недопустимый порт отвергается.");
        Expect(ChromeDiscovery.ActivePortEndpoint("43567\nhttps://example.com/") is null, "Некорректный DevToolsActivePort отвергается.");
        Directory.CreateDirectory(profile);
        File.WriteAllText(Path.Combine(profile, "DevToolsActivePort"), "43567\n/devtools/browser/test-id\n");
        Expect(ChromeDiscovery.Endpoints(profile).Contains("ws://127.0.0.1:43567/devtools/browser/test-id"), "Порт обнаруживается без собственного marker-файла.");
    }

    private static HttpResponseMessage Json(string data, string? cursor = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"data\":" + data + "}") };
        if (cursor is not null) response.Headers.Add("x-cursor", cursor);
        return response;
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Self-test: " + message);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
