using Segaris.Api.Composition;
using Segaris.Api.Modules.Inventory.Mutations;
using Segaris.Api.Modules.Inventory.Persistence;
using Segaris.Api.Modules.Inventory.Queries;
using Segaris.Api.Modules.Inventory.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Business module for stock-tracked items and supplier-specific replenishment
/// orders. Wave 0 registered the module and froze its public contracts; Wave 1 adds
/// the persistence model, the one-time category and location initialization, and the
/// module-owned category and location catalog read and administrator management
/// endpoints surfaced through Configuration. Later Waves add the item and order
/// read, mutation, attachment, and receive surfaces.
/// </summary>
internal sealed class InventoryModule : ISegarisModule
{
    public string Name => "Inventory";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, InventoryModelContributor>();
        services.AddScoped<InventorySeeder>();
        services.AddScoped<InventoryReadService>();
        services.AddScoped<InventoryCategoryManagementService>();
        services.AddScoped<InventoryLocationManagementService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapInventoryEndpoints();
    }
}
