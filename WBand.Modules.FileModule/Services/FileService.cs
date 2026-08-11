using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace WBand.Modules.FileModule.Services;

internal class FileService(IMinioClient minioClient, IOptions<FileServiceConfig> options)
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
