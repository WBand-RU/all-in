using System.Net;
using System.Security.Cryptography;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.MixerModule.Application;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class MixSourceDownloaderTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"mix-source-test-{Guid.NewGuid():N}");
    private readonly byte[] bytes = [1, 2, 3, 4, 5];

    public MixSourceDownloaderTests() => Directory.CreateDirectory(directory);
    private string FilePath => Path.Combine(directory, "source.wav");
    private FileDownloadAccess Source => new(Guid.NewGuid(), Guid.NewGuid(), "https://storage.test/source",
        "audio/wav", bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)));

    [Fact]
    public async Task Download_WhenSourceIsCached_DoesNotRequestItAgain()
    {
        var requests = 0;
        using var client = new HttpClient(new TestHandler((_, _) => {
            requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent(bytes) });
        }));
        var downloader = new MixSourceDownloader(client); var source = Source;
        await downloader.DownloadAsync(source, FilePath, default);
        await downloader.DownloadAsync(source, FilePath, default);
        Assert.Equal(1, requests);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(FilePath));
    }

    [Fact]
    public async Task Download_WhenNetworkFails_RetriesWithoutInstallingPartialFile()
    {
        var requests = 0;
        using var client = new HttpClient(new TestHandler((_, _) => {
            if (++requests == 1) throw new HttpRequestException("Connection reset");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent(bytes) });
        }));
        await new MixSourceDownloader(client).DownloadAsync(Source, FilePath, default);
        Assert.Equal(2, requests);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(FilePath));
        Assert.Single(Directory.GetFiles(directory));
    }

    [Fact]
    public async Task Download_WhenChecksumIsWrong_RejectsItAfterBoundedAttempts()
    {
        var requests = 0;
        using var client = new HttpClient(new TestHandler((_, _) => {
            requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new ByteArrayContent([9, 9, 9, 9, 9]) });
        }));
        await Assert.ThrowsAsync<IOException>(() => new MixSourceDownloader(client)
            .DownloadAsync(Source, FilePath, default));
        Assert.Equal(3, requests);
        Assert.Empty(Directory.GetFiles(directory));
    }

    [Fact]
    public async Task Download_WhenRequestStalls_TimesOutAndStopsRetrying()
    {
        var requests = 0;
        using var client = new HttpClient(new TestHandler(async (_, token) => {
            requests++;
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        await Assert.ThrowsAsync<TimeoutException>(() => new MixSourceDownloader(client)
            .DownloadAsync(Source, FilePath, default, TimeSpan.FromMilliseconds(20)));
        Assert.Equal(3, requests);
        Assert.Empty(Directory.GetFiles(directory));
    }

    [Fact]
    public async Task Download_WhenWorkerStops_CancelsWithoutRetrying()
    {
        var requests = 0;
        using var client = new HttpClient(new TestHandler(async (_, token) => {
            requests++;
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        using var stopping = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new MixSourceDownloader(client)
            .DownloadAsync(Source, FilePath, stopping.Token));
        Assert.Equal(1, requests);
        Assert.Empty(Directory.GetFiles(directory));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    private sealed class TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
