namespace FSH.Modules.Messaging.Contracts.v1;

/// <summary>
/// Base class for all messages.
/// </summary>
public abstract record MessageBase : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Base class for request messages.
/// </summary>
public abstract record RequestBase : MessageBase, IRequest
{
}

/// <summary>
/// Base class for response messages.
/// </summary>
public abstract record ResponseBase : MessageBase, IResponse
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static T CreateSuccess<T>() where T : ResponseBase, new()
    {
        return new T { Success = true };
    }

    /// <summary>
    /// Creates a failed response with an error message.
    /// </summary>
    public static T CreateFailure<T>(string errorMessage) where T : ResponseBase, new()
    {
        return new T { Success = false, ErrorMessage = errorMessage };
    }
}
