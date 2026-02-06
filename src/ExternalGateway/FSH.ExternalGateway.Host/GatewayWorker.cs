using Rebus.Bus;

namespace FSH.ExternalGateway;

public sealed class GatewayWorker : BackgroundService
{
    private readonly ILogger<GatewayWorker> _logger;
    private readonly IBus _bus;

    public GatewayWorker(
        ILogger<GatewayWorker> logger,
        IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("External Gateway Worker starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("External Gateway Worker stopping...");
    }
}
