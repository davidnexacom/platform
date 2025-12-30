using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.AdminConfirmEmail;

/// <summary>
/// Administrative command to confirm a user's email without requiring a confirmation code.
/// Requires Create Users and Update Users permissions.
/// </summary>
public sealed record AdminConfirmEmailCommand(string UserId) : ICommand<string>;
