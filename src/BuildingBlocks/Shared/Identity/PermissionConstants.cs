namespace FSH.Framework.Shared.Constants;

/// <summary>
/// Central permission registry.
/// Modules should register their permissions using the Register() method during initialization.
/// </summary>
public static class PermissionConstants
{
    private static readonly List<FshPermission> _all = new()
    {
        // Core platform permissions (not module-specific)
        
        // Hangfire / Dashboard
        new(ActionConstants.View, ResourceConstants.Hangfire, IsBasic: true),
        new(ActionConstants.View, ResourceConstants.Dashboard, IsBasic: true),
    };

    /// <summary>
    /// Register additional permissions from external projects/modules.
    /// Modules should call this during their initialization to register their permissions.
    /// </summary>
    /// <param name="additionalPermissions">Permissions to register</param>
    public static void Register(IEnumerable<FshPermission> additionalPermissions)
    {
        ArgumentNullException.ThrowIfNull(additionalPermissions);
        
        _all.AddRange(from permission in additionalPermissions
                      where !_all.Any(p => p.Name == permission.Name)
                      select permission);
    }

    public const string RequiredPermissionPolicyName = "RequiredPermission";
    
    /// <summary>
    /// All registered permissions (core + modules).
    /// </summary>
    public static IReadOnlyList<FshPermission> All => _all.AsReadOnly();
    
    /// <summary>
    /// Permissions that require root tenant access.
    /// </summary>
    public static IReadOnlyList<FshPermission> Root => [.. _all.Where(p => p.IsRoot)];
    
    /// <summary>
    /// Admin-level permissions (non-root).
    /// </summary>
    public static IReadOnlyList<FshPermission> Admin => [.. _all.Where(p => !p.IsRoot)];
    
    /// <summary>
    /// Basic permissions available to all authenticated users.
    /// </summary>
    public static IReadOnlyList<FshPermission> Basic => [.. _all.Where(p => p.IsBasic)];
}

/// <summary>
/// Represents a permission in the system.
/// Permissions are formatted as "Permissions.{Resource}.{Action}".
/// </summary>
/// <param name="Action">The action (e.g., View, Create, Update, Delete)</param>
/// <param name="Resource">The resource (e.g., Users, Roles, Tenants)</param>
/// <param name="IsBasic">Whether this permission is included in the Basic role</param>
/// <param name="IsRoot">Whether this permission requires root tenant access</param>
public record FshPermission(string Action, string Resource, bool IsBasic = false, bool IsRoot = false)
{
    /// <summary>
    /// Gets the full permission name in format "Permissions.{Resource}.{Action}".
    /// </summary>
    public string Name => NameFor(Action, Resource);
    
    /// <summary>
    /// Gets a human-readable description generated from Action and Resource.
    /// </summary>
    public string Description => $"{FormatAction(Action)} {FormatResource(Resource)}";
    
    /// <summary>
    /// Builds a permission name from action and resource.
    /// </summary>
    public static string NameFor(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }
    
    /// <summary>
    /// Formats an action name for display (e.g., "UpgradeSubscription" -> "Upgrade Subscription").
    /// </summary>
    private static string FormatAction(string action)
    {
        if (string.IsNullOrEmpty(action)) return action;
        
        // Add spaces before capital letters
        return string.Concat(action.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }
    
    /// <summary>
    /// Formats a resource name for display (handles singular/plural).
    /// </summary>
    private static string FormatResource(string resource)
    {
        if (string.IsNullOrEmpty(resource)) return resource;
        
        // Add spaces before capital letters
        var formatted = string.Concat(resource.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
        
        return formatted;
    }
}
