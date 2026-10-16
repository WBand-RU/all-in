using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.StemModule.Contracts;
using WBand.Modules.StemModule.Domain;
using WBand.Modules.StemModule.Infrastructure;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

namespace WBand.Modules.StemModule.Endpoints;

/// <summary>Refreshes a signed URL immediately before uploading a staged file.</summary>
public static class RefreshStemUploadUrlEndpoint
{
    [Authorize]
    [WolverinePost("/stem-api/songs/{songId}/stem-uploads/{batchId}/files/{fileId}/upload-url")]
    public static async Task<IResult> Post(
        Guid songId,
        Guid batchId,
        Guid fileId,
        ICurrentUser user,
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var batch = await session.LoadAsync<StemUploadBatch>(batchId, cancellationToken);
        if (batch is null || batch.SongId != songId || batch.CreatedBy != user.Id)
            return Results.NotFound();
        if (
            !await bus.InvokeAsync<bool>(
                new CheckBandPermission(batch.BandId, user.Id, BandPermission.EditContent),
                cancellationToken
            )
        )
            return Results.Forbid();
        if (batch.Status != StemUploadStatus.Uploading || batch.ExpiresAt <= DateTimeOffset.UtcNow)
            return Results.Conflict(new { code = "stem_upload_closed" });
        var draft = batch.Files.SingleOrDefault(x => x.Stem.FileId == fileId);
        if (draft is null)
            return Results.NotFound();
        var upload = await bus.InvokeAsync<CreateFileUploadResult>(
            new CreateFileUpload(
                fileId,
                batch.BandId,
                draft.ObjectKey,
                draft.FileName,
                draft.MimeType,
                draft.Size,
                draft.Sha256,
                batch.CreatedBy
            ),
            cancellationToken
        );
        if (upload.Ticket is not { } ticket)
            return Results.Conflict(new { code = upload.Error });
        if (batch.CleanupUntil < ticket.ExpiresAt.AddMinutes(10))
            batch.CleanupUntil = ticket.ExpiresAt.AddMinutes(10);
        session.Store(batch);
        await session.SaveChangesAsync(cancellationToken);
        return Results.Ok(ticket);
    }
}

/// <summary>Commits all staged stems in one transaction after every source file is verified.</summary>
public static class CompleteStemBatchEndpoint
{
    [Authorize]
    [WolverinePost("/stem-api/songs/{songId}/stem-uploads/{batchId}/complete")]
    public static async Task<IResult> Post(
        Guid songId,
        Guid batchId,
        ICurrentUser user,
        IDocumentSession session,
        IMessageBus bus,
        IMartenOutbox outbox,
        CancellationToken cancellationToken
    )
    {
        var batch = await session.LoadAsync<StemUploadBatch>(batchId, cancellationToken);
        if (batch is null || batch.SongId != songId || batch.CreatedBy != user.Id)
            return Results.NotFound();
        if (
            !await bus.InvokeAsync<bool>(
                new CheckBandPermission(batch.BandId, user.Id, BandPermission.EditContent),
                cancellationToken
            )
        )
            return Results.Forbid();
        if (batch.Status == StemUploadStatus.Completed)
            return Results.NoContent();
        if (batch.Status == StemUploadStatus.Aborted || batch.ExpiresAt <= DateTimeOffset.UtcNow)
            return Results.Conflict(new { code = "stem_upload_closed" });
        var song = await StemEndpointSupport.GetSong(bus, songId);
        if (song is null)
            return Results.NotFound();
        var files = await bus.InvokeAsync<FileReference[]>(
            new GetFileReferences(batch.Files.Select(x => x.Stem.FileId).ToArray()),
            cancellationToken
        );
        if (!StemUploadRules.AllFilesReady(batch, files))
            return Results.Conflict(new { code = "stem_upload_incomplete" });
        foreach (var draft in batch.Files)
            if (
                !await StemEndpointSupport.GroupBelongsToSong(
                    session,
                    draft.Stem.GroupId,
                    songId,
                    cancellationToken
                )
            )
                return Results.Conflict(new { code = "stem_group_outside_song" });

        var existing = (
            await session
                .Query<Stem>()
                .Where(x => x.SongId == songId)
                .ToListAsync(cancellationToken)
        ).ToList();
        foreach (var draft in batch.Files)
        {
            draft.Stem.Order = StemEndpointSupport.NextOrder(existing, draft.Stem.GroupId);
            session.Store(draft.Stem);
            existing.Add(draft.Stem);
        }
        var collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        batch.Status = StemUploadStatus.Completed;
        session.Store(batch);
        outbox.Enroll(session);
        await outbox.PublishAsync(
            new StemsChanged(songId, batch.BandId, collection.ContentVersion)
        );
        await session.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}

/// <summary>Closes an uncommitted upload before deleting its files; cleanup is retried in the background.</summary>
public static class AbortStemBatchEndpoint
{
    [Authorize]
    [WolverineDelete("/stem-api/songs/{songId}/stem-uploads/{batchId}")]
    public static async Task<IResult> Delete(
        Guid songId,
        Guid batchId,
        ICurrentUser user,
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var batch = await session.LoadAsync<StemUploadBatch>(batchId, cancellationToken);
        if (batch is null)
        {
            var song = await StemEndpointSupport.GetSong(bus, songId);
            if (song is null)
                return Results.NotFound();
            if (!await StemEndpointSupport.Can(bus, song, user.Id, BandPermission.EditContent))
                return Results.Forbid();
            // A cancellation may arrive before the initialization request finishes.
            batch = new StemUploadBatch
            {
                Id = batchId,
                SongId = songId,
                BandId = song.BandId,
                CreatedBy = user.Id,
                Status = StemUploadStatus.Aborted,
                ExpiresAt = DateTimeOffset.UtcNow,
                CleanupUntil = DateTimeOffset.UtcNow.AddHours(1),
            };
        }
        if (batch.SongId != songId || batch.CreatedBy != user.Id)
            return Results.NotFound();
        if (
            !await bus.InvokeAsync<bool>(
                new CheckBandPermission(batch.BandId, user.Id, BandPermission.EditContent),
                cancellationToken
            )
        )
            return Results.Forbid();
        if (batch.Status == StemUploadStatus.Completed)
            return Results.Conflict(new { code = "stem_upload_already_completed" });
        batch.Status = StemUploadStatus.Aborted;
        session.Store(batch);
        await session.SaveChangesAsync(cancellationToken);
        await StemUploadCleanupService.DeleteFiles(batch, bus, cancellationToken);
        return Results.NoContent();
    }
}

/// <summary>Validates the complete source-file set before any stems become visible.</summary>
public static class StemUploadRules
{
    public static bool AllFilesReady(StemUploadBatch batch, IReadOnlyList<FileReference> files) =>
        batch.Files.Count > 0
        && batch.Files.All(draft =>
            files.Any(file =>
                file.FileId == draft.Stem.FileId
                && file.BandId == batch.BandId
                && file.Status == FileObjectStatus.Ready
                && file.Size == draft.Size
                && string.Equals(
                    file.MimeType,
                    draft.MimeType.Split(';', 2)[0].Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
                && string.Equals(file.Sha256, draft.Sha256, StringComparison.OrdinalIgnoreCase)
            )
        );
}
