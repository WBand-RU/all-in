using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.HealthChecks;

public sealed class TcpEndpointHealthCheck(string host, int port) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception);
        }
    }
}
