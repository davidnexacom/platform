using FSH.BuildingBlocks.Blazor.UI.Navigation;

namespace FSH.BuildingBlocks.Blazor.UI.Extensions;

/// <summary>
/// Extension methods for registering navigation items from modules.
/// </summary>
public static class NavigationRegistryExtensions
{
    /// <summary>
    /// Helper to register a standard section with common sections predefined.
    /// </summary>
    public static INavigationRegistry AddStandardSection(
        this INavigationRegistry registry,
        string id,
        string title,
        int order)
    {
        registry.RegisterSection(new NavigationSection
        {
            Id = id,
            Title = title,
            Order = order
        });

        return registry;
    }

    /// <summary>
    /// Builder pattern for adding navigation items fluently.
    /// </summary>
    public static INavigationRegistry AddNavigationItem(
        this INavigationRegistry registry,
        Action<NavigationItemBuilder> configure)
    {
        var builder = new NavigationItemBuilder();
        configure(builder);
        registry.RegisterItem(builder.Build());
        return registry;
    }
}

/// <summary>
/// Builder for creating navigation items with a fluent API.
/// </summary>
public sealed class NavigationItemBuilder
{
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _href = string.Empty;
    private string _icon = string.Empty;
    private string _section = string.Empty;
    private int _order = 0;
    private string? _requiredPermission;
    private string[]? _requiredRoles;
    private bool _requiresRootTenantAdmin;
    private bool _isVisible = true;

    public NavigationItemBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public NavigationItemBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public NavigationItemBuilder WithHref(string href)
    {
        _href = href;
        return this;
    }

    public NavigationItemBuilder WithIcon(string icon)
    {
        _icon = icon;
        return this;
    }

    public NavigationItemBuilder InSection(string section)
    {
        _section = section;
        return this;
    }

    public NavigationItemBuilder WithOrder(int order)
    {
        _order = order;
        return this;
    }

    /// <summary>
    /// Require a specific permission to view this item.
    /// Use NavigationPermissions class for type-safe permission strings.
    /// </summary>
    public NavigationItemBuilder RequirePermission(string permission)
    {
        _requiredPermission = permission;
        return this;
    }

    public NavigationItemBuilder RequireRoles(params string[] roles)
    {
        _requiredRoles = roles;
        return this;
    }

    public NavigationItemBuilder RequireRootTenantAdmin(bool required = true)
    {
        _requiresRootTenantAdmin = required;
        return this;
    }

    public NavigationItemBuilder SetVisible(bool visible)
    {
        _isVisible = visible;
        return this;
    }

    public NavigationItem Build()
    {
        if (string.IsNullOrWhiteSpace(_id))
            throw new InvalidOperationException("Navigation item ID is required");
        if (string.IsNullOrWhiteSpace(_title))
            throw new InvalidOperationException("Navigation item Title is required");
        if (string.IsNullOrWhiteSpace(_href))
            throw new InvalidOperationException("Navigation item Href is required");
        if (string.IsNullOrWhiteSpace(_icon))
            throw new InvalidOperationException("Navigation item Icon is required");
        if (string.IsNullOrWhiteSpace(_section))
            throw new InvalidOperationException("Navigation item Section is required");

        return new NavigationItem
        {
            Id = _id,
            Title = _title,
            Href = _href,
            Icon = _icon,
            Section = _section,
            Order = _order,
            RequiredPermission = _requiredPermission,
            RequiredRoles = _requiredRoles,
            RequiresRootTenantAdmin = _requiresRootTenantAdmin,
            IsVisible = _isVisible
        };
    }
}
