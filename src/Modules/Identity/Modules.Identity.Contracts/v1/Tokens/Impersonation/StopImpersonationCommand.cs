using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;

/// <summary>
/// Command to stop impersonation and return to the original user identity.
/// Returns a new token for the original user.
/// </summary>
public sealed record StopImpersonationCommand : ICommand<TokenResponse>
{
}
