using Segaris.Api.Composition;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Modules.Opex.Mutations;
using Segaris.Api.Modules.Opex.Persistence;
using Segaris.Api.Modules.Opex.Queries;
using Segaris.Api.Modules.Opex.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex;

/// <summary>
/// Business module for recurrent income and expenses. Wave 0 registered the
/// module and froze its public contracts; Wave 1 added the persistence model,
/// the one-time category initialization, and the category catalog read and
/// administrator management endpoints; Wave 2 added the contract read APIs;
/// Wave 3 added contract create, update, deletion, and contract-level attachment
/// management; and Wave 4 adds the subordinate occurrence read, mutation, and
/// occurrence-level attachment surfaces, all authorized through the parent
/// contract. Configuration reference migration follows in a later Wave.
/// </summary>
internal sealed class OpexModule : ISegarisModule
{
    public string Name => "Opex";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, OpexModelContributor>();
        services.AddScoped<OpexSeeder>();
        services.AddScoped<OpexReadService>();
        services.AddScoped<OpexCategoryManagementService>();
        services.AddScoped<OpexCatalogValidator>();
        services.AddScoped<OpexContractWriteService>();
        services.AddScoped<OpexOccurrenceWriteService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpexEndpoints();
    }
}
