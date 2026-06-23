using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Health.Seeding;

/// <summary>
/// Inserts the frozen initial Health categories through the shared one-time
/// <see cref="CatalogInitializer"/>. Both catalogues are Health-owned but share the
/// single initialization table, so the initial values are applied only the first time
/// each unmarked, empty catalogue is seen and are never restored once an administrator
/// customizes the catalogue.
/// </summary>
internal sealed class HealthSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.HealthDiseaseCategories,
            ct => database.Set<DiseaseCategory>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < HealthCatalog.DiseaseCategories.Count; index++)
                {
                    var seed = HealthCatalog.DiseaseCategories[index];
                    database.Add(new DiseaseCategory
                    {
                        Name = seed.Name,
                        NormalizedName = HealthCatalogNormalization.Normalize(seed.Name),
                        SortOrder = index,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.HealthMedicineCategories,
            ct => database.Set<MedicineCategory>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < HealthCatalog.MedicineCategories.Count; index++)
                {
                    var seed = HealthCatalog.MedicineCategories[index];
                    database.Add(new MedicineCategory
                    {
                        Name = seed.Name,
                        NormalizedName = HealthCatalogNormalization.Normalize(seed.Name),
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
