namespace FSH.BuildingBlocks.Blazor.UI.Navigation;

/// <summary>
/// Service for managing navigation menu items.
/// Modules can register their navigation items through this service.
/// </summary>
public interface INavigationRegistry
{
    /// <summary>
    /// Register a navigation section.
    /// </summary>
    void RegisterSection(NavigationSection section);

    /// <summary>
    /// Register a navigation item.
    /// </summary>
    void RegisterItem(NavigationItem item);

    /// <summary>
    /// Register multiple navigation items.
    /// </summary>
    void RegisterItems(IEnumerable<NavigationItem> items);

    /// <summary>
    /// Get all registered sections ordered by their Order property.
    /// </summary>
    IReadOnlyList<NavigationSection> GetSections();

    /// <summary>
    /// Get all registered navigation items for a specific section.
    /// </summary>
    IReadOnlyList<NavigationItem> GetItemsForSection(string sectionId);

    /// <summary>
    /// Get all registered navigation items.
    /// </summary>
    IReadOnlyList<NavigationItem> GetAllItems();
}
