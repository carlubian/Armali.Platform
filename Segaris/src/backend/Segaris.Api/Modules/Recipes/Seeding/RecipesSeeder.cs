using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Recipes.Seeding;

/// <summary>
/// Inserts the frozen initial Recipes categories through the shared one-time
/// <see cref="CatalogInitializer"/>. The catalog is Recipes-owned but shares the
/// single initialization table, so the initial values are applied only the first time
/// the unmarked, empty catalog is seen and are never restored once an administrator
/// customizes the catalog.
/// </summary>
internal sealed class RecipesSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.RecipeCategories,
            ct => database.Set<RecipeCategory>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < RecipesCatalog.Categories.Count; index++)
                {
                    var seed = RecipesCatalog.Categories[index];
                    database.Add(new RecipeCategory
                    {
                        Name = seed.Name,
                        NormalizedName = RecipesCatalogNormalization.Normalize(seed.Name),
                        SortOrder = index,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                return Task.CompletedTask;
            },
            cancellationToken);
    }
}
