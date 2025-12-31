namespace FSH.BuildingBlocks.Blazor.UI.Navigation;

/// <summary>
/// Default implementation of INavigationRegistry.
/// Thread-safe singleton registry for navigation items.
/// </summary>
public sealed class NavigationRegistry : INavigationRegistry
{
    private readonly List<NavigationSection> _sections = [];
    private readonly List<NavigationItem> _items = [];
    private readonly object _lock = new();

    public void RegisterSection(NavigationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        lock (_lock)
        {
            if (!_sections.Any(s => s.Id == section.Id))
            {
                _sections.Add(section);
            }
        }
    }

    public void RegisterItem(NavigationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (_lock)
        {
            if (!_items.Any(i => i.Id == item.Id))
            {
                _items.Add(item);
            }
        }
    }

    public void RegisterItems(IEnumerable<NavigationItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            RegisterItem(item);
        }
    }

    public IReadOnlyList<NavigationSection> GetSections()
    {
        lock (_lock)
        {
            return _sections
                .Where(s => s.IsVisible)
                .OrderBy(s => s.Order)
                .ThenBy(s => s.Title)
                .ToList();
        }
    }

    public IReadOnlyList<NavigationItem> GetItemsForSection(string sectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);

        lock (_lock)
        {
            return _items
                .Where(i => i.Section == sectionId && i.IsVisible)
                .OrderBy(i => i.Order)
                .ThenBy(i => i.Title)
                .ToList();
        }
    }

    public IReadOnlyList<NavigationItem> GetAllItems()
    {
        lock (_lock)
        {
            return _items
                .Where(i => i.IsVisible)
                .OrderBy(i => i.Order)
                .ThenBy(i => i.Title)
                .ToList();
        }
    }
}
