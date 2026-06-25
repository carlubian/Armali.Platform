using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Configuration.Seeding;

/// <summary>
/// Inserts the frozen initial values for the shared Configuration catalogs through
/// the one-time <see cref="CatalogInitializer"/>. Initial values are applied only
/// the first time an unmarked, empty catalog is seen; customized or already-marked
/// catalogs are never repopulated.
/// </summary>
internal sealed class ConfigurationSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.Suppliers,
            ct => database.Set<SegarisSupplier>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(ConfigurationCatalog.Suppliers, (sortOrder, seed) => new SegarisSupplier
                {
                    Name = seed.Name,
                    NormalizedName = CatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.CostCenters,
            ct => database.Set<SegarisCostCenter>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(ConfigurationCatalog.CostCenters, (sortOrder, seed) => new SegarisCostCenter
                {
                    Name = seed.Name,
                    NormalizedName = CatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.Currencies,
            ct => database.Set<SegarisCurrency>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(ConfigurationCatalog.Currencies, (sortOrder, seed) => new SegarisCurrency
                {
                    Code = seed.Code,
                    NormalizedCode = CatalogNormalization.Normalize(seed.Code),
                    Name = seed.Name,
                    NormalizedName = CatalogNormalization.Normalize(seed.Name),
                    ExchangeRateToEur = seed.ExchangeRateToEur,
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private void Seed<TSeed, TEntity>(IReadOnlyList<TSeed> seeds, Func<int, TSeed, TEntity> factory)
        where TEntity : class
    {
        for (var index = 0; index < seeds.Count; index++)
        {
            database.Add(factory(index, seeds[index]));
        }
    }
}
