namespace FSH.Modules.Messaging;

/// <summary>
/// Constants for the Messaging module.
/// </summary>
public static class MessagingModuleConstants
{
    public const string ModuleName = "Messaging";
    public const string DefaultQueueName = "fsh.messaging.queue";
    public const int DefaultMaxConcurrentMessages = 10;
    public const int DefaultRetryAttempts = 3;
}
