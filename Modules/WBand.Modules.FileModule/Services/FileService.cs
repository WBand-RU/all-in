using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using WBand.Modules.FileModule.Infrastructure;

namespace WBand.Modules.FileModule.Services;

public sealed class FileService(IMinioClient minioClient, IOptions<FileStorageOptions> options)
    : IFileService
{
    private readonly string bucket = options.Value.Bucket;
    private readonly int uploadExpirationSeconds = options.Value.UploadExpirationSeconds;
    private readonly int downloadExpirationSeconds = options.Value.DownloadExpirationSeconds;

    public async Task<string> GetSignedUploadUrl(string objectKey, string mimeType, long size)
    {
        return await minioClient.PresignedPutObjectAsync(
            new PresignedPutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithHeaders(new Dictionary<string, string>
                {
                    ["Content-Type"] = mimeType,
                    ["Content-Length"] = size.ToString(System.Globalization.CultureInfo.InvariantCulture),
                })
                .WithExpiry(uploadExpirationSeconds)
        );
    }

    public async Task<string> GetSignedDownloadUrl(string objectKey)
    {
        return await minioClient.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry(downloadExpirationSeconds)
        );
    }

    public async Task<StoredObjectInfo> InspectAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        var stat = await minioClient.StatObjectAsync(
            new StatObjectArgs().WithBucket(bucket).WithObject(objectKey),
            cancellationToken);

        byte[]? digest = null;
        await minioClient.GetObjectAsync(
            new GetObjectArgs().WithBucket(bucket).WithObject(objectKey)
                .WithCallbackStream(stream => digest = SHA256.HashData(stream)),
            cancellationToken);

        return new StoredObjectInfo(stat.Size, stat.ContentType ?? "application/octet-stream",
            Convert.ToHexString(digest!).ToLowerInvariant());
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        minioClient.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(bucket).WithObject(objectKey),
            cancellationToken);
}
