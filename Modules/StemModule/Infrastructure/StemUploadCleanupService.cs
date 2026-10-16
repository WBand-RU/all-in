using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.StemModule.Domain;
using Wolverine;

namespace WBand.Modules.StemModule.Infrastructure;

internal sealed class StemUploadCleanupService(IServiceScopeFactory scopes,
    ILogger<StemUploadCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            // Wait for Wolverine and the document store to finish starting.
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { await Cleanup(stoppingToken); }
                catch (Exception e) when (e is not OperationCanceledException)
                { logger.LogWarning(e, "Could not clean staged stem uploads; retrying next minute"); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task Cleanup(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var now = DateTimeOffset.UtcNow;
        var ids = await session.Query<StemUploadBatch>().Where(x => !x.CleanupFinished
            && (x.Status == StemUploadStatus.Aborted || (x.Status == StemUploadStatus.Uploading && x.ExpiresAt < now)))
            .Take(100).Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            try
            {
                await using var batchScope = scopes.CreateAsyncScope();
                var batchSession = batchScope.ServiceProvider.GetRequiredService<IDocumentSession>();
                var bus = batchScope.ServiceProvider.GetRequiredService<IMessageBus>();
                var batch = await batchSession.LoadAsync<StemUploadBatch>(id, cancellationToken);
                if (batch is null || batch.Status == StemUploadStatus.Completed || batch.CleanupFinished) continue;
                if (batch.Status == StemUploadStatus.Uploading && batch.ExpiresAt >= now) continue;
                batch.Status = StemUploadStatus.Aborted;
                batchSession.Store(batch);
                await batchSession.SaveChangesAsync(cancellationToken);
                await DeleteFiles(batch, bus, cancellationToken);
                // Repeat until signed URLs have expired, including late in-flight PUTs.
                if (batch.CleanupUntil < now)
                {
                    batch.CleanupFinished = true;
                    batchSession.Store(batch);
                    await batchSession.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            { logger.LogWarning(e, "Could not clean stem upload {BatchId}", id); }
        }
    }

    internal static async Task DeleteFiles(StemUploadBatch batch, IMessageBus bus, CancellationToken cancellationToken)
    {
        foreach (var draft in batch.Files)
        {
            var result = await bus.InvokeAsync<DeleteFileResult>(new DeleteFile(draft.Stem.FileId, batch.BandId, draft.ObjectKey), cancellationToken);
            if (!result.Deleted) throw new InvalidOperationException("Upload file is outside its band.");
        }
    }
}
