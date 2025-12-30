using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Tokens.Impersonation;

public static class ImpersonateUserEndpoint
{
    public static RouteHandlerBuilder MapImpersonateUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("impersonate", async (ImpersonateUserCommand request, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(request, ct);
                return TypedResults.Ok(result);
            })
            .WithName(nameof(ImpersonateUserEndpoint))
            .WithSummary("Impersonate another user")
            .WithDescription("Allows an admin to impersonate another user's identity for support and debugging purposes")
            .RequirePermission(ActionConstants.Impersonate, ResourceConstants.Users)
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}
