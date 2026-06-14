using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Platform module that owns the shared Supplier, CostCenter, and Currency
/// catalogs consumed by the business modules. Wave 0 only registers the module
/// shell and freezes the public contracts; entities, seeding, the catalog
/// reader implementation, and the read-only endpoints are added in Wave 1.
/// </summary>
internal sealed class ConfigurationModule : ISegarisModule
{
    public string Name => "Configuration";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // Wave 1 registers the model contributor, deterministic seed, and the
        // IConfigurationCatalog implementation here.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Wave 1 maps the read-only catalog endpoints described by
        // ConfigurationApiRoutes here.
    }
}
