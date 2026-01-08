namespace FSH.Modules.Messaging.Contracts.Services;

/// <summary>
/// Service for sending messages and handling request/response patterns using RabbitMQ.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Sends a one-way message (fire and forget).
    /// </summary>
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Publishes a message to all subscribers.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Sends a request and waits for a response.
    /// </summary>
    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    Task SubscribeAsync<TMessage>(Func<TMessage, Task> handler)
        where TMessage : class;

    /// <summary>
    /// Handles requests of a specific type.
    /// </summary>
    Task HandleRequestAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        where TRequest : class
        where TResponse : class;
}
