using Segaris.Api.Composition;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Modules.Opex.Mcp;
using Segaris.Api.Modules.Opex.Mutations;
using Segaris.Api.Modules.Opex.Persistence;
using Segaris.Api.Modules.Opex.Queries;
using Segaris.Api.Modules.Opex.Seeding;
using Segaris.Api.Platform.Mcp;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex;

/// <summary>
/// Business module for recurrent income and expenses. Wave 0 registered the
/// module and froze its public contracts; Wave 1 added the persistence model,
/// the one-time category initialization, and the category catalog read and
/// administrator management endpoints; Wave 2 added the contract read APIs;
/// Wave 3 added contract create, update, deletion, and contract-level attachment
/// management; Wave 4 adds the subordinate occurrence read, mutation, and
/// occurrence-level attachment surfaces, all authorized through the parent
/// contract; and Wave 5 registers the shared-catalog reference handlers that
/// allow Configuration to migrate supplier, cost-center, and currency references
/// across all Opex contracts and occurrences atomically.
/// </summary>
internal sealed class OpexModule : ISegarisModule
{
    public string Name => "Opex";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, OpexModelContributor>();
        services.AddScoped<OpexSeeder>();
        services.AddScoped<OpexReadService>();
        services.AddScoped<IOpexFinancialProjectionProvider, OpexFinancialProjectionProvider>();
        services.AddScoped<OpexCategoryManagementService>();
        services.AddScoped<OpexCatalogValidator>();
        services.AddScoped<OpexContractWriteService>();
        services.AddScoped<OpexOccurrenceWriteService>();
        services.AddSingleton<ISegarisMcpToolContributor, OpexMcpToolContributor>();
        services.AddScoped<ICatalogReferenceHandler>(provider => new OpexCatalogReferenceHandler(provider.GetRequiredService<SegarisDbContext>(), ConfigurationCatalogKind.Suppliers));
        services.AddScoped<ICatalogReferenceHandler>(provider => new OpexCatalogReferenceHandler(provider.GetRequiredService<SegarisDbContext>(), ConfigurationCatalogKind.CostCenters));
        services.AddScoped<ICatalogReferenceHandler>(provider => new OpexCatalogReferenceHandler(provider.GetRequiredService<SegarisDbContext>(), ConfigurationCatalogKind.Currencies));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpexEndpoints();
    }
}
