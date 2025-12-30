using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Tokens.Impersonation;

public static class StopImpersonationEndpoint
{
    public static RouteHandlerBuilder MapStopImpersonationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("impersonate/stop", async (IMediator mediator, CancellationToken ct) =>
            {
                var command = new StopImpersonationCommand();
                var token = await mediator.Send(command, ct);
                return TypedResults.Ok(token);
            })
            .WithName(nameof(StopImpersonationEndpoint))
            .WithSummary("Stop impersonating a user")
            .WithDescription("Stops the current impersonation session and returns a new token for the original user")
            .RequireAuthorization()
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }
}
