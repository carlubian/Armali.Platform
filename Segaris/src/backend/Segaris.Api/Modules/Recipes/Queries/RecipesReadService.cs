using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Recipes.Queries;

/// <summary>
/// Read-side queries for Recipes. Wave 1 exposes the module-owned category catalog
/// in its deterministic order; later waves add the paginated, filtered, and sorted
/// recipe list, the recipe detail, and the weekly menu queries.
/// </summary>
internal sealed class RecipesReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<RecipeCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<RecipeCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new RecipeCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }
}
