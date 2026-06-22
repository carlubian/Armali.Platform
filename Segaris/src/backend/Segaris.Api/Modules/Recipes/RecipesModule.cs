using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Recipes;

/// <summary>
/// Business module for food recipes and weekly menus composed of recipes. Wave 0
/// registers the module and freezes the public contracts; later waves add
/// persistence, the module-owned category catalogue, recipe operations, the
/// ingredient-to-Inventory item reference, weekly menus, attachments, and the
/// Configuration and frontend integration. The module registers no launcher
/// attention contributor: its launcher card never requests attention.
/// </summary>
internal sealed class RecipesModule : ISegarisModule
{
    public string Name => "Recipes";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRecipesEndpoints();
    }
}
