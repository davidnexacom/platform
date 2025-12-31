using FSH.Framework.Shared.Authorization;
using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Multitenancy.Authorization;

/// <summary>
/// Multitenancy module permissions.
/// Permissions are automatically discovered from the Names nested classes.
/// </summary>
public sealed class MultitenancyPermissionConstants : ModulePermissionRegistry
{
    /// <summary>
    /// Type-safe permission names for use in navigation, authorization, etc.
    /// Define permissions here once - they will be automatically converted to FshPermission objects.
    /// </summary>
    public static class Names
    {
        public static class Tenants
        {
            [RootPermission]
            public static string View => FshPermission.NameFor(ActionConstants.View, ResourceConstants.Tenants);
            
            [RootPermission]
            public static string Create => FshPermission.NameFor(ActionConstants.Create, ResourceConstants.Tenants);
            
            [RootPermission]
            public static string Update => FshPermission.NameFor(ActionConstants.Update, ResourceConstants.Tenants);
            
            [RootPermission]
            public static string UpgradeSubscription => FshPermission.NameFor(ActionConstants.UpgradeSubscription, ResourceConstants.Tenants);
        }
    }
    
    // Singleton instance for easy access
    private static readonly Lazy<MultitenancyPermissionConstants> _instance = new();
    public static MultitenancyPermissionConstants Instance => _instance.Value;
}
