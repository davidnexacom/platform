namespace FSH.Modules.Messaging.Contracts.v1.Examples;

/// <summary>
/// Example request message for ping operation.
/// </summary>
public sealed record PingRequest : RequestBase
{
    public string Message { get; init; } = "Ping";
}

/// <summary>
/// Example response message for ping operation.
/// </summary>
public sealed record PingResponse : ResponseBase
{
    public string Message { get; init; } = "Pong";
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
