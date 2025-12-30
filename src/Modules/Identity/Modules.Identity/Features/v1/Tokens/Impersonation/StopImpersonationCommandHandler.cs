using FSH.Framework.Core.Context;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Features.v1.Tokens.Impersonation;

public sealed class StopImpersonationCommandHandler : ICommandHandler<StopImpersonationCommand, bool>
{
    private readonly ICurrentUser _currentUser;
    private readonly IAuditClient _auditClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<StopImpersonationCommandHandler> _logger;

    public StopImpersonationCommandHandler(
        ICurrentUser currentUser,
        IAuditClient auditClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<StopImpersonationCommandHandler> logger)
    {
        _currentUser = currentUser;
        _auditClient = auditClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async ValueTask<bool> Handle(StopImpersonationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get context for auditing
        var http = _httpContextAccessor.HttpContext;
        var ip = http?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = http?.Request.Headers.UserAgent.ToString() ?? "unknown";
        var clientId = http?.Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId)) clientId = "web";

        var claims = _currentUser.GetUserClaims();
        if (claims is null)
        {
            return false;
        }

        var impersonatorId = claims.FirstOrDefault(c => c.Type == ImpersonationClaims.Impersonator)?.Value;
        var impersonatorName = claims.FirstOrDefault(c => c.Type == ImpersonationClaims.ImpersonatorName)?.Value;
        var targetUserId = _currentUser.GetUserId().ToString();
        var targetUserEmail = _currentUser.GetUserEmail();

        if (string.IsNullOrEmpty(impersonatorId))
        {
            _logger.LogWarning("Stop impersonation called but no impersonation is active");
            return false;
        }

        // Audit stop impersonation
        var auditClaims = new Dictionary<string, object?>
        {
            ["ip"] = ip,
            ["userAgent"] = ua,
            ["impersonatorId"] = impersonatorId,
            ["impersonatorName"] = impersonatorName,
            ["targetUserId"] = targetUserId,
            ["targetUserEmail"] = targetUserEmail
        };

        await _auditClient.WriteSecurityAsync(
            SecurityAction.TokenRevoked,
            subjectId: impersonatorId,
            clientId: clientId,
            authMethod: "Impersonation",
            reasonCode: $"Stopped impersonating {targetUserEmail}",
            claims: auditClaims,
            severity: AuditSeverity.Information,
            source: "Identity",
            ct: cancellationToken);

        _logger.LogInformation(
            "User {ImpersonatorId} stopped impersonating user {TargetUserId}",
            impersonatorId,
            targetUserId);

        // Note: The client needs to discard the current token and request a new one
        // for the original user. This handler just logs the action.
        return true;
    }
}
