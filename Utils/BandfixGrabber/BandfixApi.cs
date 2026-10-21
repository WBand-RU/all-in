using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace BandfixGrabber;

internal sealed class BandfixApi(HttpClient http, string baseUrl, Func<string> token)
{
    internal async Task<(JsonNode Data, string? Cursor)> GetAsync(string path, CancellationToken cancellation)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + path);
            var value = token();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? value[7..] : value);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await http.SendAsync(request, cancellation);
            if ((response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) && attempt < 4)
            {
                var wait = response.Headers.RetryAfter?.Delta
                    ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : TimeSpan.FromSeconds(1 << attempt));
                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(wait.TotalSeconds, 0, 60)), cancellation);
                continue;
            }
            if (!response.IsSuccessStatusCode) throw new BandfixApiException(response.StatusCode, path.Split('?')[0]);
            var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellation))
                ?? throw new InvalidDataException("Пустой JSON API.");
            var data = json["data"] ?? throw new InvalidDataException("Ответ API не содержит data.");
            var cursor = response.Headers.TryGetValues("x-cursor", out var values) ? values.FirstOrDefault() : null;
            return (data.DeepClone(), string.IsNullOrEmpty(cursor) ? null : cursor);
        }
    }
}

internal sealed class BandfixApiException(HttpStatusCode status, string path)
    : Exception($"API {path}: HTTP {(int)status}.")
{
    internal HttpStatusCode Status { get; } = status;
}
