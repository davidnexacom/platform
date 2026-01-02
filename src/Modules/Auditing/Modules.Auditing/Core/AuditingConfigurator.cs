// Add this hosted service class once in your auditing module
using FSH.Modules.Auditing.Contracts;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Auditing;

public sealed class AuditingConfigurator : IHostedService
{
    private readonly IAuditPublisher _publisher;
    private readonly IAuditSerializer _serializer;

    public AuditingConfigurator(
        IAuditPublisher publisher,
        IAuditSerializer serializer)
    {
        _publisher = publisher;
        _serializer = serializer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Configure without enrichers - they will be resolved per-request by the publisher
        Audit.Configure(_publisher, _serializer, enrichers: null);
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

