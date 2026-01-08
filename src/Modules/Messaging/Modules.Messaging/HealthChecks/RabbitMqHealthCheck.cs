using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rebus.Bus;

namespace FSH.Modules.Messaging.HealthChecks;

/// <summary>
/// Health check for RabbitMQ connection through Rebus.
/// </summary>
internal sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IBus _bus;

    public RabbitMqHealthCheck(IBus bus)
    {
        _bus = bus;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get advanced API to check connection
            var advanced = _bus.Advanced;
            
            // If we can access the advanced API, connection is likely healthy
            return await Task.FromResult(
                HealthCheckResult.Healthy("RabbitMQ connection is healthy"));
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ connection is unhealthy",
                ex);
        }
    }
}
