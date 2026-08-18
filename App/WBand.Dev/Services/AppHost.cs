using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WBand.Dev.Services;

internal class AppHost(ProcessManager processManager, IServiceProvider serviceProvider)
    : BackgroundService
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await this.Check(stoppingToken);

                await Task.Delay(CheckDelay, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await base.StopAsync(cancellationToken);
        }
        finally
        {
            processManager.Dispose();
        }
    }

    private async Task Check(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppHost>>();
        var serviceInfoManager = scope.ServiceProvider.GetRequiredService<ServiceInfoManager>();
        var processManager = scope.ServiceProvider.GetRequiredService<ProcessManager>();
        var healthChecker = scope.ServiceProvider.GetRequiredService<HealthChecker>();

        foreach (var serviceInfo in serviceInfoManager.Services.ToList())
        {
            try
            {
                await CheckService(
                    serviceInfo,
                    serviceInfoManager,
                    processManager,
                    healthChecker,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "CheckService throws an exception");
            }
        }
    }

    private static async Task CheckService(
        ServiceInfo serviceInfo,
        ServiceInfoManager serviceInfoManager,
        ProcessManager processManager,
        HealthChecker healthChecker,
        CancellationToken cancellationToken
    )
    {
        // if service started then check not exit
        // else then start process

        if (!processManager.HasProcess(serviceInfo.ServiceName))
        {
            processManager.Start(serviceInfo);
        }

        var isHealth = (await healthChecker.CheckHealth(serviceInfo.HealthUri, cancellationToken));
        serviceInfoManager.Update(serviceInfo.ServiceName, serviceInfo with { Health = isHealth });
    }
}
