using Segaris.Api.Composition;
using Segaris.Api.Modules.Inventory.Attention;
using Segaris.Api.Modules.Inventory.Mutations;
using Segaris.Api.Modules.Inventory.Persistence;
using Segaris.Api.Modules.Inventory.Queries;
using Segaris.Api.Modules.Inventory.Seeding;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Business module for stock-tracked items and supplier-specific replenishment
/// orders. Wave 0 registered the module and froze its public contracts; Wave 1 added
/// the persistence model, the one-time category and location initialization, and the
/// module-owned category and location catalog read and administrator management
/// endpoints surfaced through Configuration; Waves 2 and 3 add the item read,
/// mutation, attachment, quick stock-adjustment, and launcher attention surfaces;
/// Wave 4 adds the non-receive order read, mutation, and attachment surfaces. Wave
/// 5 adds explicit receive.
/// </summary>
internal sealed class InventoryModule : ISegarisModule
{
    public string Name => "Inventory";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, InventoryModelContributor>();
        services.AddScoped<InventorySeeder>();
        services.AddScoped<InventoryReadService>();
        services.AddScoped<InventoryItemWriteService>();
        services.AddScoped<InventoryOrderWriteService>();
        services.AddScoped<InventoryCategoryManagementService>();
        services.AddScoped<InventoryLocationManagementService>();
        services.AddScoped<ILauncherAttentionContributor, InventoryAttentionContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapInventoryEndpoints();
    }
}
