using JasperFx;
using Marten;
using Quartz;
using WBand.Modules.MixerModule.Application;
using WBand.Modules.MixerModule.Contracts;
using WBand.Modules.MixerModule.Domain;
using Wolverine;

namespace WBand.MixerWorker;

/// <summary>Claims and processes durable mixer batches without running FFmpeg in WebAPI.</summary>
[DisallowConcurrentExecution]
public sealed class GenerateQueuedMixesJob(IDocumentStore store, IMessageBus bus,
    IHttpClientFactory httpClientFactory, ILogger<GenerateQueuedMixesJob> logger,
    ILogger<GenerateSongMixesHandler> handlerLogger) : IJob
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        await using var query = store.QuerySession();
        var candidates = await query.Query<MixBatch>().Where(batch =>
                batch.Status == MixBatchStatus.Queued || batch.Status == MixBatchStatus.Processing)
            .OrderBy(batch => batch.CreatedAt).ToListAsync(cancellationToken);
        var candidate = candidates.FirstOrDefault(batch => MixBatchLeasePolicy.CanClaim(batch, now));
        if (candidate is null) return;

        await using var session = store.LightweightSession();
        var batch = await session.LoadAsync<MixBatch>(candidate.Id, cancellationToken);
        if (batch is null || !MixBatchLeasePolicy.CanClaim(batch, now)) return;

        batch.LeaseId = Guid.CreateVersion7();
        batch.LeaseExpiresAt = now.Add(LeaseDuration);
        batch.LastEnqueuedAt = now;
        batch.Status = MixBatchStatus.Queued;
        batch.CurrentStage = "Queued in mixer worker";
        session.Store(batch);
        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            logger.LogDebug("Mix batch {BatchId} was claimed by another worker", batch.Id);
            return;
        }

        logger.LogInformation("Processing mix batch {BatchId} for song {SongId}", batch.Id,
            batch.SongId);
        var handler = new GenerateSongMixesHandler(httpClientFactory, handlerLogger);
        await handler.Handle(new GenerateSongMixes(batch.Id, batch.SongId, batch.BandId,
            batch.CreatedBy), session, bus, cancellationToken);
    }
}
