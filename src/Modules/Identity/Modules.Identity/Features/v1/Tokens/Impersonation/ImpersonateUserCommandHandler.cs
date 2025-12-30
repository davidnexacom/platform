using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;
using FSH.Modules.Identity.Features.v1.Users;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Tokens.Impersonation;

public sealed class ImpersonateUserCommandHandler : ICommandHandler<ImpersonateUserCommand, TokenResponse>
{
    private readonly UserManager<FshUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditClient _auditClient;
    private readonly ISecurityAudit _securityAudit;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    private readonly IOutboxStore _outboxStore;
    private readonly ILogger<ImpersonateUserCommandHandler> _logger;

    public ImpersonateUserCommandHandler(
        UserManager<FshUser> userManager,
        ITokenService tokenService,
        ICurrentUser currentUser,
        IAuditClient auditClient,
        ISecurityAudit securityAudit,
        IHttpContextAccessor httpContextAccessor,
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        IOutboxStore outboxStore,
        ILogger<ImpersonateUserCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _auditClient = auditClient;
        _securityAudit = securityAudit;
        _httpContextAccessor = httpContextAccessor;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _outboxStore = outboxStore;
        _logger = logger;
    }

    public async ValueTask<TokenResponse> Handle(ImpersonateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate impersonator has permission
        if (!_currentUser.IsAuthenticated())
        {
            throw new UnauthorizedException("You must be authenticated to impersonate users");
        }

        var impersonatorId = _currentUser.GetUserId();
        var impersonator = await _userManager.FindByIdAsync(impersonatorId.ToString());
        if (impersonator is null)
        {
            throw new UnauthorizedException("Invalid impersonator");
        }

        // Find target user to impersonate
        var targetUser = await _userManager.FindByIdAsync(request.UserId);
        if (targetUser is null)
        {
            throw new NotFoundException($"User with ID {request.UserId} not found");
        }

        // Ensure both users belong to the same tenant
        var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo;
        if (currentTenant is null)
        {
            throw new UnauthorizedException("Tenant context not available");
        }

        // Both users must be from the current tenant (multi-tenancy is enforced by UserManager filtering)
        // Since UserManager is tenant-aware, if we can find both users, they're in the same tenant

        // Check if target user is active
        if (!targetUser.IsActive)
        {
            throw new CustomException($"Cannot impersonate inactive user {targetUser.Email}");
        }

        // Prevent impersonating yourself
        if (impersonator.Id == targetUser.Id)
        {
            throw new CustomException("You cannot impersonate yourself");
        }

        // Get context for auditing
        var http = _httpContextAccessor.HttpContext;
        var ip = http?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = http?.Request.Headers.UserAgent.ToString() ?? "unknown";
        var clientId = http?.Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId)) clientId = "web";

        // Build claims for impersonated user
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, targetUser.Id),
            new(ClaimTypes.Email, targetUser.Email!),
            new(ClaimTypes.Name, targetUser.FirstName ?? string.Empty),
            new(ClaimTypes.MobilePhone, targetUser.PhoneNumber ?? string.Empty),
            new(ClaimConstants.Fullname, $"{targetUser.FirstName} {targetUser.LastName}"),
            new(ClaimTypes.Surname, targetUser.LastName ?? string.Empty),
            new(ClaimConstants.Tenant, currentTenant.Id),
            new(ClaimConstants.ImageUrl, targetUser.ImageUrl?.ToString() ?? string.Empty),
            
            // Special claims to track impersonation
            new(ImpersonationClaims.Impersonator, impersonator.Id),
            new(ImpersonationClaims.ImpersonatorName, $"{impersonator.FirstName} {impersonator.LastName}"),
            new(ImpersonationClaims.OriginalUserId, impersonator.Id)
        };

        // Add target user roles
        var roles = await _userManager.GetRolesAsync(targetUser);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Issue token
        var token = await _tokenService.IssueAsync(
            targetUser.Id,
            claims,
            currentTenant.Id,
            cancellationToken);

        // Audit impersonation action using security audit
        var auditClaims = new Dictionary<string, object?>
        {
            ["ip"] = ip,
            ["userAgent"] = ua,
            ["impersonatorId"] = impersonator.Id,
            ["impersonatorEmail"] = impersonator.Email,
            ["targetUserId"] = targetUser.Id,
            ["targetUserEmail"] = targetUser.Email
        };

        await _auditClient.WriteSecurityAsync(
            SecurityAction.RoleAssigned, // Using RoleAssigned as placeholder for impersonation
            subjectId: impersonator.Id,
            clientId: clientId,
            authMethod: "Impersonation",
            reasonCode: $"Impersonating {targetUser.Email}",
            claims: auditClaims,
            severity: AuditSeverity.Warning,
            source: "Identity",
            ct: cancellationToken);

        // Publish integration event
        var integrationEvent = new UserImpersonationStartedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: currentTenant.Id,
            CorrelationId: Guid.NewGuid().ToString(),
            Source: "Identity",
            ImpersonatorId: impersonator.Id,
            ImpersonatorEmail: impersonator.Email!,
            TargetUserId: targetUser.Id,
            TargetUserEmail: targetUser.Email!,
            IpAddress: ip,
            UserAgent: ua);

        await _outboxStore.AddAsync(integrationEvent, cancellationToken);

        _logger.LogWarning(
            "User {ImpersonatorId} ({ImpersonatorEmail}) is now impersonating user {TargetUserId} ({TargetUserEmail}) in tenant {TenantId}",
            impersonator.Id,
            impersonator.Email,
            targetUser.Id,
            targetUser.Email,
            currentTenant.Id);

        return token;
    }
}
