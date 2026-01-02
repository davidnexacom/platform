using FSH.Framework.Shared.Constants;
using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace FSH.Modules.Auditing.Enrichers;

/// <summary>
/// Enricher that captures HTTP request context and impersonation information.
/// </summary>
public sealed class HttpContextAuditEnricher : IAuditEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextAuditEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(IAuditEvent auditEvent)
    {
        if (auditEvent is not AuditEnvelope envelope)
            return;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        // Capture HTTP context information
        var request = httpContext.Request;
        var user = httpContext.User;

        // Build enriched context
        var httpMethod = request.Method;
        var endpoint = $"{httpMethod} {request.Path}{request.QueryString}";
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = request.Headers.UserAgent.ToString();

        // Detect impersonation
        string? realUserId = null;
        string? realUserName = null;
        bool isImpersonating = false;

        if (user?.Identity?.IsAuthenticated == true)
        {
            // Check for impersonation claims
            var impersonatorClaim = user.FindFirst(ClaimConstants.Impersonator);
            var impersonatorNameClaim = user.FindFirst(ClaimConstants.ImpersonatorName);
            var originalUserIdClaim = user.FindFirst(ClaimConstants.OriginalUserId);

            if (impersonatorClaim != null || originalUserIdClaim != null)
            {
                isImpersonating = true;
                realUserId = impersonatorClaim?.Value ?? originalUserIdClaim?.Value;
                realUserName = impersonatorNameClaim?.Value;
            }
        }

        // Enrich payload with HTTP context and impersonation info
        if (envelope.Payload is SecurityEventPayload securityPayload)
        {
            EnrichSecurityPayload(securityPayload, endpoint, ipAddress, userAgent, 
                                isImpersonating, realUserId, realUserName);
        }
        else if (envelope.Payload is ActivityEventPayload activityPayload)
        {
            EnrichActivityPayload(activityPayload, endpoint, ipAddress, userAgent);
        }
    }

    private static void EnrichSecurityPayload(
        SecurityEventPayload payload,
        string endpoint,
        string ipAddress,
        string userAgent,
        bool isImpersonating,
        string? realUserId,
        string? realUserName)
    {
        var claims = payload.ClaimsSnapshot != null 
            ? new Dictionary<string, object?>(payload.ClaimsSnapshot)
            : new Dictionary<string, object?>();

        // Add HTTP context
        claims["endpoint"] = endpoint;
        if (!claims.ContainsKey("ip"))
            claims["ip"] = ipAddress;
        if (!claims.ContainsKey("userAgent"))
            claims["userAgent"] = userAgent;

        // Add impersonation information
        if (isImpersonating)
        {
            claims["isImpersonating"] = true;
            claims["realUserId"] = realUserId;
            claims["realUserName"] = realUserName;
            claims["impersonatedUserId"] = payload.SubjectId; // The user being impersonated
        }

        // Update the claims in the payload using reflection
        var claimsProperty = typeof(SecurityEventPayload).GetProperty(nameof(SecurityEventPayload.ClaimsSnapshot));
        claimsProperty?.SetValue(payload, claims);
    }

    private static void EnrichActivityPayload(
        ActivityEventPayload payload,
        string endpoint,
        string ipAddress,
        string userAgent)
    {
        // Store HTTP context in request preview
        var requestPreview = payload.RequestPreview is Dictionary<string, object?> dict
            ? new Dictionary<string, object?>(dict)
            : new Dictionary<string, object?>
            {
                ["original"] = payload.RequestPreview
            };

        requestPreview["endpoint"] = endpoint;
        requestPreview["ipAddress"] = ipAddress;
        requestPreview["userAgent"] = userAgent;

        // Update request preview using reflection
        var requestProperty = typeof(ActivityEventPayload).GetProperty(nameof(ActivityEventPayload.RequestPreview));
        requestProperty?.SetValue(payload, requestPreview);
    }
}
