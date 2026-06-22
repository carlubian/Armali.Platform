using Segaris.Api.Composition;
using Segaris.Api.Modules.Recipes.Mutations;
using Segaris.Api.Modules.Recipes.Persistence;
using Segaris.Api.Modules.Recipes.Queries;
using Segaris.Api.Modules.Recipes.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Recipes;

/// <summary>
/// Business module for food recipes and weekly menus composed of recipes. Wave 0
/// registered the module and froze the public contracts; Wave 1 adds the persistence
/// model, the one-time category initialization, and the module-owned category catalog
/// read and administrator management endpoints surfaced through Configuration; later
/// waves add recipe operations, the ingredient-to-Inventory item reference, weekly
/// menus, attachments, and the Configuration and frontend integration. The module
/// registers no launcher attention contributor: its launcher card never requests
/// attention.
/// </summary>
internal sealed class RecipesModule : ISegarisModule
{
    public string Name => "Recipes";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, RecipesModelContributor>();
        services.AddScoped<RecipesSeeder>();
        services.AddScoped<RecipesReadService>();
        services.AddScoped<RecipesCategoryManagementService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRecipesEndpoints();
    }
}
