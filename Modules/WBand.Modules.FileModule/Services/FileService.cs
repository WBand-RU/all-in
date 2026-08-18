using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using WBand.Modules.FileModule.Infrastructure;

namespace WBand.Modules.FileModule.Services;

internal sealed class FileService(IMinioClient minioClient, IOptions<FileStorageOptions> options)
    : IFileService
{
    private readonly string bucket = options.Value.Bucket;
    private readonly int uploadExpirationSeconds = options.Value.UploadExpirationSeconds;
    private readonly int downloadExpirationSeconds = options.Value.DownloadExpirationSeconds;

    public async Task<string> GetSignedUploadUrl(string fileName)
    {
        return await minioClient.PresignedPutObjectAsync(
            new PresignedPutObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName)
                .WithExpiry(uploadExpirationSeconds)
        );
    }

    public async Task<string> GetSignedDownloadUrl(string fileName)
    {
        return await minioClient.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName)
                .WithExpiry(downloadExpirationSeconds)
        );
    }
}
