using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BandfixGrabber;

internal sealed class SongExporter(BandfixApi api)
{
    internal static JsonArray Items(JsonNode data) => data["items"] as JsonArray
        ?? throw new InvalidDataException("Ответ API не содержит массив items.");

    internal static bool SameBand(string? left, string right) => Normalize(left) == Normalize(right);

    private static string Normalize(string? value) => string.Concat((value ?? "").Normalize(NormalizationForm.FormKC)
        .Where(c => !char.IsWhiteSpace(c) && c is not ('_' or '-')).Select(char.ToUpperInvariant));

    internal async Task<int> ExportAsync(GrabberOptions options, CancellationToken cancellation, JsonNode? bands = null)
    {
        bands ??= (await api.GetAsync("/bands", cancellation)).Data;
        var matches = Items(bands).Where(b => SameBand(b?["name"]?.ToString(), options.Band)).ToList();
        if (matches.Count != 1)
            throw new InvalidOperationException($"Группа '{options.Band}': совпадений {matches.Count}. Доступны: {string.Join(", ", Items(bands).Select(b => b?["name"]?.ToString()))}.");
        var band = matches[0]!;
        var bandId = RequiredId(band);
        var bandName = band["name"]!.ToString();
        Console.WriteLine($"Группа: {bandName} ({bandId}).");
        Directory.CreateDirectory(options.Output);
        var manifestPath = Path.Combine(options.Output, "_manifest.json");
        if (File.Exists(manifestPath))
        {
            var previous = JsonNode.Parse(await File.ReadAllTextAsync(manifestPath, cancellation));
            if (previous?["bandId"]?.ToString() != bandId)
                throw new InvalidOperationException("В папке уже есть выгрузка другой группы. Укажите другую --output.");
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var exported = new List<object>();
        var failures = new List<object>();
        var complete = false;
        string? fatalError = null;
        await SaveManifestAsync();
        try
        {
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            string? cursor = null;
            do
            {
                cancellation.ThrowIfCancellationRequested();
                var suffix = cursor is null ? "" : "?cursor=" + Uri.EscapeDataString(cursor);
                var result = await api.GetAsync($"/songs/band/{Uri.EscapeDataString(bandId)}{suffix}", cancellation);
                foreach (var item in Items(result.Data))
                {
                    if (item is null) throw new InvalidDataException("API вернул null вместо песни.");
                    if (IsDeleted(item)) continue;
                    ids.Add(RequiredId(item));
                }
                Console.WriteLine($"Найдено песен: {ids.Count}.");
                cursor = result.Cursor;
                if (cursor is not null && !cursors.Add(cursor)) throw new InvalidDataException("API повторил курсор страницы; полнота выгрузки не подтверждена.");
            } while (cursor is not null);

            foreach (var id in ids.Order(StringComparer.Ordinal))
            {
                cancellation.ThrowIfCancellationRequested();
                try
                {
                    var song = (await api.GetAsync("/songs/" + Uri.EscapeDataString(id), cancellation)).Data;
                    if (RequiredId(song) != id) throw new InvalidDataException("API вернул другой ID песни.");
                    if (song["bandId"] is { } owner && owner.ToString() != bandId)
                        throw new InvalidDataException("API вернул песню другой группы.");
                    if (IsDeleted(song)) throw new InvalidDataException("Песня удалена во время выгрузки.");
                    // Hex encoding is collision-free and safe even for IDs containing path separators.
                    var filename = SafeFileName(id);
                    await WriteJsonAsync(Path.Combine(options.Output, filename), song);
                    exported.Add(new { id, file = filename });
                    Console.WriteLine($"Сохранено {exported.Count}/{ids.Count}: {song["content"]?["name"] ?? song["name"] ?? JsonValue.Create(id)}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failures.Add(new { id, error = ex.Message });
                    Console.Error.WriteLine($"Ошибка песни {id}: {ex.Message}");
                }
            }
            complete = failures.Count == 0;
        }
        catch (Exception ex)
        {
            fatalError = ex is OperationCanceledException ? "Выгрузка отменена." : ex.Message;
            throw;
        }
        finally { await SaveManifestAsync(); }
        Console.WriteLine($"Сохранено: {exported.Count}/{ids.Count}. Папка: {options.Output}");
        return complete ? 0 : 1;

        Task SaveManifestAsync() => WriteJsonAsync(manifestPath, new
        {
            source = options.Site, bandId, bandName, exportedAtUtc = DateTimeOffset.UtcNow,
            discovered = ids.Count, exported = exported.Count, complete, fatalError, songs = exported, failures
        });
    }

    internal static string SafeFileName(string id) => "id-" + Convert.ToHexString(Encoding.UTF8.GetBytes(id)) + ".json";

    private static bool IsDeleted(JsonNode node) => node["isDeleted"]?.ToString().ToLowerInvariant() is "true" or "1";

    private static string RequiredId(JsonNode node) => node["id"] is JsonValue value && value.TryGetValue<string>(out var id)
        && !string.IsNullOrWhiteSpace(id) && Encoding.UTF8.GetByteCount(id) <= 100
        ? id : throw new InvalidDataException("API вернул отсутствующий или неподдерживаемый ID.");

    private static async Task WriteJsonAsync(string path, object value)
    {
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
        File.Move(temporary, path, overwrite: true);
    }
}
