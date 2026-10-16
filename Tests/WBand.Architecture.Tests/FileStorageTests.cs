using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WBand.Modules.FileModule;
using WBand.Modules.FileModule.Infrastructure;
using WBand.Modules.FileModule.Services;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class FileStorageTests
{
    [Theory]
    [InlineData("http://localhost:20219")]
    [InlineData("https://storage.example.com:9443")]
    public async Task SignedUrls_WhenStorageSchemeIsConfigured_UseThatScheme(string endpoint)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{FileStorageOptions.SectionName}:Url"] = endpoint,
            [$"{FileStorageOptions.SectionName}:Bucket"] = "test-bucket",
            [$"{FileStorageOptions.SectionName}:Region"] = "us-east-1",
            [$"{FileStorageOptions.SectionName}:AccessKey"] = "test-access-key",
            [$"{FileStorageOptions.SectionName}:SecretKey"] = "test-secret-key",
            [$"{FileStorageOptions.SectionName}:UploadExpirationSeconds"] = "300",
            [$"{FileStorageOptions.SectionName}:DownloadExpirationSeconds"] = "300",
        });
        FileModule.AddStorageServices(builder, addUploadCleanup: false);
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var files = scope.ServiceProvider.GetRequiredService<IFileService>();
        var expected = new Uri(endpoint);

        var upload = await files.GetSignedUploadUrl("stem.wav", "audio/wav", 100);
        var download = await files.GetSignedDownloadUrl("stem.wav");

        foreach (var url in new[] { upload, download })
        {
            var actual = new Uri(url);
            Assert.Equal(expected.Scheme, actual.Scheme);
            Assert.Equal(expected.Authority, actual.Authority);
            Assert.Equal("/test-bucket/stem.wav", actual.AbsolutePath);
            Assert.Contains("X-Amz-Signature=", actual.Query);
        }
    }
}
