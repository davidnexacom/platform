using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;

/// <summary>
/// Command to stop impersonation and return to the original user identity.
/// </summary>
public sealed record StopImpersonationCommand : ICommand<bool>
{
}
