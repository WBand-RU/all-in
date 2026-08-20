using System.Reflection;
using FluentValidation;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Minio;
using Shared.Modules;
using WBand.Modules.FileModule.Infrastructure;
using WBand.Modules.FileModule.Domain;
using WBand.Modules.FileModule.Services;

namespace WBand.Modules.FileModule;

/// <summary>
/// Composes file storage services and Wolverine HTTP endpoints.
/// </summary>
public sealed class FileModule : IWBandModule
{
    public string Name => "FileModule";

    public Assembly Assembly => typeof(FileModule).Assembly;

    public string MartenSchemaName => "files";

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
            var uri = new Uri(storage.Url, UriKind.Absolute);
            var endpoint = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

            options.WithEndpoint(endpoint).WithCredentials(storage.AccessKey, storage.SecretKey);

            if (!string.IsNullOrWhiteSpace(storage.Region))
            {
                options.WithRegion(storage.Region);
            }

            if (uri.Scheme == Uri.UriSchemeHttps)
            {
                options.WithSSL();
            }
        });

        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddHostedService<FileUploadCleanupService>();
        builder.Services.AddValidatorsFromAssembly(Assembly);
    }

    public void ConfigureMarten(StoreOptions options) =>
        options.Schema.For<FileObject>().DatabaseSchemaName(MartenSchemaName);

    private static void Copy(FileStorageOptions source, FileStorageOptions target)
    {
        target.Url = source.Url;
        target.Bucket = source.Bucket;
        target.Region = source.Region;
        target.AccessKey = source.AccessKey;
        target.SecretKey = source.SecretKey;
        target.UploadExpirationSeconds = source.UploadExpirationSeconds;
        target.DownloadExpirationSeconds = source.DownloadExpirationSeconds;
        target.MaxFileSizeBytes = source.MaxFileSizeBytes;
        target.IncompleteUploadLifetimeMinutes = source.IncompleteUploadLifetimeMinutes;
        target.CleanupIntervalMinutes = source.CleanupIntervalMinutes;
        target.AllowedMimeTypes = source.AllowedMimeTypes;
    }
}
