using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Minio;
using Shared.Modules;
using WBand.Modules.FileModule.Infrastructure;
using WBand.Modules.FileModule.Services;

namespace WBand.Modules.FileModule;

/// <summary>
/// Composes file storage services and Wolverine HTTP endpoints.
/// </summary>
public sealed class FileModule : IWBandModule
{
    public string Name => "FileModule";

    public Assembly Assembly => typeof(FileModule).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        var storage = FileStorageOptions.FromConfiguration(builder.Configuration);

        builder
            .Services.AddOptions<FileStorageOptions>()
            .Configure(options => Copy(storage, options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddMinio(options =>
        {
            options.WithEndpoint(storage.Url).WithCredentials(storage.AccessKey, storage.SecretKey);

            if (!string.IsNullOrWhiteSpace(storage.Region))
            {
                options.WithRegion(storage.Region);
            }

            if (Uri.TryCreate(storage.Url, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps)
            {
                options.WithSSL();
            }
        });

        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddValidatorsFromAssembly(Assembly);
    }

    private static void Copy(FileStorageOptions source, FileStorageOptions target)
    {
        target.Url = source.Url;
        target.Bucket = source.Bucket;
        target.Region = source.Region;
        target.AccessKey = source.AccessKey;
        target.SecretKey = source.SecretKey;
        target.UploadExpirationSeconds = source.UploadExpirationSeconds;
        target.DownloadExpirationSeconds = source.DownloadExpirationSeconds;
    }
}
