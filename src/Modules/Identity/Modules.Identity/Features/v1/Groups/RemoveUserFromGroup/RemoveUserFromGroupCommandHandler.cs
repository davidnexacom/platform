using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using FSH.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public sealed class RemoveUserFromGroupCommandHandler : ICommandHandler<RemoveUserFromGroupCommand, Unit>
{
    private readonly IdentityDbContext _dbContext;

    public RemoveUserFromGroupCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(RemoveUserFromGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await _dbContext.UserGroups
            .FirstOrDefaultAsync(ug => ug.GroupId == command.GroupId && ug.UserId == command.UserId, cancellationToken);

        if (membership is null)
        {
            throw new NotFoundException($"User '{command.UserId}' is not a member of group '{command.GroupId}'.");
        }

        _dbContext.UserGroups.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
