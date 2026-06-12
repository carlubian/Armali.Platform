using Segaris.Api.Modules.Identity;
using Segaris.Api.Platform;

namespace Segaris.Api.Composition;

internal static class SegarisModules
{
    private static readonly ISegarisModule[] RegisteredModules =
    [
        new PlatformModule(),
        new IdentityModule(),
    ];

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
