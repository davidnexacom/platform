using System.Reflection;
using FSH.Framework.Shared.Constants;

namespace FSH.Framework.Shared.Authorization;

/// <summary>
/// Base class that automatically generates permission list from nested Names classes using reflection.
/// This eliminates duplication - you only define permissions once in the Names classes.
/// </summary>
public abstract class ModulePermissionRegistry
{
    private readonly Lazy<List<FshPermission>> _permissions;

    protected ModulePermissionRegistry()
    {
        _permissions = new Lazy<List<FshPermission>>(() => DiscoverPermissions());
    }

    /// <summary>
    /// All permissions discovered from the Names nested classes.
    /// </summary>
    public IReadOnlyList<FshPermission> All => _permissions.Value.AsReadOnly();

    /// <summary>
    /// Discovers all permissions by reflecting over the Names nested classes.
    /// </summary>
    private List<FshPermission> DiscoverPermissions()
    {
        var permissions = new List<FshPermission>();
        var type = GetType();
        
        // Find the "Names" nested class
        var namesType = type.GetNestedType("Names", BindingFlags.Public | BindingFlags.Static);
        if (namesType == null)
        {
            return permissions;
        }

        // Find all nested classes within Names (e.g., Users, Roles, Tenants)
        var resourceTypes = namesType.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
        
        foreach (var resourceType in resourceTypes)
        {
            var resourceName = resourceType.Name;
            
            // Get all static string properties (e.g., View, Create, Update)
            var properties = resourceType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(string) && p.CanRead);
            
            foreach (var property in properties)
            {
                var actionName = property.Name;
                var permissionName = property.GetValue(null) as string;
                
                if (string.IsNullOrEmpty(permissionName))
                    continue;
                
                // Check if this permission has special attributes
                var isBasic = HasAttribute<BasicPermissionAttribute>(property);
                var isRoot = HasAttribute<RootPermissionAttribute>(property);
                
                // Extract action from the permission name (last part after the last dot)
                // e.g., "Permissions.Users.View" -> "View"
                var parts = permissionName.Split('.');
                var action = parts.Length >= 3 ? parts[2] : actionName;
                
                permissions.Add(new FshPermission(action, resourceName, isBasic, isRoot));
            }
        }
        
        return permissions;
    }
    
    private static bool HasAttribute<T>(PropertyInfo property) where T : Attribute
    {
        return property.GetCustomAttribute<T>() != null;
    }
}

/// <summary>
/// Marks a permission as a basic permission (included in Basic role).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BasicPermissionAttribute : Attribute { }

/// <summary>
/// Marks a permission as requiring root tenant access.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RootPermissionAttribute : Attribute { }
