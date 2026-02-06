using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Groups.GetGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Groups.GetGroups;

public static class GetGroupsEndpoint
{
    public static RouteHandlerBuilder MapGetGroupsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/groups", (IMediator mediator, string? search, CancellationToken cancellationToken) =>
            mediator.Send(new GetGroupsQuery(search), cancellationToken))
        .WithName("ListGroups")
        .WithSummary("List all groups")
        .RequirePermission(IdentityPermissionConstants.Groups.View)
        .WithDescription("Retrieve all groups for the current tenant with optional search filter.");
    }
}
