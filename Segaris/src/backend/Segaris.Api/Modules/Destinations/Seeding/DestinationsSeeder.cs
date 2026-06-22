using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Destinations.Seeding;

/// <summary>
/// Inserts the frozen initial Destinations categories through the shared one-time
/// <see cref="CatalogInitializer"/>. Both catalogues are Destinations-owned but share
/// the single initialization table, so the initial values are applied only the first
/// time each unmarked, empty catalogue is seen and are never restored once an
/// administrator customizes the catalogue.
/// </summary>
internal sealed class DestinationsSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.DestinationCategories,
            ct => database.Set<DestinationCategory>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < DestinationsCatalog.DestinationCategories.Count; index++)
                {
                    var seed = DestinationsCatalog.DestinationCategories[index];
                    database.Add(new DestinationCategory
                    {
                        Name = seed.Name,
                        NormalizedName = DestinationsCatalogNormalization.Normalize(seed.Name),
                        SortOrder = index,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.DestinationPlaceCategories,
            ct => database.Set<PlaceCategory>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < DestinationsCatalog.PlaceCategories.Count; index++)
                {
                    var seed = DestinationsCatalog.PlaceCategories[index];
                    database.Add(new PlaceCategory
                    {
                        Name = seed.Name,
                        NormalizedName = DestinationsCatalogNormalization.Normalize(seed.Name),
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
