using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Business module recording atomic income and expense movements. Wave 0 only
/// registers the module shell and freezes the public contracts; the domain,
/// persistence, calculations, read APIs, mutations, attachments, and the
/// launcher attention contributor are added in Waves 2-4.
/// </summary>
internal sealed class CapexModule : ISegarisModule
{
    public string Name => "Capex";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // Wave 2 registers the model contributor and category seed; Wave 3
        // registers the launcher attention contributor here.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Waves 3-4 map the category, entry, and attachment endpoints described
        // by CapexApiRoutes here.
    }
}
