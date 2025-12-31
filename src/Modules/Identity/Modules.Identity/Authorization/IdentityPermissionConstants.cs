using FSH.Framework.Shared.Authorization;
using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Identity.Authorization;

/// <summary>
/// Identity module permissions.
/// Permissions are automatically discovered from the Names nested classes.
/// </summary>
public sealed class IdentityPermissionConstants : ModulePermissionRegistry
{
    /// <summary>
    /// Type-safe permission names for use in navigation, authorization, etc.
    /// Define permissions here once - they will be automatically converted to FshPermission objects.
    /// </summary>
    public static class Names
    {
        public static class Users
        {
            [BasicPermission]
            public static string View => FshPermission.NameFor(ActionConstants.View, ResourceConstants.Users);
            
            public static string Search => FshPermission.NameFor(ActionConstants.Search, ResourceConstants.Users);
            public static string Create => FshPermission.NameFor(ActionConstants.Create, ResourceConstants.Users);
            public static string Update => FshPermission.NameFor(ActionConstants.Update, ResourceConstants.Users);
            public static string Delete => FshPermission.NameFor(ActionConstants.Delete, ResourceConstants.Users);
            public static string Export => FshPermission.NameFor(ActionConstants.Export, ResourceConstants.Users);
            public static string Impersonate => FshPermission.NameFor(ActionConstants.Impersonate, ResourceConstants.Users);
        }

        public static class UserRoles
        {
            [BasicPermission]
            public static string View => FshPermission.NameFor(ActionConstants.View, ResourceConstants.UserRoles);
            
            public static string Update => FshPermission.NameFor(ActionConstants.Update, ResourceConstants.UserRoles);
        }

        public static class Roles
        {
            [BasicPermission]
            public static string View => FshPermission.NameFor(ActionConstants.View, ResourceConstants.Roles);
            
            public static string Create => FshPermission.NameFor(ActionConstants.Create, ResourceConstants.Roles);
            public static string Update => FshPermission.NameFor(ActionConstants.Update, ResourceConstants.Roles);
            public static string Delete => FshPermission.NameFor(ActionConstants.Delete, ResourceConstants.Roles);
        }

        public static class RoleClaims
        {
            [BasicPermission]
            public static string View => FshPermission.NameFor(ActionConstants.View, ResourceConstants.RoleClaims);
            
            public static string Update => FshPermission.NameFor(ActionConstants.Update, ResourceConstants.RoleClaims);
        }
    }
    
    // Singleton instance for easy access
    private static readonly Lazy<IdentityPermissionConstants> _instance = new();
    public static IdentityPermissionConstants Instance => _instance.Value;
}
