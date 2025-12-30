using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;

/// <summary>
/// Command to impersonate another user.
/// Only users with Impersonate permission can execute this command.
/// </summary>
public sealed record ImpersonateUserCommand : ICommand<TokenResponse>
{
    /// <summary>
    /// The ID of the user to impersonate.
    /// </summary>
    public required string UserId { get; init; }
}
