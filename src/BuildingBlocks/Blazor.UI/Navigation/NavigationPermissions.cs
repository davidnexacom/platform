namespace FSH.BuildingBlocks.Blazor.UI.Navigation;

/// <summary>
/// Type-safe helper for building permission strings for navigation items.
/// Generates permission names in the format "Permissions.{Resource}.{Action}".
/// 
/// Note: For module-specific permissions, prefer using the module's own permission constants
/// (e.g., IdentityPermissionConstants.Names, MultitenancyPermissionConstants.Names).
/// This class provides a fallback for general-purpose permission building.
/// </summary>
public static class NavigationPermissions
{
    /// <summary>
    /// Build a permission string from action and resource.
    /// </summary>
    public static string Build(string action, string resource)
    {
        return $"Permissions.{resource}.{action}";
    }

    /// <summary>
    /// Common permissions as static properties for IntelliSense support.
    /// These mirror the module-specific permission constants for convenience.
    /// </summary>
    public static class Users
    {
        public static string View => "Permissions.Users.View";
        public static string Create => "Permissions.Users.Create";
        public static string Update => "Permissions.Users.Update";
        public static string Delete => "Permissions.Users.Delete";
        public static string Export => "Permissions.Users.Export";
        public static string Search => "Permissions.Users.Search";
        public static string Impersonate => "Permissions.Users.Impersonate";
    }

    public static class Roles
    {
        public static string View => "Permissions.Roles.View";
        public static string Create => "Permissions.Roles.Create";
        public static string Update => "Permissions.Roles.Update";
        public static string Delete => "Permissions.Roles.Delete";
    }

    public static class UserRoles
    {
        public static string View => "Permissions.UserRoles.View";
        public static string Update => "Permissions.UserRoles.Update";
    }

    public static class RoleClaims
    {
        public static string View => "Permissions.RoleClaims.View";
        public static string Update => "Permissions.RoleClaims.Update";
    }

    public static class Tenants
    {
        public static string View => "Permissions.Tenants.View";
        public static string Create => "Permissions.Tenants.Create";
        public static string Update => "Permissions.Tenants.Update";
        public static string UpgradeSubscription => "Permissions.Tenants.UpgradeSubscription";
    }

    public static class AuditTrails
    {
        public static string View => "Permissions.AuditTrails.View";
    }

    public static class Hangfire
    {
        public static string View => "Permissions.Hangfire.View";
    }

    public static class Dashboard
    {
        public static string View => "Permissions.Dashboard.View";
    }
}
