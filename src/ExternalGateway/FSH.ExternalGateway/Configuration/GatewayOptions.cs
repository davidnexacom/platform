namespace FSH.ExternalGateway.Configuration;

public sealed class GatewayOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public bool EnableQueryLogging { get; set; } = true;
}

public sealed class RabbitMqOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public int MaxConcurrentMessages { get; set; } = 10;
    public int RetryAttempts { get; set; } = 3;
}
