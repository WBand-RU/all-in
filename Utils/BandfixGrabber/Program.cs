using BandfixGrabber;

try
{
    var options = GrabberOptions.Parse(args);
    if (options.Help)
    {
        Console.WriteLine(
            """
            BandfixGrabber — песни Bandfix в отдельные JSON-файлы.
            dotnet run --project Utils/BandfixGrabber

            --band NAME        Группа (по умолчанию MBV Youth)
            --output PATH      Папка JSON (по умолчанию <проект>/songs)
            --profile PATH     Отдельный профиль обычного Chrome
            --chrome PATH      Исполняемый файл Google Chrome
            --cdp URL          Подключиться к уже открытому Chrome на localhost
            --site URL         https://bandfix.by или https://bandfix.app
            --timeout MINUTES  Ожидание входа (по умолчанию 10 минут)
            --self-test        Проверить выгрузку на тестовых ответах API без входа
            --help             Справка

            Также можно задать BANDFIX_TOKEN в окружении для запуска без браузера.
            """
        );
        return 0;
    }
    if (options.SelfTest)
        return await GrabberSelfTest.RunAsync();

    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cancellation.Cancel();
    };
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    var token = Environment.GetEnvironmentVariable("BANDFIX_TOKEN");
    if (!string.IsNullOrWhiteSpace(token))
    {
        var api = new BandfixApi(http, options.ApiBase, () => token);
        return await new SongExporter(api).ExportAsync(options, cancellation.Token);
    }

    await using var browser = await BrowserSession.OpenAsync(options, cancellation.Token);
    var authenticated = await browser.WaitForBandAsync(http, options, cancellation.Token);
    return await new SongExporter(authenticated.Api).ExportAsync(
        options,
        cancellation.Token,
        authenticated.Bands
    );
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Выгрузка отменена.");
    return 130;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Выгрузка не завершена: {ex.Message}");
    return 1;
}
