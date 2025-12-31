using FSH.BuildingBlocks.Blazor.UI.Extensions;
using FSH.BuildingBlocks.Blazor.UI.Modules;
using FSH.BuildingBlocks.Blazor.UI.Navigation;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity;
using MudBlazor;

namespace FSH.Playground.Blazor.Modules.Identity;

/// <summary>
/// Example module that registers Identity-related navigation items.
/// This demonstrates how modules can contribute to the navigation menu.
/// </summary>
public class IdentityBlazorModule : IBlazorModule
{
    public void ConfigureNavigation(INavigationRegistry registry)
    {
        // This module adds Users and Roles to the Administration section
        // (These are already added in PlaygroundNavigationConfiguration, 
        // but this shows how a module would do it)

        registry.AddNavigationItem(item => item
            .WithId("module.identity.users")
            .WithTitle("Users (Module)")
            .WithHref("/users")
            .WithIcon(Icons.Material.Outlined.Person)
            .InSection(NavigationSections.Administration)
            .WithOrder(10));

        registry.AddNavigationItem(item => item
            .WithId("module.identity.roles")
            .WithTitle("Roles (Module)")
            .WithHref("/roles")
            //.RequireRoles(RoleConstants.Admin)
            .RequirePermission(IdentityPermissionConstants.Roles.View)
            .WithIcon(Icons.Material.Outlined.AdminPanelSettings)
            .InSection(NavigationSections.Administration)
            .WithOrder(20));
    }
}
