using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WBand.Modules.FileModule.Domain;
using WBand.Modules.FileModule.Contracts;
using WBand.Modules.FileModule.Services;

namespace WBand.Modules.FileModule.Infrastructure;

internal sealed class FileUploadCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<FileStorageOptions> options,
    ILogger<FileUploadCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes));
        do
        {
            await Cleanup(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task Cleanup(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileService>();
        var expired = await session.Query<FileObject>()
            .Where(file => file.Status == FileObjectStatus.Pending &&
                file.ExpiresAt < DateTimeOffset.UtcNow)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var file in expired)
        {
            try
            {
                await storage.DeleteAsync(file.ObjectKey, cancellationToken);
                file.Status = FileObjectStatus.Rejected;
                file.RejectionReason = "file_upload_expired";
                session.Store(file);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not clean incomplete file {FileId}", file.Id);
            }
        }
        await session.SaveChangesAsync(cancellationToken);
    }
}
