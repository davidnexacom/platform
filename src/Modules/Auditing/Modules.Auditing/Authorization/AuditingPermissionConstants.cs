using FSH.Framework.Shared.Authorization;
using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Auditing.Authorization;

/// <summary>
/// Auditing module permissions.
/// Permissions are automatically discovered from the Names nested classes.
/// </summary>
public sealed class AuditingPermissionConstants : ModulePermissionRegistry
{
    /// <summary>
    /// Type-safe permission names for use in navigation, authorization, etc.
    /// Define permissions here once - they will be automatically converted to FshPermission objects.
    /// </summary>
    public static class Names
    {
        public static class AuditTrails
        {
            [BasicPermission]
            public static string View => FshPermission.NameFor(ActionConstants.View, ResourceConstants.AuditTrails);
        }
    }
    
    // Singleton instance for easy access
    private static readonly Lazy<AuditingPermissionConstants> _instance = new();
    public static AuditingPermissionConstants Instance => _instance.Value;
}
