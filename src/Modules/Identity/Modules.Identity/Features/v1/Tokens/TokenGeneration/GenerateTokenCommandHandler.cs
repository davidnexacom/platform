using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using Mediator;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Events;

namespace FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;

public sealed class GenerateTokenCommandHandler
    : ICommandHandler<GenerateTokenCommand, TokenResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ISecurityAudit _securityAudit;
    private readonly IHttpContextAccessor _http;
    private readonly IOutboxStore _outboxStore;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    private readonly ISessionService _sessionService;

    public GenerateTokenCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ISecurityAudit securityAudit,
        IHttpContextAccessor http,
        IOutboxStore outboxStore,
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        ISessionService sessionService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _securityAudit = securityAudit;
        _http = http;
        _outboxStore = outboxStore;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _sessionService = sessionService;
    }

    public async ValueTask<TokenResponse> Handle(
        GenerateTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Gather context for auditing
        var http = _http.HttpContext;
        
        // Check for forwarded headers first (from Blazor BFF)
        var ip = http?.Request.Headers["X-Forwarded-For"].ToString();
        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = http?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
        
        // Get User-Agent string for auditing and session storage
        // Note: SessionService will parse Client Hints from HttpContext directly
        var ua = GetUserAgentString(http?.Request.Headers);
        
        var clientId = http?.Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId)) clientId = "web";

        // Validate credentials
        var identityResult = await _identityService
            .ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);

        if (identityResult is null)
        {
            // 1) Audit failed login BEFORE throwing
            await _securityAudit.LoginFailedAsync(
                subjectIdOrName: request.Email,
                clientId: clientId!,
                reason: "InvalidCredentials",
                ip: ip,
                ct: cancellationToken);

            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        // Unpack subject + claims
        var (subject, claims) = identityResult.Value;

        // 2) Audit successful login
        await _securityAudit.LoginSucceededAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? request.Email,
            clientId: clientId!,
            ip: ip,
            userAgent: ua,
            ct: cancellationToken);

        // Issue token
        var token = await _tokenService.IssueAsync(subject, claims, /*extra*/ null, cancellationToken);

        // Persist refresh token (hashed) for this user
        await _identityService.StoreRefreshTokenAsync(subject, token.RefreshToken, token.RefreshTokenExpiresAt, cancellationToken);

        // Create user session for session management (non-blocking, fail gracefully)
        // SessionService will automatically parse Client Hints from HttpContext via IHttpContextAccessor
        try
        {
            var refreshTokenHash = Sha256Short(token.RefreshToken);
            await _sessionService.CreateSessionAsync(
                subject,
                refreshTokenHash,
                ip,
                ua, // Store full User-Agent string for reference
                token.RefreshTokenExpiresAt,
                cancellationToken);
        }
        catch (Exception)
        {
            // Session creation is non-critical - don't fail the login
            // This can happen if migrations haven't been applied yet
        }

        // 3) Audit token issuance with a fingerprint (never raw token)
        var fingerprint = Sha256Short(token.AccessToken);
        await _securityAudit.TokenIssuedAsync(
            userId: subject,
            userName: claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? request.Email,
            clientId: clientId!,
            tokenFingerprint: fingerprint,
            expiresUtc: token.AccessTokenExpiresAt,
            ct: cancellationToken);

        // 4) Enqueue integration event for token generation (sample event for testing eventing)
        var tenantId = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        var correlationId = Guid.NewGuid().ToString();

        var integrationEvent = new TokenGeneratedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            TenantId: tenantId,
            CorrelationId: correlationId,
            Source: "Identity",
            UserId: subject,
            Email: request.Email,
            ClientId: clientId!,
            IpAddress: ip,
            UserAgent: ua,
            TokenFingerprint: fingerprint,
            AccessTokenExpiresAtUtc: token.AccessTokenExpiresAt);

        await _outboxStore.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        return token;
    }

    private static string GetUserAgentString(IHeaderDictionary? headers)
    {
        if (headers is null)
        {
            return "unknown";
        }

        // Check for forwarded User-Agent first (from Blazor BFF)
        var forwardedUa = headers["X-Forwarded-User-Agent"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedUa))
        {
            return forwardedUa;
        }

        // Try standard User-Agent
        var ua = headers.UserAgent.ToString();
        if (!string.IsNullOrWhiteSpace(ua))
        {
            return ua;
        }

        return "unknown";
    }

    private static string Sha256Short(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        // short printable fingerprint; store only this
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}
