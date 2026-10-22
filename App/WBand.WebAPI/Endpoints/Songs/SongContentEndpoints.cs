using Wolverine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WBand.WebAPI.Application.Songs;
using WBand.WebAPI.Data;
using WBand.WebAPI.Services;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Songs;

public static class GetSongContentEndpoint
{
    [Authorize, Tags("SongContent"), ProducesResponseType<SongContentResponse>(200)]
    [WolverineGet("/song-api/songs/{songId}/content", OperationId = "GetSongContent")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, false, cancellationToken)) return Results.Forbid();
        var sections = await db.QueryAsync<SongSection>(
            "select * from public.song_sections where song_id = @songId order by \"order\", created_at, id", new { songId }, cancellationToken);
        var partitions = await db.QueryAsync<Partition>(
            "select * from public.song_partitions where song_id = @songId order by \"order\", created_at, id", new { songId }, cancellationToken);
        var cells = await db.QueryAsync<SongContentCell>(
            "select * from public.song_content_cells where song_id = @songId", new { songId }, cancellationToken);
        return Results.Ok(new SongContentResponse(sections.Select(SongSectionResponse.From).ToArray(),
            partitions.Select(PartitionResponse.From).ToArray(), cells.Select(SongContentCellResponse.From).ToArray()));
    }
}

public static class UpdateSongContentCellEndpoint
{
    [Authorize, Tags("SongContent"), ProducesResponseType<SongContentCellResponse>(200)]
    [WolverinePut("/song-api/songs/{songId}/sections/{sectionId}/partitions/{partitionId}", OperationId = "UpdateSongContentCell")]
    public static async Task<IResult> Put(Guid songId, Guid sectionId, Guid partitionId, UpdateSongContentCellRequest request,
        ICurrentUser user, [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var section = await db.FindAsync<SongSection>(sectionId, cancellationToken);
        var partition = await db.FindAsync<Partition>(partitionId, cancellationToken);
        if (section?.SongId != songId || partition?.SongId != songId) return Results.NotFound();
        // The song lock serializes first writes as well as updates, preventing duplicate cells.
        var cell = await db.SingleOrDefaultAsync<SongContentCell>(
            "select * from public.song_content_cells where section_id = @sectionId and partition_id = @partitionId",
            new { sectionId, partitionId }, cancellationToken);
        var revision = cell?.Revision ?? 0;
        if (revision != request.ExpectedRevision)
            return Results.Conflict(new { code = "song_cell_version_conflict", currentRevision = revision });
        cell = await SongContentWriter.Save(db, cell, songId, sectionId, partitionId, request.Content, user.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(SongContentCellResponse.From(cell));
    }
}

public static class UpdatePartitionContentEndpoint
{
    [Authorize, Tags("SongContent"), ProducesResponseType<SongContentCellResponse[]>(200)]
    [WolverinePut("/song-api/songs/{songId}/partitions/{partitionId}/content", OperationId = "UpdatePartitionContent")]
    public static async Task<IResult> Put(Guid songId, Guid partitionId, UpdatePartitionContentRequest request,
        ICurrentUser user, [FromServices] DataContext db, [FromServices] IMessageBus bus, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var song = await SongAccess.Load(db, songId, cancellationToken);
        if (song is null) return Results.NotFound();
        if (!await SongAccess.Check(db, bus, song.BandId, user.Id, true, cancellationToken)) return Results.Forbid();
        var partition = await db.FindAsync<Partition>(partitionId, cancellationToken);
        if (partition?.SongId != songId) return Results.NotFound();
        var sections = await db.QueryAsync<SongSection>("select * from public.song_sections where song_id = @songId", new { songId }, cancellationToken);
        var sectionIds = sections.Select(x => x.Id).ToHashSet();
        if (request.Cells.Any(x => !sectionIds.Contains(x.SectionId))) return Results.NotFound();
        var existing = (await db.QueryAsync<SongContentCell>(
            "select * from public.song_content_cells where song_id = @songId and partition_id = @partitionId",
            new { songId, partitionId }, cancellationToken)).ToDictionary(x => x.SectionId);
        var conflicts = request.Cells.Where(x => x.ExpectedRevision != (existing.GetValueOrDefault(x.SectionId)?.Revision ?? 0))
            .Select(x => x.SectionId).ToArray();
        if (conflicts.Length > 0)
            return Results.Conflict(new { code = "song_partition_content_conflict", sectionIds = conflicts });
        var saved = new List<SongContentCell>();
        foreach (var draft in request.Cells)
            saved.Add(await SongContentWriter.Save(db, existing.GetValueOrDefault(draft.SectionId), songId, draft.SectionId,
                partitionId, draft.Content, user.Id, cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(saved.Select(SongContentCellResponse.From).ToArray());
    }
}
