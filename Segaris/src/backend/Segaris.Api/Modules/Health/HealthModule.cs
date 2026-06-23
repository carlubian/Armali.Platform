using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Health;

/// <summary>
/// Business module for diseases, medicines, their symmetric association, medicine
/// attachments, and medicine-to-Inventory item references. Wave 0 registers the
/// shell and freezes public contracts; later waves add persistence and behavior.
/// Health contributes no launcher attention.
/// </summary>
internal sealed class HealthModule : ISegarisModule
{
    public string Name => "Health";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthEndpoints();
    }
}
