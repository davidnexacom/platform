namespace FSH.BuildingBlocks.Blazor.UI.Navigation;

/// <summary>
/// Represents a navigation menu item that can be registered by modules.
/// </summary>
public sealed class NavigationItem
{
    /// <summary>
    /// Unique identifier for this navigation item.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display text for the navigation item.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// URL/route path for the navigation item.
    /// </summary>
    public required string Href { get; init; }

    /// <summary>
    /// MudBlazor icon for the navigation item.
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Section/group where this item belongs (e.g., "Administration", "Communication").
    /// </summary>
    public required string Section { get; init; }

    /// <summary>
    /// Display order within the section (lower numbers appear first).
    /// </summary>
    public int Order { get; init; } = 0;

    /// <summary>
    /// Optional permission required to see this menu item.
    /// </summary>
    public string? RequiredPermission { get; init; }

    /// <summary>
    /// Optional roles required to see this menu item.
    /// </summary>
    public string[]? RequiredRoles { get; init; }

    /// <summary>
    /// Whether this item should only be visible to root tenant admins.
    /// </summary>
    public bool RequiresRootTenantAdmin { get; init; }

    /// <summary>
    /// Whether this item is visible (can be toggled dynamically).
    /// </summary>
    public bool IsVisible { get; init; } = true;
}
