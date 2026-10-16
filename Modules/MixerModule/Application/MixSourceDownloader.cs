using System.Security.Cryptography;
using WBand.Modules.FileModule.Contracts;

namespace WBand.Modules.MixerModule.Application;

/// <summary>Downloads verified sources with bounded attempts and reusable local caching.</summary>
public sealed class MixSourceDownloader(HttpClient client)
{
    /// <summary>Reuses a verified cached source or atomically installs a complete download.</summary>
    public async Task DownloadAsync(FileDownloadAccess source, string path,
        CancellationToken cancellationToken, TimeSpan? attemptTimeout = null)
    {
        if (await IsValidAsync(path, source, cancellationToken)) return;
        for (var attempt = 1; ; attempt++)
        {
            var temporary = $"{path}.{Guid.NewGuid():N}.part";
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(attemptTimeout ?? TimeSpan.FromMinutes(2));
                try
                {
                    using var response = await client.GetAsync(source.DownloadUrl,
                        HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    response.EnsureSuccessStatusCode();
                    await using (var output = File.Create(temporary))
                        await response.Content.CopyToAsync(output, timeout.Token);
                    if (!await IsValidAsync(temporary, source, timeout.Token))
                        throw new IOException($"Source {source.FileId} size or checksum does not match.");
                    File.Move(temporary, path, overwrite: true);
                    return;
                }
                catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Downloading source {source.FileId} timed out.", exception);
                }
            }
            catch (Exception exception) when (attempt < 3 &&
                exception is HttpRequestException or IOException or TimeoutException)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }

    private static async Task<bool> IsValidAsync(string path, FileDownloadAccess source,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || new FileInfo(path).Length != source.Size) return false;
        await using var input = File.OpenRead(path);
        var checksum = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken));
        return checksum.Equals(source.Sha256, StringComparison.OrdinalIgnoreCase);
    }
}
