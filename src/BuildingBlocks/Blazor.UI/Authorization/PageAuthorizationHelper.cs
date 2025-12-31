using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace FSH.BuildingBlocks.Blazor.UI.Authorization;

/// <summary>
/// Helper to check page-level permission attributes.
/// </summary>
public static class PageAuthorizationHelper
{
    /// <summary>
    /// Gets the RequirePermissionsAttribute from a component type.
    /// </summary>
    public static RequirePermissionsAttribute? GetRequiredPermissions(Type componentType)
    {
        return componentType.GetCustomAttribute<RequirePermissionsAttribute>();
    }
    
    /// <summary>
    /// Checks if a component has permission requirements.
    /// </summary>
    public static bool HasPermissionRequirements(Type componentType)
    {
        return GetRequiredPermissions(componentType) != null;
    }
}
