using Segaris.Api.Modules.Assets;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Clothes;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Destinations;
using Segaris.Api.Modules.Firebird;
using Segaris.Api.Modules.Health;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Inventory;
using Segaris.Api.Modules.Launcher;
using Segaris.Api.Modules.Maintenance;
using Segaris.Api.Modules.Mood;
using Segaris.Api.Modules.Opex;
using Segaris.Api.Modules.Processes;
using Segaris.Api.Modules.Projects;
using Segaris.Api.Modules.Recipes;
using Segaris.Api.Modules.Travel;
using Segaris.Api.Platform;

namespace Segaris.Api.Composition;

internal static class SegarisModules
{
    private static readonly ISegarisModule[] RegisteredModules =
    [
        new PlatformModule(),
        new IdentityModule(),
        new ConfigurationModule(),
        new CapexModule(),
        new OpexModule(),
        new InventoryModule(),
        new TravelModule(),
        new ClothesModule(),
        new AssetsModule(),
        new MoodModule(),
        new MaintenanceModule(),
        new ProjectsModule(),
        new ProcessesModule(),
        new FirebirdModule(),
        new RecipesModule(),
        new DestinationsModule(),
        new HealthModule(),
        new LauncherModule(),
    ];

    /// <summary>
    /// The registered module names in registration order. Exposed for tests that
    /// assert deterministic, duplicate-free module composition.
    /// </summary>
    public static IReadOnlyList<string> ModuleNames =>
        RegisteredModules.Select(module => module.Name).ToArray();

    public static IServiceCollection AddSegarisModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var duplicateModule = RegisteredModules
            .GroupBy(module => module.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateModule is not null)
        {
            throw new InvalidOperationException(
                $"The module name '{duplicateModule.Key}' is registered more than once.");
        }

        foreach (var module in RegisteredModules)
        {
            module.AddServices(services, configuration);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapSegarisModules(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in RegisteredModules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
