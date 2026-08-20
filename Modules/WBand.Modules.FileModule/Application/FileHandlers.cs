using Marten;
using Microsoft.Extensions.Options;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.FileModule.Domain;
using WBand.Modules.FileModule.Infrastructure;
using WBand.Modules.FileModule.Services;
using Minio.Exceptions;

namespace WBand.Modules.FileModule.Application;

public static class CreateFileUploadHandler
{
    public static async Task<CreateFileUploadResult> Handle(
        CreateFileUpload command,
        IDocumentSession session,
        IFileService fileService,
        IOptions<FileStorageOptions> options,
        CancellationToken cancellationToken)
    {
        var error = FileUploadRules.Validate(command.MimeType, command.Size, command.Sha256,
            options.Value);
        if (error is not null) return CreateFileUploadResult.Rejected(error);
        if (!FileUploadRules.IsSafeObjectKey(command.ObjectKey, command.BandId, command.FileId))
            return CreateFileUploadResult.Rejected("file_object_key_invalid");

        var now = DateTimeOffset.UtcNow;
        var file = new FileObject
        {
            Id = command.FileId,
            BandId = command.BandId,
            ObjectKey = command.ObjectKey,
            OriginalFileName = Path.GetFileName(command.OriginalFileName),
            MimeType = FileUploadRules.NormalizeMime(command.MimeType),
            Size = command.Size,
            Sha256 = command.Sha256.ToLowerInvariant(),
            CreatedAt = now,
            CreatedBy = command.CreatedBy,
            ExpiresAt = now.AddMinutes(options.Value.IncompleteUploadLifetimeMinutes),
        };
        session.Store(file);
        await session.SaveChangesAsync(cancellationToken);

        var url = await fileService.GetSignedUploadUrl(file.ObjectKey, file.MimeType, file.Size);
        return CreateFileUploadResult.Success(new FileUploadTicket(
            file.Id, url, file.ExpiresAt, file.MimeType, file.Size));
    }
}

public static class GetFileReferencesHandler
{
    public static async Task<FileReference[]> Handle(
        GetFileReferences query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        var ids = query.FileIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var files = await session.Query<FileObject>()
            .Where(file => ids.Contains(file.Id))
            .ToListAsync(cancellationToken);
        return files.Select(file => new FileReference(file.Id, file.BandId, file.Status,
            file.MimeType, file.Size, file.Sha256)).ToArray();
    }
}

public static class GetReadyFileDownloadsHandler
{
    public static async Task<FileDownloadAccess[]> Handle(GetReadyFileDownloads query,
        IQuerySession session, IFileService fileService, CancellationToken cancellationToken)
    {
        var ids = query.FileIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var files = await session.Query<FileObject>().Where(file => ids.Contains(file.Id) &&
            file.Status == FileObjectStatus.Ready).ToListAsync(cancellationToken);
        var result = new List<FileDownloadAccess>(files.Count);
        foreach (var file in files)
            result.Add(new FileDownloadAccess(file.Id, file.BandId,
                await fileService.GetSignedDownloadUrl(file.ObjectKey), file.MimeType,
                file.Size, file.Sha256));
        return result.ToArray();
    }
}

public static class CompleteFileUploadHandler
{
    public static async Task<CompleteFileUploadResult> Handle(CompleteFileUpload command,
        IDocumentSession session, IFileService storage, CancellationToken cancellationToken)
    {
        var file = await session.LoadAsync<FileObject>(command.FileId, cancellationToken);
        if (file is null) return CompleteFileUploadResult.Rejected("file_not_found");
        if (file.Status == FileObjectStatus.Ready) return CompleteFileUploadResult.Success(ToReference(file));
        if (file.Status != FileObjectStatus.Pending || file.ExpiresAt <= DateTimeOffset.UtcNow)
            return CompleteFileUploadResult.Rejected("file_upload_not_pending");
        StoredObjectInfo actual;
        try { actual = await storage.InspectAsync(file.ObjectKey, cancellationToken); }
        catch (MinioException) { return CompleteFileUploadResult.Rejected("file_object_unavailable"); }
        var mime = FileUploadRules.NormalizeMime(actual.ContentType);
        var rejection = actual.Size != file.Size ? "file_size_mismatch"
            : !string.Equals(mime, file.MimeType, StringComparison.OrdinalIgnoreCase) ? "file_mime_mismatch"
            : !string.Equals(actual.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase)
                ? "file_sha256_mismatch" : null;
        if (rejection is not null)
        {
            await storage.DeleteAsync(file.ObjectKey, cancellationToken);
            file.Status = FileObjectStatus.Rejected; file.RejectionReason = rejection;
            session.Store(file); await session.SaveChangesAsync(cancellationToken);
            return CompleteFileUploadResult.Rejected(rejection);
        }
        file.Status = FileObjectStatus.Ready; file.CompletedAt = DateTimeOffset.UtcNow;
        session.Store(file); await session.SaveChangesAsync(cancellationToken);
        return CompleteFileUploadResult.Success(ToReference(file));
    }

    private static FileReference ToReference(FileObject file) => new(file.Id, file.BandId,
        file.Status, file.MimeType, file.Size, file.Sha256);
}

public static class FileUploadRules
{
    public static string? Validate(string? mimeType, long size, string? sha256,
        FileStorageOptions options)
    {
        if (size <= 0 || size > options.MaxFileSizeBytes) return "file_size_not_allowed";
        if (!options.AllowedMimeTypes.Contains(NormalizeMime(mimeType),
            StringComparer.OrdinalIgnoreCase)) return "file_mime_not_allowed";
        return sha256 is { Length: 64 } && sha256.All(Uri.IsHexDigit)
            ? null
            : "file_sha256_invalid";
    }

    public static string NormalizeMime(string? mimeType) =>
        (mimeType ?? string.Empty).Split(';', 2)[0].Trim().ToLowerInvariant();

    public static bool IsSafeObjectKey(string? objectKey, Guid bandId, Guid fileId) =>
        objectKey is not null && !objectKey.Contains("..", StringComparison.Ordinal) &&
        objectKey.StartsWith($"bands/{bandId:D}/", StringComparison.Ordinal) &&
        objectKey.Contains($"/{fileId:D}/", StringComparison.Ordinal);
}
