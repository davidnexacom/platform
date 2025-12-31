namespace FSH.BuildingBlocks.Blazor.UI.Authorization;

/// <summary>
/// Specifies the permission requirements for a page.
/// Can be applied to Razor components to enforce authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequirePermissionsAttribute : Attribute
{
    /// <summary>
    /// The permissions required to access the page.
    /// </summary>
    public string[] Permissions { get; }
    
    /// <summary>
    /// Whether all permissions are required (AND logic) or just one (OR logic).
    /// Default is OR (RequireAll = false).
    /// </summary>
    public bool RequireAll { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the RequirePermissionsAttribute with a single permission.
    /// </summary>
    /// <param name="permission">The permission required</param>
    public RequirePermissionsAttribute(string permission)
    {
        Permissions = new[] { permission };
        RequireAll = false;
    }
    
    /// <summary>
    /// Initializes a new instance of the RequirePermissionsAttribute with multiple permissions.
    /// </summary>
    /// <param name="permissions">The permissions required</param>
    public RequirePermissionsAttribute(params string[] permissions)
    {
        Permissions = permissions;
        RequireAll = false;
    }
}
