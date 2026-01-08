namespace FSH.Modules.Messaging.Configuration;

/// <summary>
/// Configuration options for RabbitMQ messaging.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMqOptions";

    /// <summary>
    /// RabbitMQ connection string (e.g., amqp://user:pass@localhost:5672)
    /// </summary>
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";

    /// <summary>
    /// Default queue name for the module
    /// </summary>
    public string QueueName { get; set; } = MessagingModuleConstants.DefaultQueueName;

    /// <summary>
    /// Maximum number of concurrent messages to process
    /// </summary>
    public int MaxConcurrentMessages { get; set; } = MessagingModuleConstants.DefaultMaxConcurrentMessages;

    /// <summary>
    /// Number of retry attempts for failed messages
    /// </summary>
    public int RetryAttempts { get; set; } = MessagingModuleConstants.DefaultRetryAttempts;

    /// <summary>
    /// Enable or disable the messaging module
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Timeout for request/response operations in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}
