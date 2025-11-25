using Minio;
using Minio.DataModel.Args;

namespace PlaybackService.Services;

/// <summary>
/// Service for handling file storage in MinIO
/// </summary>
public sealed class MinioStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;
    private const string BucketName = "playbacks";

    public MinioStorageService(IMinioClient minioClient, ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(BucketName);
            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(BucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
                _logger.LogInformation("Created bucket: {BucketName}", BucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring bucket exists");
            throw;
        }
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType
    )
    {
        await EnsureBucketExistsAsync();

        var objectKey = $"{Guid.NewGuid()}/{fileName}";

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs);

        return objectKey;
    }

    public async Task<Stream> GetFileAsync(string objectKey)
    {
        var memoryStream = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await _minioClient.GetObjectAsync(getObjectArgs);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<string> GetPresignedUrlAsync(string objectKey, int expiryInSeconds = 3600)
    {
        var presignedGetObjectArgs = new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithExpiry(expiryInSeconds);

        return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
    }

    public async Task DeleteFileAsync(string objectKey)
    {
        var removeObjectArgs = new RemoveObjectArgs().WithBucket(BucketName).WithObject(objectKey);

        await _minioClient.RemoveObjectAsync(removeObjectArgs);
    }
}
