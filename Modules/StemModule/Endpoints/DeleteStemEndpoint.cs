using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.StemModule.Contracts;
using WBand.Modules.StemModule.Domain;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

namespace WBand.Modules.StemModule.Endpoints;

/// <summary>Permanently removes a stem and its source file from a song.</summary>
public static class DeleteStemEndpoint
{
    [Authorize]
    [WolverineDelete("/stem-api/songs/{songId}/stems/{stemId}")]
    public static async Task<IResult> Delete(
        Guid songId,
        Guid stemId,
        [FromQuery] long expectedRevision,
        ICurrentUser user,
        IDocumentSession session,
        IMessageBus bus,
        IMartenOutbox outbox,
        CancellationToken cancellationToken
    )
    {
        var song = await StemEndpointSupport.GetSong(bus, songId);
        if (song is null)
            return Results.NotFound();
        if (!await StemEndpointSupport.Can(bus, song, user.Id, BandPermission.EditContent))
            return Results.Forbid();

        var stem = await session.LoadAsync<Stem>(stemId, cancellationToken);
        if (stem is null)
            return Results.NoContent();
        if (stem.SongId != songId || stem.BandId != song.BandId)
            return Results.NotFound();
        if (expectedRevision <= 0)
            return Results.BadRequest(new { code = "stem_revision_required" });
        if (stem.Revision != expectedRevision)
            return Results.Conflict(
                new { code = "stem_version_conflict", currentRevision = stem.Revision }
            );

        var deletion = await bus.InvokeAsync<DeleteFileResult>(
            new DeleteFile(stem.FileId, song.BandId),
            cancellationToken
        );
        if (!deletion.Deleted)
            return Results.Conflict(new { code = "stem_file_outside_band" });

        var collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        session.Delete(stem);
        outbox.Enroll(session);
        await outbox.PublishAsync(new StemsChanged(songId, song.BandId, collection.ContentVersion));
        await session.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
