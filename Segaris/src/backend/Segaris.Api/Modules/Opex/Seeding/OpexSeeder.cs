using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex.Seeding;

/// <summary>
/// Inserts the frozen initial Opex categories through the shared one-time
/// <see cref="CatalogInitializer"/>. The category catalog is Opex-owned but shares
/// the single initialization table, so its initial values are applied only the
/// first time the unmarked, empty catalog is seen and are never restored once an
/// administrator customizes the catalog.
/// </summary>
internal sealed class OpexSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public Task SeedAsync(CancellationToken cancellationToken) =>
        initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.OpexCategories,
            ct => database.Set<OpexCategory>().AnyAsync(ct),
            (now, _) =>
            {
                var categories = OpexCategoryCatalog.Categories;
                for (var index = 0; index < categories.Count; index++)
                {
                    var seed = categories[index];
                    database.Add(new OpexCategory
                    {
                        Name = seed.Name,
                        NormalizedName = OpexCategoryNormalization.Normalize(seed.Name),
                        SortOrder = index,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                return Task.CompletedTask;
            },
            cancellationToken);
}
