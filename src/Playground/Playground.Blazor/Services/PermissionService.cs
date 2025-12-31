using System.Security.Claims;

namespace FSH.Playground.Blazor.Services;

/// <summary>
/// Service for checking user permissions in Blazor.
/// Queries permissions from the API instead of reading from JWT claims.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all permissions for the current user.
    /// Results are cached per user session.
    /// Returns null if permissions couldn't be loaded.
    /// </summary>
    Task<List<string>?> GetPermissionsAsync();
    
    /// <summary>
    /// Checks if the current user has a specific permission.
    /// Returns false if permissions couldn't be loaded or permission is not found.
    /// </summary>
    Task<bool> HasPermissionAsync(string permission);
    
    /// <summary>
    /// Clears the cached permissions (call after login/logout).
    /// </summary>
    void ClearCache();
}

public class PermissionService : IPermissionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PermissionService> _logger;
    private List<string>? _cachedPermissions;
    private bool _fetchAttempted;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public PermissionService(HttpClient httpClient, ILogger<PermissionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<string>?> GetPermissionsAsync()
    {
        // Return cached permissions if available
        if (_fetchAttempted && _cachedPermissions != null)
        {
            return _cachedPermissions;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_fetchAttempted && _cachedPermissions != null)
            {
                return _cachedPermissions;
            }

            _logger.LogInformation("Fetching permissions from API");
            _fetchAttempted = true;

            try
            {
                // Query permissions from API
                var response = await _httpClient.GetAsync("/api/v1/identity/permissions");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch permissions: {StatusCode} - {Reason}", 
                        response.StatusCode, response.ReasonPhrase);
                    _cachedPermissions = null;
                    return null;
                }

                var permissions = await response.Content.ReadFromJsonAsync<List<string>>();
                
                if (permissions == null)
                {
                    _logger.LogWarning("Received null permissions from API");
                    _cachedPermissions = null;
                    return null;
                }

                _logger.LogInformation("Successfully fetched {Count} permissions from API", permissions.Count);

                // Cache the permissions
                _cachedPermissions = permissions;
                
                return permissions;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching permissions from API");
                _cachedPermissions = null;
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching permissions from API");
                _cachedPermissions = null;
                return null;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            _logger.LogDebug("No permission specified, allowing access");
            return true; // No permission required
        }

        var permissions = await GetPermissionsAsync();
        
        if (permissions == null)
        {
            _logger.LogWarning("Permissions not available, denying access to '{Permission}'", permission);
            return false; // Failed to load permissions, deny access
        }

        var hasPermission = permissions.Contains(permission);
        
        _logger.LogDebug("Permission check for '{Permission}': {Result}", 
            permission, hasPermission ? "GRANTED" : "DENIED");
        
        return hasPermission;
    }

    public void ClearCache()
    {
        _logger.LogInformation("Clearing permissions cache");
        _cachedPermissions = null;
        _fetchAttempted = false;
    }
}
