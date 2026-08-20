using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio.Exceptions;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.FileModule.Application;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.FileModule.Domain;
using WBand.Modules.FileModule.Infrastructure;
using WBand.Modules.FileModule.Services;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.FileModule.Endpoints;

public sealed record PresignUploadRequest(
    Guid BandId, string FileName, string MimeType, long Size, string Sha256);
public sealed record PresignDownloadRequest(Guid FileId);
public sealed record SignedUrlResponse(Guid FileId, string SignedUrl, DateTimeOffset? ExpiresAt = null);

public sealed class PresignUploadRequestValidator : AbstractValidator<PresignUploadRequest>
{
    public PresignUploadRequestValidator(IOptions<FileStorageOptions> options)
    {
        RuleFor(request => request.BandId).NotEmpty();
        RuleFor(request => request.FileName).NotEmpty().MaximumLength(255);
        RuleFor(request => request).Custom((request, context) =>
        {
            var error = FileUploadRules.Validate(request.MimeType, request.Size,
                request.Sha256, options.Value);
            if (error is not null) context.AddFailure(error);
        });
    }
}

public static class PresignUploadEndpoint
{
    [Authorize]
    [WolverinePost("/files/presign-upload")]
    public static async Task<IResult> Post(
        PresignUploadRequest request,
        ICurrentUser user,
        IMessageBus bus)
    {
        if (!await bus.InvokeAsync<bool>(new CheckBandPermission(request.BandId,
            user.GetUserId, BandPermission.EditContent))) return Results.Forbid();

        var fileId = Guid.CreateVersion7();
        var safeName = string.Concat(Path.GetFileName(request.FileName).Select(character =>
            char.IsLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '_'));
        var key = $"bands/{request.BandId:D}/files/{fileId:D}/{safeName}";
        var result = await bus.InvokeAsync<CreateFileUploadResult>(new CreateFileUpload(fileId,
            request.BandId, key, request.FileName, request.MimeType, request.Size,
            request.Sha256, user.GetUserId));
        if (result.Ticket is not { } ticket)
            return Results.BadRequest(new { code = result.Error });
        return Results.Ok(new SignedUrlResponse(ticket.FileId, ticket.UploadUrl, ticket.ExpiresAt));
    }
}

public static class CompleteUploadEndpoint
{
    [Authorize]
    [WolverinePost("/files/{fileId}/complete")]
    public static async Task<IResult> Post(
        Guid fileId,
        ICurrentUser user,
        IDocumentSession session,
        IFileService storage,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var file = await session.LoadAsync<FileObject>(fileId, cancellationToken);
        if (file is null) return Results.NotFound();
        if (!await bus.InvokeAsync<bool>(new CheckBandPermission(file.BandId,
            user.GetUserId, BandPermission.EditContent))) return Results.Forbid();
        if (file.Status == FileObjectStatus.Ready) return Results.Ok(ToReference(file));
        if (file.Status != FileObjectStatus.Pending || file.ExpiresAt <= DateTimeOffset.UtcNow)
            return Results.Conflict(new { code = "file_upload_not_pending" });

        StoredObjectInfo actual;
        try
        {
            actual = await storage.InspectAsync(file.ObjectKey, cancellationToken);
        }
        catch (MinioException)
        {
            return Results.Conflict(new { code = "file_object_unavailable" });
        }

        var mime = FileUploadRules.NormalizeMime(actual.ContentType);
        var rejection = actual.Size != file.Size ? "file_size_mismatch"
            : !string.Equals(mime, file.MimeType, StringComparison.OrdinalIgnoreCase)
                ? "file_mime_mismatch"
                : !string.Equals(actual.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase)
                    ? "file_sha256_mismatch"
                    : null;
        if (rejection is not null)
        {
            await storage.DeleteAsync(file.ObjectKey, cancellationToken);
            file.Status = FileObjectStatus.Rejected;
            file.RejectionReason = rejection;
            session.Store(file);
            await session.SaveChangesAsync(cancellationToken);
            return Results.BadRequest(new { code = rejection });
        }

        file.Status = FileObjectStatus.Ready;
        file.CompletedAt = DateTimeOffset.UtcNow;
        session.Store(file);
        await session.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToReference(file));
    }

    private static FileReference ToReference(FileObject file) => new(file.Id, file.BandId,
        file.Status, file.MimeType, file.Size, file.Sha256);
}

public static class PresignDownloadEndpoint
{
    [Authorize]
    [WolverinePost("/files/presign-download")]
    public static async Task<IResult> Post(
        PresignDownloadRequest request,
        ICurrentUser user,
        IQuerySession session,
        IFileService storage,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var file = await session.LoadAsync<FileObject>(request.FileId, cancellationToken);
        if (file is null || file.Status != FileObjectStatus.Ready) return Results.NotFound();
        if (!await bus.InvokeAsync<bool>(new CheckBandPermission(file.BandId,
            user.GetUserId, BandPermission.View))) return Results.Forbid();
        return Results.Ok(new SignedUrlResponse(file.Id,
            await storage.GetSignedDownloadUrl(file.ObjectKey)));
    }
}
