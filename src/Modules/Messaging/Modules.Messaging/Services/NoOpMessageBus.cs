using FSH.Modules.Messaging.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Messaging.Services;

/// <summary>
/// No-operation implementation of IMessageBus used when messaging is disabled.
/// All operations are no-ops and log warnings.
/// </summary>
internal sealed class NoOpMessageBus : IMessageBus
{
    private readonly ILogger<NoOpMessageBus> _logger;

    public NoOpMessageBus(ILogger<NoOpMessageBus> logger)
    {
        _logger = logger;
    }

    public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _logger.LogWarning(
            "Messaging is disabled. Message of type {MessageType} was not sent.",
            typeof(TMessage).Name);
        return Task.CompletedTask;
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _logger.LogWarning(
            "Messaging is disabled. Message of type {MessageType} was not published.",
            typeof(TMessage).Name);
        return Task.CompletedTask;
    }

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        _logger.LogWarning(
            "Messaging is disabled. Request of type {RequestType} was not sent.",
            typeof(TRequest).Name);
        
        throw new InvalidOperationException(
            $"Messaging is disabled. Cannot send request of type {typeof(TRequest).Name}. " +
            "Enable RabbitMQ in configuration to use request/response patterns.");
    }

    public Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler)
        where TMessage : class
    {
        _logger.LogWarning(
            "Messaging is disabled. Subscription to {MessageType} was not created.",
            typeof(TMessage).Name);
        return Task.CompletedTask;
    }

    public Task HandleRequestAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        where TRequest : class
        where TResponse : class
    {
        _logger.LogWarning(
            "Messaging is disabled. Request handler for {RequestType} was not registered.",
            typeof(TRequest).Name);
        return Task.CompletedTask;
    }
}
