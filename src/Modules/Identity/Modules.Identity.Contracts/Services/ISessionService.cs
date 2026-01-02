using FSH.Modules.Identity.Contracts.DTOs;

namespace FSH.Modules.Identity.Contracts.Services;

public interface ISessionService
{
    Task<UserSessionDto> CreateSessionAsync(
        string userId,
        string refreshTokenHash,
        string ipAddress,
        string userAgent,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task<List<UserSessionDto>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<List<UserSessionDto>> GetUserSessionsForAdminAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserSessionDto?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeSessionAsync(
        Guid sessionId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllSessionsAsync(
        string userId,
        string revokedBy,
        Guid? exceptSessionId = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllSessionsForAdminAsync(
        string userId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeSessionForAdminAsync(
        Guid sessionId,
        string revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task UpdateSessionActivityAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task UpdateSessionRefreshTokenAsync(
        string oldRefreshTokenHash,
        string newRefreshTokenHash,
        DateTime newExpiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateSessionAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetSessionIdByRefreshTokenAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetMostRecentSessionIdAsync(
        string userId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default);
}
