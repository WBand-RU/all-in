using FluentValidation;
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

/// <summary>Identifies a stem and the revision confirmed for permanent deletion.</summary>
public sealed record DeleteStemItem(Guid StemId, long ExpectedRevision);

/// <summary>Deletes the confirmed snapshot of all stems or a group's stems.</summary>
public sealed record DeleteStemsRequest(
    long ExpectedContentVersion,
    IReadOnlyList<DeleteStemItem> Stems
);

public sealed class DeleteStemsRequestValidator : AbstractValidator<DeleteStemsRequest>
{
    public DeleteStemsRequestValidator()
    {
        RuleFor(x => x.ExpectedContentVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stems)
            .NotEmpty()
            .Must(x => x.Count <= 1000)
            .Must(x => x.Select(item => item.StemId).Distinct().Count() == x.Count);
        RuleForEach(x => x.Stems)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.StemId).NotEmpty();
                item.RuleFor(x => x.ExpectedRevision).GreaterThan(0);
            });
    }
}

/// <summary>Permanently deletes a confirmed set of stems and their source files.</summary>
public static class DeleteStemsEndpoint
{
    [Authorize]
    [WolverineDelete("/stem-api/songs/{songId}/stems")]
    public static async Task<IResult> Delete(
        Guid songId,
        [FromBody] DeleteStemsRequest request,
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

        var collection = await session.LoadAsync<StemCollection>(songId, cancellationToken);
        if ((collection?.ContentVersion ?? 0) != request.ExpectedContentVersion)
            return Results.Conflict(new { code = "stem_collection_version_conflict" });

        var stems = await session.LoadManyAsync<Stem>(
            request.Stems.Select(x => x.StemId).ToArray()
        );
        if (stems.Any(x => x.SongId != songId || x.BandId != song.BandId))
            return Results.NotFound();
        var revisions = request.Stems.ToDictionary(x => x.StemId, x => x.ExpectedRevision);
        if (stems.Any(x => x.Revision != revisions[x.Id]))
            return Results.Conflict(new { code = "stem_version_conflict" });
        if (stems.Count == 0)
            return Results.NoContent();

        // Finish storage deletion before committing metadata. Missing files are safe on retries.
        foreach (var stem in stems)
        {
            var result = await bus.InvokeAsync<DeleteFileResult>(
                new DeleteFile(stem.FileId, song.BandId),
                cancellationToken
            );
            if (!result.Deleted)
                return Results.Conflict(new { code = "stem_file_outside_band" });
        }
        collection = await StemEndpointSupport.Touch(session, song, cancellationToken);
        foreach (var stem in stems)
            session.Delete(stem);
        outbox.Enroll(session);
        await outbox.PublishAsync(new StemsChanged(songId, song.BandId, collection.ContentVersion));
        await session.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
