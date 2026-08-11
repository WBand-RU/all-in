using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Minio;
using Shared;
using WBand.Modules.FileModule.Endpoints;
using WBand.Modules.FileModule.Services;

namespace WBand.Modules.FileModule;

public static class DependencyInjectionExtensions
{
    public static void AddFileModule(this IHostApplicationBuilder builder)
    {
        const string sectionPrefix = "Modules:FileModule:S3";
        var url =
            builder.Configuration[$"{sectionPrefix}:Url"] ?? EnvironmentVariable.Get("S3_HOST");
        var accessKey =
            builder.Configuration[$"{sectionPrefix}:AccessKey"]
            ?? EnvironmentVariable.Get("S3_ACCESS_KEY");
        var secretKey =
            builder.Configuration[$"{sectionPrefix}:SecretKey"]
            ?? EnvironmentVariable.Get("S3_SECRET_KEY");
        var region =
            builder.Configuration[$"{sectionPrefix}:Region"]
            ?? EnvironmentVariable.Get("S3_REGION");
        var bucket =
            builder.Configuration[$"{sectionPrefix}:Bucket"]
            ?? EnvironmentVariable.Get("S3_REGION");
        var uploadExpiration = int.Parse(
            builder.Configuration[$"{sectionPrefix}:UploadExpirationSeconds"]
                ?? EnvironmentVariable.Get("S3_UPLOAD_EXPIRATION_SECONDS")
        );
        var downloadExpiration = int.Parse(
            builder.Configuration[$"{sectionPrefix}:DownloadExpirationSeconds"]
                ?? EnvironmentVariable.Get("S3_DOWNLOAD_EXPIRATION_SECONDS")
        );

        builder.Services.AddMinio(options =>
        {
            options.WithEndpoint(new Uri($"s3://{accessKey}:{secretKey}@{url}/{region}"));
            options.WithCredentials(accessKey, secretKey);
            options.WithSSL();
        });

        builder.Services.AddScoped<IFileService, FileService>();

        builder.Services.Configure<FileServiceConfig>(x =>
        {
            x.Bucket = bucket;
            x.UploadExpirationSeconds = uploadExpiration;
            x.DownloadExpirationSeconds = downloadExpiration;
        });
    }

    public static void MapFileModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/files").WithTags("Files");

        group.MapPost("/download", DownloadFileEndpoint.GetDownloadUrl);
        group.MapPost("/upload", UploadFileEndpoint.GetUploadUrl);
    }
}
