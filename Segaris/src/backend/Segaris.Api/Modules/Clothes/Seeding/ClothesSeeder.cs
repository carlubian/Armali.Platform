using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Clothes.Seeding;

/// <summary>
/// Inserts the frozen initial Clothes categories and colours through the shared
/// one-time <see cref="CatalogInitializer"/>. Both catalogs are Clothes-owned but
/// share the single initialization table, so their initial values are applied only
/// the first time the unmarked, empty catalog is seen and are never restored once an
/// administrator customizes the catalog. Colour seeds carry their canonical
/// <c>#RRGGBB</c> colour value.
/// </summary>
internal sealed class ClothesSeeder(SegarisDbContext database, CatalogInitializer initializer)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.ClothingCategories,
            ct => database.Set<ClothingCategory>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < ClothesCatalog.Categories.Count; index++)
                {
                    var seed = ClothesCatalog.Categories[index];
                    database.Add(new ClothingCategory
                    {
                        Name = seed.Name,
                        NormalizedName = ClothesCatalogNormalization.Normalize(seed.Name),
                        SortOrder = index,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                return Task.CompletedTask;
            },
            cancellationToken);

        await initializer.EnsureInitializedAsync(
            ConfigurationInitializationKeys.ClothingColors,
            ct => database.Set<ClothingColor>().AnyAsync(ct),
            (now, _) =>
            {
                for (var index = 0; index < ClothesCatalog.Colors.Count; index++)
                {
                    var seed = ClothesCatalog.Colors[index];
                    database.Add(new ClothingColor
                    {
                        Name = seed.Name,
                        NormalizedName = ClothesCatalogNormalization.Normalize(seed.Name),
                        ColorValue = ClothesValidation.ValidateColorValue(seed.ColorValue),
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
