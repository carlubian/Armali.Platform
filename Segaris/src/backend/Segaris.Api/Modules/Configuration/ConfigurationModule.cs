using Segaris.Api.Composition;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Platform module that owns the shared Supplier, CostCenter, and Currency
/// catalogs consumed by business modules.
/// </summary>
internal sealed class ConfigurationModule : ISegarisModule
{
    public string Name => "Configuration";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, ConfigurationModelContributor>();
        services.AddScoped<ConfigurationSeeder>();
        services.AddScoped<IConfigurationCatalog, ConfigurationCatalogService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapConfigurationEndpoints();
    }
}
