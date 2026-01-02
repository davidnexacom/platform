using FSH.Framework.Core.Context;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Sessions.RevokeAllSessions;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Features.v1.Sessions.RevokeAllSessions;

public sealed class RevokeAllSessionsCommandHandler : ICommandHandler<RevokeAllSessionsCommand, int>
{
    private readonly ISessionService _sessionService;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RevokeAllSessionsCommandHandler> _logger;

    public RevokeAllSessionsCommandHandler(
        ISessionService sessionService, 
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RevokeAllSessionsCommandHandler> logger)
    {
        _sessionService = sessionService;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async ValueTask<int> Handle(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        
        // If ExceptSessionId is not provided, try to identify the current session
        var exceptSessionId = command.ExceptSessionId;
        if (!exceptSessionId.HasValue)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                // Get IP address (check for forwarded headers first, as this is used in BFF pattern)
                var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].ToString();
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                }

                // Get User Agent (check for forwarded headers first, as this is used in BFF pattern)
                var userAgent = httpContext.Request.Headers["X-Forwarded-User-Agent"].ToString();
                if (string.IsNullOrWhiteSpace(userAgent))
                {
                    userAgent = httpContext.Request.Headers.UserAgent.ToString();
                }

                // Try to find the most recent session matching this IP and User Agent
                exceptSessionId = await _sessionService.GetMostRecentSessionIdAsync(
                    userId,
                    ipAddress,
                    userAgent,
                    cancellationToken);

                if (exceptSessionId.HasValue)
                {
                    _logger.LogInformation(
                        "Identified current session {SessionId} for user {UserId} by IP {IpAddress}",
                        exceptSessionId.Value,
                        userId,
                        ipAddress);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not identify current session for user {UserId}. All sessions will be revoked.",
                        userId);
                }
            }
        }
        
        return await _sessionService.RevokeAllSessionsAsync(
            userId,
            userId,
            exceptSessionId,
            "User requested logout from all devices",
            cancellationToken);
    }
}
