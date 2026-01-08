using FSH.Modules.Messaging.Contracts.v1.Examples;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace FSH.Modules.Messaging.Features.v1.Examples;

/// <summary>
/// Example handler for PingRequest messages using Rebus.
/// </summary>
internal sealed class PingRequestHandler : IHandleMessages<PingRequest>
{
    private readonly ILogger<PingRequestHandler> _logger;

    public PingRequestHandler(ILogger<PingRequestHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(PingRequest message)
    {
        _logger.LogInformation(
            "Received ping request with message: {Message}",
            message.Message);

        // Process the ping request
        await Task.Delay(100); // Simulate some processing

        _logger.LogInformation("Ping request processed successfully");
    }
}
