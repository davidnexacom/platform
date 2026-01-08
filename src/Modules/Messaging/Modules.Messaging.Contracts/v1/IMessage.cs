namespace FSH.Modules.Messaging.Contracts.v1;

/// <summary>
/// Base interface for all messages in the messaging module.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    Guid MessageId { get; }

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Correlation ID for tracking related messages
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Base interface for request messages that expect a response.
/// </summary>
public interface IRequest : IMessage
{
}

/// <summary>
/// Base interface for response messages.
/// </summary>
public interface IResponse : IMessage
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    string? ErrorMessage { get; }
}
