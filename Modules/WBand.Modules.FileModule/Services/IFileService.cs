namespace WBand.Modules.FileModule.Services;

/// <summary>Provides private S3-compatible object operations.</summary>
public interface IFileService
{
    Task<string> GetSignedDownloadUrl(string objectKey);
    Task<string> GetSignedUploadUrl(string objectKey, string mimeType, long size);
    Task<StoredObjectInfo> InspectAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}

public sealed record StoredObjectInfo(long Size, string ContentType, string Sha256);
