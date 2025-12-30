using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Identity.Contracts.Events;

/// <summary>
/// Integration event raised when an admin starts impersonating another user.
/// </summary>
public sealed record UserImpersonationStartedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string? CorrelationId,
    string Source,
    string ImpersonatorId,
    string ImpersonatorEmail,
    string TargetUserId,
    string TargetUserEmail,
    string IpAddress,
    string UserAgent) : IIntegrationEvent;
