using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;

namespace BandfixGrabber;

internal sealed class SessionTracker
{
    private readonly ConcurrentQueue<string> changedBases = new();
    private readonly ConcurrentDictionary<string, string> latestTokens = new();

    internal void Observe(string url, IReadOnlyDictionary<string, string> headers)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || uri.Host is not ("prod-bandfix-gateway.azurewebsites.net" or "bandfix-be.apps.nullflip.com")
            || !uri.AbsolutePath.StartsWith("/api/", StringComparison.Ordinal)
            || !headers.TryGetValue("authorization", out var token) || !token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return;
        var baseUrl = uri.GetLeftPart(UriPartial.Authority) + "/api";
        if (latestTokens.TryGetValue(baseUrl, out var previous) && previous == token) return;
        latestTokens[baseUrl] = token;
        changedBases.Enqueue(baseUrl);
    }

    internal async Task<(BandfixApi Api, JsonNode Bands)> WaitForBandAsync(HttpClient http, GrabberOptions options, CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(options.LoginTimeout);
        JsonNode? lastBands = null;
        var candidates = new Dictionary<string, BandfixApi>();
        var nextProbe = DateTimeOffset.MinValue;
        try
        {
            while (true)
            {
                while (changedBases.TryDequeue(out var baseUrl))
                {
                    candidates[baseUrl] = new BandfixApi(http, baseUrl, () => latestTokens[baseUrl]);
                    nextProbe = DateTimeOffset.MinValue;
                }
                if (DateTimeOffset.UtcNow >= nextProbe)
                {
                    foreach (var api in candidates.Values)
                    {
                        try
                        {
                            var bands = (await api.GetAsync("/bands", timeout.Token)).Data;
                            var items = SongExporter.Items(bands);
                            lastBands = bands;
                            if (items.Any(b => SongExporter.SameBand(b?["name"]?.ToString(), options.Band)))
                                return (api, bands);
                            // Empty bands can belong to a guest session, not the account selected in Google.
                        }
                        catch (BandfixApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { }
                        catch (HttpRequestException) { /* Keep waiting during a temporary network outage. */ }
                        catch (OperationCanceledException) when (!timeout.IsCancellationRequested) { }
                    }
                    nextProbe = DateTimeOffset.UtcNow.AddSeconds(10);
                }
                await Task.Delay(500, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
        {
            var available = lastBands is null ? "нет авторизованного ответа" : string.Join(", ", SongExporter.Items(lastBands).Select(b => b?["name"]?.ToString()));
            if (available.Length == 0) available = "список пуст";
            throw new TimeoutException($"За {options.LoginTimeout.TotalMinutes:g} мин. не получен доступ к '{options.Band}'. Группы: {available}. Chrome оставлен открытым; завершите вход и повторите команду.");
        }
    }
}
