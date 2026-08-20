using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.MixerModule.Contracts;
using WBand.Modules.MixerModule.Domain;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.StemModule.Contracts;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.MixerModule.Endpoints;

public sealed record MixArtifactResponse(Guid Id, MixArtifactKind Kind, Guid? TargetStemId,
    string Group, string Name, string Format, Guid FileId, string? DownloadUrl);
public sealed record MixBatchResponse(Guid Id, Guid SongId, MixBatchStatus Status, int SourceCount,
    string? Error, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, DateTimeOffset? HeartbeatAt, int AttemptCount,
    string? CurrentStage, string? CurrentPlan, int CompletedOutputCount,
    int TotalOutputCount, IReadOnlyList<MixArtifactResponse> Artifacts);

internal static class MixerEndpointSupport
{
    public static async Task<SongReference?> GetSong(IMessageBus bus, Guid songId) =>
        (await bus.InvokeAsync<SongReference[]>(new GetSongReferences([songId]))).SingleOrDefault();
}

public static class QueueSongMixesEndpoint
{
    [Authorize]
    [WolverinePost("/mixer-api/songs/{songId}/mixes")]
    public static async Task<IResult> Post(Guid songId, ICurrentUser user,
        IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await MixerEndpointSupport.GetSong(bus, songId);
        if (song is null) return Results.NotFound();
        if (!await bus.InvokeAsync<bool>(new CheckBandPermission(song.BandId, user.GetUserId,
            BandPermission.EditContent))) return Results.Forbid();
        var sources = await bus.InvokeAsync<ReadyStemForMix[]>(new GetReadyStemsForMix(songId));
        if (sources.Length == 0) return Results.BadRequest(new { code = "mix_no_ready_stems" });
        var running = await session.Query<MixBatch>().AnyAsync(batch => batch.SongId == songId &&
            (batch.Status == MixBatchStatus.Queued || batch.Status == MixBatchStatus.Processing),
            cancellationToken);
        if (running) return Results.Conflict(new { code = "mix_already_queued" });
        var batch = new MixBatch { Id = Guid.CreateVersion7(), SongId = songId,
            BandId = song.BandId, SourceCount = sources.Length, CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = user.GetUserId, LastEnqueuedAt = DateTimeOffset.UtcNow };
        session.Store(batch); await session.SaveChangesAsync(cancellationToken);
        await bus.SendAsync(new GenerateSongMixes(batch.Id, songId, song.BandId, user.GetUserId));
        return Results.Accepted($"/mixer-api/songs/{songId}/mixes", new MixBatchResponse(batch.Id,
            songId, batch.Status, batch.SourceCount, null, batch.CreatedAt, null, null,
            null, 0, "Queued", null, 0, 0, []));
    }
}

public static class GetSongMixesEndpoint
{
    [Authorize]
    [WolverineGet("/mixer-api/songs/{songId}/mixes")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, IQuerySession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await MixerEndpointSupport.GetSong(bus, songId);
        if (song is null) return Results.NotFound();
        if (!await bus.InvokeAsync<bool>(new CheckBandPermission(song.BandId, user.GetUserId,
            BandPermission.View))) return Results.Forbid();
        var batch = await session.Query<MixBatch>().Where(item => item.SongId == songId)
            .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (batch is null) return Results.Ok(null);
        var artifacts = await session.Query<MixArtifact>().Where(item => item.BatchId == batch.Id)
            .OrderBy(item => item.Group).ThenBy(item => item.Name).ToListAsync(cancellationToken);
        var downloads = (await bus.InvokeAsync<FileDownloadAccess[]>(new GetReadyFileDownloads(
            artifacts.Select(artifact => artifact.FileId).ToArray()))).ToDictionary(file => file.FileId);
        return Results.Ok(new MixBatchResponse(batch.Id, batch.SongId, batch.Status,
            batch.SourceCount, batch.Error, batch.CreatedAt, batch.StartedAt, batch.CompletedAt,
            batch.HeartbeatAt, batch.AttemptCount, batch.CurrentStage, batch.CurrentPlan,
            batch.CompletedOutputCount, batch.TotalOutputCount,
            artifacts.Select(artifact => new MixArtifactResponse(artifact.Id, artifact.Kind,
                artifact.TargetStemId, artifact.Group, artifact.Name, artifact.Format, artifact.FileId,
                downloads.GetValueOrDefault(artifact.FileId)?.DownloadUrl)).ToArray()));
    }
}
