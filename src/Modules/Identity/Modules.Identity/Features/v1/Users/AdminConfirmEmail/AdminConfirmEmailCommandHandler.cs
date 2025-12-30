using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.AdminConfirmEmail;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Users.AdminConfirmEmail;

public sealed class AdminConfirmEmailCommandHandler : ICommandHandler<AdminConfirmEmailCommand, string>
{
    private readonly IdentityDbContext _db;

    public AdminConfirmEmailCommandHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public async ValueTask<string> Handle(AdminConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await _db.Users
            .Where(u => u.Id == command.UserId && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new CustomException("User not found or email already confirmed.");
        }

        user.EmailConfirmed = true;
        await _db.SaveChangesAsync(cancellationToken);

        return $"Email confirmed for user {user.Email}.";
    }
}
