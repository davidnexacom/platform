using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.Impersonation;
using FSH.Modules.Identity.Features.v1.Users;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FSH.Modules.Identity.Features.v1.Tokens.Impersonation;

public sealed class StopImpersonationCommandHandler : ICommandHandler<StopImpersonationCommand, TokenResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly IAuditClient _auditClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<StopImpersonationCommandHandler> _logger;
    private readonly UserManager<FshUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IIdentityService _identityService;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public StopImpersonationCommandHandler(
        ICurrentUser currentUser,
        IAuditClient auditClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<StopImpersonationCommandHandler> logger,
        UserManager<FshUser> userManager,
        ITokenService tokenService,
        IIdentityService identityService,
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    {
        _currentUser = currentUser;
        _auditClient = auditClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _userManager = userManager;
        _tokenService = tokenService;
        _identityService = identityService;
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public async ValueTask<TokenResponse> Handle(StopImpersonationCommand request, CancellationToken cancellationToken)
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
            throw new UnauthorizedException("No user claims found");
        }

        var impersonatorId = claims.FirstOrDefault(c => c.Type == ImpersonationClaims.Impersonator)?.Value;
        var impersonatorName = claims.FirstOrDefault(c => c.Type == ImpersonationClaims.ImpersonatorName)?.Value;
        var targetUserId = _currentUser.GetUserId().ToString();
        var targetUserEmail = _currentUser.GetUserEmail();

        if (string.IsNullOrEmpty(impersonatorId))
        {
            throw new CustomException("No impersonation is active");
        }

        // Get the original user (impersonator)
        var originalUser = await _userManager.FindByIdAsync(impersonatorId);
        if (originalUser is null)
        {
            throw new NotFoundException($"Original user {impersonatorId} not found");
        }

        // Check if original user is still active
        if (!originalUser.IsActive)
        {
            throw new CustomException("Original user account is no longer active");
        }

        var currentTenant = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo;
        if (currentTenant is null)
        {
            throw new UnauthorizedException("Tenant context not available");
        }

        // Build claims for original user
        var originalUserClaims = new List<Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, originalUser.Id),
            new(ClaimTypes.Email, originalUser.Email!),
            new(ClaimTypes.Name, originalUser.FirstName ?? string.Empty),
            new(ClaimTypes.MobilePhone, originalUser.PhoneNumber ?? string.Empty),
            new(ClaimConstants.Fullname, $"{originalUser.FirstName} {originalUser.LastName}"),
            new(ClaimTypes.Surname, originalUser.LastName ?? string.Empty),
            new(ClaimConstants.Tenant, currentTenant.Id),
            new(ClaimConstants.ImageUrl, originalUser.ImageUrl?.ToString() ?? string.Empty)
        };

        // Add original user roles
        var roles = await _userManager.GetRolesAsync(originalUser);
        originalUserClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Issue new token for original user
        var token = await _tokenService.IssueAsync(
            originalUser.Id,
            originalUserClaims,
            currentTenant.Id,
            cancellationToken);

        // Store new refresh token
        await _identityService.StoreRefreshTokenAsync(
            originalUser.Id,
            token.RefreshToken,
            token.RefreshTokenExpiresAt,
            cancellationToken);

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
            "User {ImpersonatorId} ({ImpersonatorEmail}) stopped impersonating user {TargetUserId} ({TargetEmail})",
            impersonatorId,
            originalUser.Email,
            targetUserId,
            targetUserEmail);

        return token;
    }
}
