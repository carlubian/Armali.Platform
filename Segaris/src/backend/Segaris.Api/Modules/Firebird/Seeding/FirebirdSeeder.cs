using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Firebird.Seeding;

internal sealed class FirebirdSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.FirebirdPersonCategories,
            ct => database.Set<PersonCategory>().AnyAsync(ct),
            (now, _) =>
            {
                var seeds = FirebirdDefaults.InitialCategories;
                for (var index = 0; index < seeds.Count; index++)
                {
                    database.Add(new PersonCategory
                    {
                        Name = seeds[index],
                        NormalizedName = FirebirdCatalogNormalization.Normalize(seeds[index]),
                        SortOrder = index,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.FirebirdUsernamePlatforms,
            ct => database.Set<UsernamePlatform>().AnyAsync(ct),
            (now, _) =>
            {
                var seeds = FirebirdDefaults.InitialUsernamePlatforms;
                for (var index = 0; index < seeds.Count; index++)
                {
                    database.Add(new UsernamePlatform
                    {
                        Name = seeds[index],
                        NormalizedName = FirebirdCatalogNormalization.Normalize(seeds[index]),
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

