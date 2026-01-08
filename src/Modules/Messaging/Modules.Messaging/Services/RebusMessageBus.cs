using FSH.Modules.Messaging.Contracts.Services;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace FSH.Modules.Messaging.Services;

/// <summary>
/// Implementation of IMessageBus using Rebus.
/// Note: Request/Response pattern in Rebus requires manual correlation handling.
/// Use Publish/Subscribe for most scenarios.
/// </summary>
internal sealed class RebusMessageBus : IMessageBus
{
    private readonly IBus _bus;
    private readonly ILogger<RebusMessageBus> _logger;

    public RebusMessageBus(IBus bus, ILogger<RebusMessageBus> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            _logger.LogDebug("Sending message of type {MessageType}", typeof(TMessage).Name);
            await _bus.Send(message);
            _logger.LogInformation("Successfully sent message of type {MessageType}", typeof(TMessage).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message of type {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            _logger.LogDebug("Publishing message of type {MessageType}", typeof(TMessage).Name);
            await _bus.Publish(message);
            _logger.LogInformation("Successfully published message of type {MessageType}", typeof(TMessage).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message of type {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogWarning(
            "Direct Request/Response is not implemented in this version. " +
            "Use correlation-based pattern: " +
            "1) Send request with CorrelationId, " +
            "2) Subscribe to response type, " +
            "3) Handler replies with bus.Reply(response). " +
            "See documentation for examples.");

        throw new NotImplementedException(
            "Direct Request/Response requires additional infrastructure. " +
            "Use Publish/Subscribe pattern with correlation IDs instead. " +
            "Example: await _bus.Publish(request); then subscribe to response type with matching CorrelationId.");
    }

    public async Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        try
        {
            _logger.LogDebug("Subscribing to messages of type {MessageType}", typeof(TMessage).Name);
            await _bus.Subscribe<TMessage>();
            _logger.LogInformation("Successfully subscribed to messages of type {MessageType}", typeof(TMessage).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to messages of type {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    public Task HandleRequestAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "Handler registration for request type {RequestType} with response type {ResponseType}. " +
            "In Rebus, handlers implement IHandleMessages<TRequest> and use IBus.Reply(response) to send responses.",
            typeof(TRequest).Name,
            typeof(TResponse).Name);

        // In Rebus, handlers are registered through DI and implement IHandleMessages<T>
        // They use IBus.Reply() to send responses
        return Task.CompletedTask;
    }
}
