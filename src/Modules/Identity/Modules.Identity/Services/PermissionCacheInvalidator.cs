using FSH.Framework.Caching;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Features.v1.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Service for invalidating permission-related cache entries.
/// Should be called whenever roles, permissions, or user-role assignments change.
/// </summary>
public interface IPermissionCacheInvalidator
{
    /// <summary>
    /// Invalidates permission cache for a specific user.
    /// </summary>
    Task InvalidateUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates permission cache for all users with a specific role.
    /// Call this when a role's permissions are modified.
    /// </summary>
    Task InvalidateRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates permission cache for all users.
    /// Use sparingly - only for system-wide permission changes.
    /// </summary>
    Task InvalidateAllPermissionsAsync(CancellationToken cancellationToken = default);
}

public class PermissionCacheInvalidator : IPermissionCacheInvalidator
{
    private readonly ICacheService _cache;
    private readonly IdentityDbContext _db;
    private readonly ILogger<PermissionCacheInvalidator> _logger;

    public PermissionCacheInvalidator(
        ICacheService cache,
        IdentityDbContext db,
        ILogger<PermissionCacheInvalidator> logger)
    {
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    public async Task InvalidateUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating permission cache for user: {UserId}", userId);
        
        var cacheKey = $"perm:{userId}";
        await _cache.RemoveItemAsync(cacheKey, cancellationToken);
        
        _logger.LogInformation("Permission cache invalidated for user: {UserId}", userId);
    }

    public async Task InvalidateRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating permission cache for all users with role: {RoleId}", roleId);
        
        // Get all users with this role from AspNetUserRoles
        var usersInRole = await _db.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("Found {Count} users with role {RoleId}, invalidating their caches", 
            usersInRole.Count, roleId);
        
        // Invalidate cache for each user
        foreach (var userId in usersInRole)
        {
            await InvalidateUserPermissionsAsync(userId, cancellationToken);
        }
        
        _logger.LogInformation("Permission cache invalidated for {Count} users", usersInRole.Count);
    }

    public async Task InvalidateAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Invalidating permission cache for ALL users - this is expensive!");
        
        // Get all user IDs
        var allUserIds = await _db.Users
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("Found {Count} users, invalidating their caches", allUserIds.Count);
        
        // Invalidate cache for each user
        foreach (var userId in allUserIds)
        {
            await InvalidateUserPermissionsAsync(userId, cancellationToken);
        }
        
        _logger.LogInformation("Permission cache invalidated for ALL {Count} users", allUserIds.Count);
    }
}
