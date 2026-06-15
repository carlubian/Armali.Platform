using Segaris.Api.Composition;
using Segaris.Api.Modules.Capex.Attention;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Capex.Mutations;
using Segaris.Api.Modules.Capex.Persistence;
using Segaris.Api.Modules.Capex.Queries;
using Segaris.Api.Modules.Capex.Seeding;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Business module recording atomic income and expense movements. It exposes the
/// read APIs (categories, paginated entries, and entry detail), the Wave 4 entry
/// mutation, deletion, and attachment endpoints, and registers the launcher
/// attention contributor.
/// </summary>
internal sealed class CapexModule : ISegarisModule
{
    public string Name => "Capex";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, CapexModelContributor>();
        services.AddScoped<CapexSeeder>();
        services.AddScoped<CapexCatalogValidator>();
        services.AddScoped<CapexReadService>();
        services.AddScoped<CapexEntryWriteService>();
        services.AddScoped<CapexCategoryManagementService>();
        services.AddScoped<ICatalogReferenceHandler>(provider => new CapexCatalogReferenceHandler(provider.GetRequiredService<SegarisDbContext>(), ConfigurationCatalogKind.Suppliers));
        services.AddScoped<ICatalogReferenceHandler>(provider => new CapexCatalogReferenceHandler(provider.GetRequiredService<SegarisDbContext>(), ConfigurationCatalogKind.CostCenters));
        services.AddScoped<ICatalogReferenceHandler>(provider => new CapexCatalogReferenceHandler(provider.GetRequiredService<SegarisDbContext>(), ConfigurationCatalogKind.Currencies));
        services.AddScoped<ILauncherAttentionContributor, CapexAttentionContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCapexEndpoints();
    }
}
