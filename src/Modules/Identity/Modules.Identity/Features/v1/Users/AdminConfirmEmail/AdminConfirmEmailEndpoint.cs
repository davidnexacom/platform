using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.AdminConfirmEmail;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.AdminConfirmEmail;

public static class AdminConfirmEmailEndpoint
{
    internal static RouteHandlerBuilder MapAdminConfirmEmailEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/users/{userId}/admin-confirm-email", async (string userId, string tenant, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new AdminConfirmEmailCommand(userId), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("AdminConfirmEmail")
        .WithSummary("Admin confirm user email")
        .WithDescription("Confirm a user's email address without requiring a confirmation code. Requires Create Users and Update Users permissions.")
        .RequirePermission(
            FshPermission.NameFor(ActionConstants.Create, ResourceConstants.Users),
            FshPermission.NameFor(ActionConstants.Update, ResourceConstants.Users));
    }
}
