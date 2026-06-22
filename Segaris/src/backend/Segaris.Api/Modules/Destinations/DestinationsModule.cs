using Segaris.Api.Composition;
using Segaris.Api.Modules.Destinations.Mutations;
using Segaris.Api.Modules.Destinations.Persistence;
using Segaris.Api.Modules.Destinations.Queries;
using Segaris.Api.Modules.Destinations.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Destinations;

/// <summary>
/// Business module for visited destinations and destination-scoped places. Wave 2
/// adds the destination read and mutation APIs on top of the persisted model and
/// category catalogues; later waves add place APIs, attachment operations, the Travel
/// destination reference, and the frontend surfaces. The module registers no launcher
/// attention contributor: its launcher card never requests attention.
/// </summary>
internal sealed class DestinationsModule : ISegarisModule
{
    public string Name => "Destinations";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, DestinationsModelContributor>();
        services.AddScoped<DestinationsSeeder>();
        services.AddScoped<DestinationsCatalogReadService>();
        services.AddScoped<DestinationsReadService>();
        services.AddScoped<DestinationWriteService>();
        services.AddScoped<DestinationCategoryManagementService>();
        services.AddScoped<PlaceCategoryManagementService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDestinationsEndpoints();
    }
}
