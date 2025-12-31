using FSH.BuildingBlocks.Blazor.UI.Navigation;
using System.Reflection;

namespace FSH.BuildingBlocks.Blazor.UI.Modules;

/// <summary>
/// Loads and configures Blazor modules for navigation and other UI concerns.
/// </summary>
public static class BlazorModuleLoader
{
    /// <summary>
    /// Discover and configure navigation from all Blazor modules in the specified assemblies.
    /// </summary>
    public static void ConfigureModuleNavigation(INavigationRegistry registry, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var source = assemblies is { Length: > 0 }
            ? assemblies
            : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

        var moduleTypes = source
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                    return [];
                }
            })
            .Where(t => typeof(IBlazorModule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var moduleType in moduleTypes)
        {
            try
            {
                if (Activator.CreateInstance(moduleType) is IBlazorModule module)
                {
                    module.ConfigureNavigation(registry);
                }
            }
            catch (Exception ex)
            {
                // Log or handle module loading errors
                Console.WriteLine($"Failed to load Blazor module {moduleType.Name}: {ex.Message}");
            }
        }
    }
}
