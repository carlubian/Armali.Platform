using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Wellness.Seeding;

/// <summary>
/// Optional development-only seed for a small starter task pool. Production can
/// leave the catalogue empty and let administrators manage it through Configuration.
/// </summary>
internal sealed class WellnessSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    private static readonly (string Name, WellnessCategory Category)[] Seeds =
    [
        ("Drink water", WellnessCategory.HealthAndBody),
        ("Move for ten minutes", WellnessCategory.HealthAndBody),
        ("Step outside for daylight", WellnessCategory.HealthAndBody),
        ("Plan sleep wind-down", WellnessCategory.MindAndSleep),
        ("Write a three-line reflection", WellnessCategory.MindAndSleep),
        ("Avoid screens before bed", WellnessCategory.MindAndSleep),
        ("Check in with someone", WellnessCategory.PeopleAndWork),
        ("Tidy one small work surface", WellnessCategory.PeopleAndWork),
        ("Close one loose loop", WellnessCategory.PeopleAndWork),
    ];

    public Task SeedAsync(CancellationToken cancellationToken) =>
        initializer.EnsureInitializedAsync(
            "wellness.tasks",
            ct => database.Set<WellnessTask>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < Seeds.Length; index++)
                {
                    var seed = Seeds[index];
                    database.Add(WellnessTask.Create(seed.Name, seed.Category, index, creatorId: null, now));
                }

                return Task.CompletedTask;
            },
            cancellationToken);
}
