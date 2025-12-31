using FSH.BuildingBlocks.Blazor.UI.Navigation;

namespace FSH.BuildingBlocks.Blazor.UI.Modules;

/// <summary>
/// Interface for Blazor modules that need to register navigation items.
/// </summary>
public interface IBlazorModule
{
    /// <summary>
    /// Configure navigation items for this module.
    /// </summary>
    /// <param name="registry">The navigation registry to register items with.</param>
    void ConfigureNavigation(INavigationRegistry registry);
}
