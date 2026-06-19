using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Maintenance.Seeding;

/// <summary>
/// Inserts the frozen initial <c>MaintenanceType</c> values through the shared
/// one-time <see cref="CatalogInitializer"/>. The catalogue is Maintenance-owned but
/// shares the single initialization table, so the initial values are applied only the
/// first time the unmarked, empty catalogue is seen and are never restored once an
/// administrator customizes the catalogue.
/// </summary>
internal sealed class MaintenanceSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.MaintenanceTypes,
            ct => database.Set<MaintenanceType>().AnyAsync(ct),
            (now, _) =>
            {
                var seeds = MaintenanceDefaults.InitialTypes;
                for (var index = 0; index < seeds.Count; index++)
                {
                    database.Add(new MaintenanceType
                    {
                        Name = seeds[index],
                        NormalizedName = MaintenanceCatalogNormalization.Normalize(seeds[index]),
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
