using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Business module for stock-tracked items and supplier-specific replenishment
/// orders. Wave 0 registers the module and freezes its public contracts;
/// persistence and endpoints are added by later Waves.
/// </summary>
internal sealed class InventoryModule : ISegarisModule
{
    public string Name => "Inventory";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
