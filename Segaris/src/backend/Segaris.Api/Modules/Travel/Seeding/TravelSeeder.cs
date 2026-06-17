using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Travel.Seeding;

/// <summary>
/// Inserts the frozen initial Travel trip types and expense categories through the
/// shared one-time <see cref="CatalogInitializer"/>. Both catalogs are Travel-owned
/// but share the single initialization table, so their initial values are applied
/// only the first time the unmarked, empty catalog is seen and are never restored
/// once an administrator customizes the catalog.
/// </summary>
internal sealed class TravelSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.TravelTripTypes,
            ct => database.Set<TravelTripType>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(TravelCatalog.TripTypes, (sortOrder, seed) => new TravelTripType
                {
                    Name = seed.Name,
                    NormalizedName = TravelCatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.TravelExpenseCategories,
            ct => database.Set<TravelExpenseCategory>().AnyAsync(ct),
            (now, _) =>
            {
                Seed(TravelCatalog.ExpenseCategories, (sortOrder, seed) => new TravelExpenseCategory
                {
                    Name = seed.Name,
                    NormalizedName = TravelCatalogNormalization.Normalize(seed.Name),
                    SortOrder = sortOrder,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private void Seed<TEntity>(
        IReadOnlyList<TravelCatalogSeed> seeds,
        Func<int, TravelCatalogSeed, TEntity> factory)
        where TEntity : class
    {
        for (var index = 0; index < seeds.Count; index++)
        {
            database.Add(factory(index, seeds[index]));
        }
    }
}
