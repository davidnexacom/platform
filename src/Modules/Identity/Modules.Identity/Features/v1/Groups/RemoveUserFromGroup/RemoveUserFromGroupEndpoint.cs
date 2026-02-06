using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Groups.RemoveUserFromGroup;

public static class RemoveUserFromGroupEndpoint
{
    public static RouteHandlerBuilder MapRemoveUserFromGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/groups/{groupId:guid}/members/{userId}", (Guid groupId, string userId, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new RemoveUserFromGroupCommand(groupId, userId), cancellationToken))
        .WithName("RemoveUserFromGroup")
        .WithSummary("Remove a user from a group")
        .RequirePermission(IdentityPermissionConstants.Groups.ManageMembers)
        .WithDescription("Remove a specific user from a group.");
    }
}
