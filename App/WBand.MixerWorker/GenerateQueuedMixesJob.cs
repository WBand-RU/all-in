using JasperFx;
using Marten;
using Quartz;
using Shared.Configuration;
using WBand.Modules.MixerModule.Application;
using WBand.Modules.MixerModule.Contracts;
using WBand.Modules.MixerModule.Domain;
using Wolverine;

namespace WBand.MixerWorker;

/// <summary>Claims and processes durable mixer batches without running FFmpeg in WebAPI.</summary>
[DisallowConcurrentExecution]
public sealed class GenerateQueuedMixesJob(IDocumentStore store, IMessageBus bus,
    IHttpClientFactory httpClientFactory, ILogger<GenerateQueuedMixesJob> logger,
    ILogger<GenerateSongMixesHandler> handlerLogger, IConfiguration configuration,
    IHostApplicationLifetime lifetime) : IJob
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    public async Task Execute(IJobExecutionContext context)
    {
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(
            context.CancellationToken, lifetime.ApplicationStopping);
        var cancellationToken = stopping.Token;
        var now = DateTimeOffset.UtcNow;
        await using var query = store.QuerySession();
        var candidates = await query.Query<MixBatch>().Where(batch =>
                batch.Status == MixBatchStatus.Queued || batch.Status == MixBatchStatus.Processing)
            .OrderBy(batch => batch.CreatedAt).ToListAsync(cancellationToken);
        foreach (var candidate in candidates.Where(batch => MixBatchLeasePolicy.CanClaim(batch, now)))
        {
            await using var ownership = await MixBatchLock.TryAcquireAsync(
                MartenOptions.FromConfiguration(configuration).ConnectionString, candidate.Id, cancellationToken);
            if (ownership is null) continue;
            using var processing = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, ownership.LockLost);
            cancellationToken = processing.Token;

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
            var handler = new GenerateSongMixesHandler(httpClientFactory, handlerLogger, store);
            await handler.Handle(new GenerateSongMixes(batch.Id, batch.SongId, batch.BandId,
                batch.CreatedBy), session, bus, cancellationToken);
            return;
        }
    }
}
