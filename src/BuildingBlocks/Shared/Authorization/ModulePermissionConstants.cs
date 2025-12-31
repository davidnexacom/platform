using FSH.Framework.Shared.Constants;

namespace FSH.Framework.Shared.Authorization;

/// <summary>
/// Base class for module permission constants that provides common functionality
/// and reduces code duplication.
/// </summary>
public abstract class ModulePermissionConstants
{
    private readonly List<FshPermission> _permissions;

    protected ModulePermissionConstants(List<FshPermission> permissions)
    {
        _permissions = permissions;
    }

    /// <summary>
    /// All permissions defined by this module.
    /// </summary>
    public IReadOnlyList<FshPermission> All => _permissions.AsReadOnly();

    /// <summary>
    /// Helper to create a new permission.
    /// </summary>
    protected static FshPermission Create(string action, string resource, bool isBasic = false, bool isRoot = false)
    {
        return new FshPermission(action, resource, isBasic, isRoot);
    }

    /// <summary>
    /// Helper to generate a type-safe permission name.
    /// </summary>
    protected static string NameFor(string action, string resource)
    {
        return FshPermission.NameFor(action, resource);
    }
}
