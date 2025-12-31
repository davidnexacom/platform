using FSH.BuildingBlocks.Blazor.UI.Navigation;
using Microsoft.Extensions.DependencyInjection;
namespace FSH.Framework.Blazor.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHeroUI(this IServiceCollection services)
    {
        services.AddMudServices(options =>
        {
            options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            options.SnackbarConfiguration.ShowCloseIcon = true;
            options.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            options.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
        });

        services.AddMudPopoverService();
        services.AddScoped<FSH.Framework.Blazor.UI.Components.Feedback.Snackbar.FshSnackbar>();
        services.AddSingleton(FSH.Framework.Blazor.UI.Theme.FshTheme.Build());

        // Register navigation registry
        services.AddSingleton<INavigationRegistry, NavigationRegistry>();

        return services;
    }
}
