using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Capex.Seeding;

/// <summary>
/// Inserts the frozen initial Capex categories through the shared one-time
/// <see cref="CatalogInitializer"/>. The category catalog is Capex-owned but shares
/// the single initialization table, so its initial values are applied only the
/// first time the unmarked, empty catalog is seen and are never restored once an
/// administrator customizes the catalog.
/// </summary>
internal sealed class CapexSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public Task SeedAsync(CancellationToken cancellationToken) =>
        initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.CapexCategories,
            ct => database.Set<CapexCategory>().AnyAsync(ct),
            (now, _) =>
            {
                var categories = CapexCategoryCatalog.Categories;
                for (var index = 0; index < categories.Count; index++)
                {
                    var seed = categories[index];
                    database.Add(new CapexCategory
                    {
                        Name = seed.Name,
                        NormalizedName = CapexCategoryNormalization.Normalize(seed.Name),
                        SortOrder = index,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                return Task.CompletedTask;
            },
            cancellationToken);
}
