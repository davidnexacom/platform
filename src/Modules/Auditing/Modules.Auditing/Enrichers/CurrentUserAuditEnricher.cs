using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Constants;
using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;

namespace FSH.Modules.Auditing.Enrichers;

/// <summary>
/// Enricher that captures current user and tenant information.
/// Handles impersonation by capturing both the real user and impersonated user.
/// </summary>
public sealed class CurrentUserAuditEnricher : IAuditEnricher
{
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAuditEnricher(ICurrentUser currentUser, IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(IAuditEvent auditEvent)
    {
        if (auditEvent is not AuditEnvelope envelope)
            return;

        // Skip if user/tenant already set explicitly
        if (!string.IsNullOrEmpty(envelope.UserId) && !string.IsNullOrEmpty(envelope.TenantId))
            return;

        if (!_currentUser.IsAuthenticated())
            return;

        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        // Get current (potentially impersonated) user
        var userId = _currentUser.GetUserId().ToString();
        var userEmail = _currentUser.GetUserEmail();
        var userName = _currentUser.Name ?? userEmail;
        var tenantId = _currentUser.GetTenant();

        // Check for impersonation
        string? realUserId = null;
        string? realUserName = null;
        
        if (user != null)
        {
            var impersonatorClaim = user.FindFirst(ClaimConstants.Impersonator);
            var impersonatorNameClaim = user.FindFirst(ClaimConstants.ImpersonatorName);
            var originalUserIdClaim = user.FindFirst(ClaimConstants.OriginalUserId);

            if (impersonatorClaim != null || originalUserIdClaim != null)
            {
                realUserId = impersonatorClaim?.Value ?? originalUserIdClaim?.Value;
                realUserName = impersonatorNameClaim?.Value;
            }
        }

        // Update envelope properties using reflection
        // If impersonating, store REAL user in UserId/UserName, impersonated user in payload
        var userIdToSet = !string.IsNullOrEmpty(realUserId) ? realUserId : userId;
        var userNameToSet = !string.IsNullOrEmpty(realUserName) ? realUserName : userName;

        if (string.IsNullOrEmpty(envelope.UserId))
        {
            var userIdProp = typeof(AuditEnvelope).GetProperty(nameof(AuditEnvelope.UserId));
            userIdProp?.SetValue(envelope, userIdToSet);
        }

        if (string.IsNullOrEmpty(envelope.UserName))
        {
            var userNameProp = typeof(AuditEnvelope).GetProperty(nameof(AuditEnvelope.UserName));
            userNameProp?.SetValue(envelope, userNameToSet);
        }

        if (string.IsNullOrEmpty(envelope.TenantId))
        {
            var tenantIdProp = typeof(AuditEnvelope).GetProperty(nameof(AuditEnvelope.TenantId));
            tenantIdProp?.SetValue(envelope, tenantId);
        }

        // If impersonating, add impersonated user info to payload
        if (!string.IsNullOrEmpty(realUserId) && envelope.Payload is SecurityEventPayload securityPayload)
        {
            var claims = securityPayload.ClaimsSnapshot != null
                ? new Dictionary<string, object?>(securityPayload.ClaimsSnapshot)
                : new Dictionary<string, object?>();

            claims["isImpersonating"] = true;
            claims["impersonatedUserId"] = userId;
            claims["impersonatedUserEmail"] = userEmail;
            claims["realUserId"] = realUserId;
            claims["realUserName"] = realUserName;

            var claimsProperty = typeof(SecurityEventPayload).GetProperty(nameof(SecurityEventPayload.ClaimsSnapshot));
            claimsProperty?.SetValue(securityPayload, claims);
        }
    }
}
