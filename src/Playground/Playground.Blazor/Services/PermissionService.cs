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
    /// </summary>
    Task<List<string>> GetPermissionsAsync();
    
    /// <summary>
    /// Checks if the current user has a specific permission.
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
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public PermissionService(HttpClient httpClient, ILogger<PermissionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<string>> GetPermissionsAsync()
    {
        // Return cached permissions if available
        if (_cachedPermissions != null)
        {
            return _cachedPermissions;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedPermissions != null)
            {
                return _cachedPermissions;
            }

            _logger.LogInformation("Fetching permissions from API");

            // Query permissions from API
            var response = await _httpClient.GetAsync("/api/v1/identity/permissions");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch permissions: {StatusCode}", response.StatusCode);
                return new List<string>();
            }

            var permissions = await response.Content.ReadFromJsonAsync<List<string>>() 
                ?? new List<string>();

            _logger.LogInformation("Fetched {Count} permissions from API", permissions.Count);

            // Cache the permissions
            _cachedPermissions = permissions;
            
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permissions from API");
            return new List<string>();
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
            return true; // No permission required
        }

        var permissions = await GetPermissionsAsync();
        return permissions.Contains(permission);
    }

    public void ClearCache()
    {
        _logger.LogInformation("Clearing permissions cache");
        _cachedPermissions = null;
    }
}
