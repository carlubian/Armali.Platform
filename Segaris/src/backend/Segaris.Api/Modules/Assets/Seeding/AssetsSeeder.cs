using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Assets.Seeding;

/// <summary>
/// Inserts the frozen initial Assets categories and locations through the shared
/// one-time <see cref="CatalogInitializer"/>. Both catalogs are Assets-owned but
/// share the single initialization table, so their initial values are applied only
/// the first time the unmarked, empty catalog is seen and are never restored once an
/// administrator customizes the catalog.
/// </summary>
internal sealed class AssetsSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.AssetCategories,
            ct => database.Set<AssetCategory>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(AssetCatalog.Categories, (sortOrder, seed) => new AssetCategory
                {
                    Name = seed.Name,
                    NormalizedName = AssetCatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.AssetLocations,
            ct => database.Set<AssetLocation>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(AssetCatalog.Locations, (sortOrder, seed) => new AssetLocation
                {
                    Name = seed.Name,
                    NormalizedName = AssetCatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private void Seed<TEntity>(
        IReadOnlyList<AssetCatalogSeed> seeds,
        Func<int, AssetCatalogSeed, TEntity> factory)
        where TEntity : class
    {
        for (var index = 0; index < seeds.Count; index++)
        {
            database.Add(factory(index, seeds[index]));
        }
    }
}
