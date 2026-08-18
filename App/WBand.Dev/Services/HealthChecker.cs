using Microsoft.Extensions.Logging;

namespace WBand.Dev.Services;

public sealed class HealthChecker(
    IHttpClientFactory httpClientFactory,
    ILogger<HealthChecker> logger
)
{
    private readonly TimeSpan healthCheckDelay = TimeSpan.FromMilliseconds(500);

    public async Task<bool> CheckHealth(string? healthCheckUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(healthCheckUrl))
        {
            return true;
        }

        var httpClient = httpClientFactory.CreateClient("health");

        try
        {
            var result = await httpClient.GetAsync(healthCheckUrl, cancellationToken);

            _ = result.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check: {Uri}", this.healthCheckDelay);

            return false;
        }
        finally
        {
            await Task.Delay(this.healthCheckDelay, cancellationToken);
        }
    }
}
