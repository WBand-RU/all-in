using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WBand.Modules.MixerModule.Contracts;
using WBand.Modules.MixerModule.Domain;
using Wolverine;

namespace WBand.Modules.MixerModule.Application;

/// <summary>Re-enqueues mixer jobs abandoned by a stopped worker or lost delivery.</summary>
public sealed class MixBatchRecoveryService(IServiceScopeFactory scopeFactory,
    ILogger<MixBatchRecoveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await RecoverAsync(stoppingToken, recoverAllProcessing: true);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RecoverAsync(stoppingToken, recoverAllProcessing: false);
    }

    private async Task RecoverAsync(CancellationToken cancellationToken,
        bool recoverAllProcessing)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await using var session = store.LightweightSession();
            var active = await session.Query<MixBatch>().Where(batch =>
                batch.Status == MixBatchStatus.Queued ||
                batch.Status == MixBatchStatus.Processing).ToListAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var stale = active.Where(batch =>
                (recoverAllProcessing && batch.Status == MixBatchStatus.Processing) ||
                MixBatchRecoveryPolicy.IsStale(batch, now)).ToArray();
            if (stale.Length == 0) return;

            foreach (var batch in stale)
            {
                batch.Status = MixBatchStatus.Queued;
                batch.LastEnqueuedAt = now;
                batch.Error = null;
                session.Store(batch);
            }
            await session.SaveChangesAsync(cancellationToken);

            var handler = ActivatorUtilities.CreateInstance<GenerateSongMixesHandler>(
                scope.ServiceProvider);
            foreach (var batch in stale)
            {
                logger.LogWarning("Starting recovery of abandoned mix batch {BatchId} for song {SongId}",
                    batch.Id, batch.SongId);
                await using var processingSession = store.LightweightSession();
                await handler.Handle(new GenerateSongMixes(batch.Id, batch.SongId, batch.BandId,
                    batch.CreatedBy), processingSession, bus, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not recover abandoned mixer jobs");
        }
    }
}

public static class MixBatchRecoveryPolicy
{
    public static bool IsStale(MixBatch batch, DateTimeOffset now) => batch.Status switch
    {
        MixBatchStatus.Processing => now - (batch.HeartbeatAt ?? batch.StartedAt ??
            batch.CreatedAt) >= TimeSpan.FromMinutes(30),
        MixBatchStatus.Queued => now - (batch.LastEnqueuedAt ?? batch.CreatedAt) >=
            TimeSpan.FromSeconds(30),
        _ => false,
    };
}
