using FSH.BuildingBlocks.Blazor.UI.Extensions;
using FSH.BuildingBlocks.Blazor.UI.Navigation;
using FSH.Framework.Shared.Constants;
using MudBlazor;

namespace FSH.Playground.Blazor.Navigation;

/// <summary>
/// Configures the default navigation menu items for the Playground application.
/// This can be extended or replaced by modules.
/// </summary>
public static class PlaygroundNavigationConfiguration
{
    public static void ConfigureNavigation(INavigationRegistry registry)
    {
        // Register sections (including Home for proper handling)
        registry.AddStandardSection(NavigationSections.Home, "Home", 0);
        registry.AddStandardSection(NavigationSections.Administration, "Administration", 100);
        registry.AddStandardSection(NavigationSections.Communication, "Communication", 200);
        registry.AddStandardSection(NavigationSections.System, "System", 300);
        registry.AddStandardSection(NavigationSections.Settings, "Settings", 400);

        // Home (no section header displayed, but needs section for grouping)
        registry.AddNavigationItem(item => item
            .WithId("home")
            .WithTitle("Home")
            .WithHref("/")
            .WithIcon(Icons.Material.Outlined.Home)
            .InSection(NavigationSections.Home)
            .WithOrder(0));

        // Administration Section
        
        // Opción 1: Usar NavigationPermissions (type-safe, con IntelliSense)
        registry.AddNavigationItem(item => item
            .WithId("users")
            .WithTitle("Users")
            .WithHref("/users")
            .WithIcon(Icons.Material.Outlined.Person)
            .InSection(NavigationSections.Administration)
            .RequirePermission(NavigationPermissions.Users.View) // Type-safe!
            .WithOrder(10));

        // Opción 2: Usar string directo (funciona, pero sin type-safety)
        registry.AddNavigationItem(item => item
            .WithId("roles")
            .WithTitle("Roles")
            .WithHref("/roles")
            .WithIcon(Icons.Material.Outlined.AdminPanelSettings)
            .InSection(NavigationSections.Administration)
            .RequirePermission("Permissions.Roles.View") // String directo
            .WithOrder(20));

        // Opción 3: Construir dinámicamente usando FshPermission.NameFor
        registry.AddNavigationItem(item => item
            .WithId("user-roles")
            .WithTitle("User Roles")
            .WithHref("/user-roles")
            .WithIcon(Icons.Material.Outlined.PersonAdd)
            .InSection(NavigationSections.Administration)
            .RequirePermission(FshPermission.NameFor(ActionConstants.View, ResourceConstants.UserRoles))
            .WithOrder(25));

        registry.AddNavigationItem(item => item
            .WithId("tenants")
            .WithTitle("Tenants")
            .WithHref("/tenants")
            .WithIcon(Icons.Material.Outlined.CorporateFare)
            .InSection(NavigationSections.Administration)
            .RequireRootTenantAdmin()
            .WithOrder(30));

        registry.AddNavigationItem(item => item
            .WithId("tenant-settings")
            .WithTitle("Tenant Settings")
            .WithHref("/tenants/settings")
            .WithIcon(Icons.Material.Outlined.Tune)
            .InSection(NavigationSections.Administration)
            .RequireRootTenantAdmin()
            .WithOrder(40));

        registry.AddNavigationItem(item => item
            .WithId("audits")
            .WithTitle("Audit Logs")
            .WithHref("/audits")
            .WithIcon(Icons.Material.Outlined.History)
            .InSection(NavigationSections.Administration)
            .RequirePermission(NavigationPermissions.AuditTrails.View) // Type-safe
            .WithOrder(50));

        // Communication Section
        registry.AddNavigationItem(item => item
            .WithId("chat")
            .WithTitle("Chat")
            .WithHref("/chat")
            .WithIcon(Icons.Material.Outlined.Chat)
            .InSection(NavigationSections.Communication)
            .WithOrder(10));

        registry.AddNavigationItem(item => item
            .WithId("notifications")
            .WithTitle("Notifications")
            .WithHref("/notifications")
            .WithIcon(Icons.Material.Outlined.Notifications)
            .InSection(NavigationSections.Communication)
            .WithOrder(20));

        // System Section
        registry.AddNavigationItem(item => item
            .WithId("health")
            .WithTitle("Health")
            .WithHref("/health")
            .WithIcon(Icons.Material.Outlined.MonitorHeart)
            .InSection(NavigationSections.System)
            .WithOrder(10));

        registry.AddNavigationItem(item => item
            .WithId("logs")
            .WithTitle("Logs")
            .WithHref("/logs")
            .WithIcon(Icons.Material.Outlined.Terminal)
            .InSection(NavigationSections.System)
            .WithOrder(20));

        // Settings Section
        registry.AddNavigationItem(item => item
            .WithId("profile")
            .WithTitle("Account")
            .WithHref("/settings/profile")
            .WithIcon(Icons.Material.Outlined.ManageAccounts)
            .InSection(NavigationSections.Settings)
            .WithOrder(10));

        registry.AddNavigationItem(item => item
            .WithId("theme")
            .WithTitle("Theme")
            .WithHref("/settings/theme")
            .WithIcon(Icons.Material.Outlined.Palette)
            .InSection(NavigationSections.Settings)
            .WithOrder(20));

        registry.AddNavigationItem(item => item
            .WithId("security")
            .WithTitle("Security")
            .WithHref("/settings/security")
            .WithIcon(Icons.Material.Outlined.Security)
            .InSection(NavigationSections.Settings)
            .WithOrder(30));

        registry.AddNavigationItem(item => item
            .WithId("sessions")
            .WithTitle("Sessions")
            .WithHref("/sessions")
            .WithIcon(Icons.Material.Outlined.Devices)
            .InSection(NavigationSections.Settings)
            .WithOrder(40));

        registry.AddNavigationItem(item => item
            .WithId("about")
            .WithTitle("About")
            .WithHref("/about")
            .WithIcon(Icons.Material.Outlined.Info)
            .InSection(NavigationSections.Settings)
            .WithOrder(50));
    }
}
