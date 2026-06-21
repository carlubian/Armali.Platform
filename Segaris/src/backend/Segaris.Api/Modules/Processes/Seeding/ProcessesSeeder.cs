using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Processes.Seeding;

/// <summary>
/// Inserts the frozen initial <c>ProcessCategory</c> values through the shared one-time
/// <see cref="CatalogInitializer"/>. The catalogue is Processes-owned but shares the single
/// initialization table, so the initial values are applied only the first time the
/// unmarked, empty catalogue is seen and are never restored once an administrator
/// customizes the catalogue.
/// </summary>
internal sealed class ProcessesSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.ProcessCategories,
            ct => database.Set<ProcessCategory>().AnyAsync(ct),
            (now, _) =>
            {
                var seeds = ProcessesDefaults.InitialCategories;
                for (var index = 0; index < seeds.Count; index++)
                {
                    database.Add(new ProcessCategory
                    {
                        Name = seeds[index],
                        NormalizedName = ProcessesCatalogNormalization.Normalize(seeds[index]),
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
