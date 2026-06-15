using Segaris.Api.Composition;
using Segaris.Api.Modules.Opex.Mutations;
using Segaris.Api.Modules.Opex.Persistence;
using Segaris.Api.Modules.Opex.Queries;
using Segaris.Api.Modules.Opex.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex;

/// <summary>
/// Business module for recurrent income and expenses. Wave 0 registered the
/// module and froze its public contracts; Wave 1 adds the persistence model,
/// the one-time category initialization, and the category catalog read and
/// administrator management endpoints. Contract, occurrence, and attachment
/// surfaces are added by later Waves.
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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpexEndpoints();
    }
}
