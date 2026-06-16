using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Inventory.Seeding;

/// <summary>
/// Inserts the frozen initial Inventory categories and locations through the shared
/// one-time <see cref="CatalogInitializer"/>. Both catalogs are Inventory-owned but
/// share the single initialization table, so their initial values are applied only
/// the first time the unmarked, empty catalog is seen and are never restored once an
/// administrator customizes the catalog.
/// </summary>
internal sealed class InventorySeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.InventoryCategories,
            ct => database.Set<InventoryCategory>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(InventoryCatalog.Categories, (sortOrder, seed) => new InventoryCategory
                {
                    Name = seed.Name,
                    NormalizedName = InventoryCatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.InventoryLocations,
            ct => database.Set<InventoryLocation>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(InventoryCatalog.Locations, (sortOrder, seed) => new InventoryLocation
                {
                    Name = seed.Name,
                    NormalizedName = InventoryCatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private void Seed<TEntity>(
        IReadOnlyList<InventoryCatalogSeed> seeds,
        Func<int, InventoryCatalogSeed, TEntity> factory)
        where TEntity : class
    {
        for (var index = 0; index < seeds.Count; index++)
        {
            database.Add(factory(index, seeds[index]));
        }
    }
}
