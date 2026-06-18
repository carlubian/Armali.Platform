using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Clothes.Queries;

/// <summary>
/// Read-side queries for Clothes. Wave 1 exposes the module-owned category and colour
/// catalogs in their deterministic order; the colour read carries the canonical
/// colour value. Later waves add the paginated garment gallery and detail reads.
/// </summary>
internal sealed class ClothesReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<ClothingCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<ClothingCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new ClothingCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClothingColorResponse>> ListColorsAsync(CancellationToken cancellationToken)
    {
        return await database.Set<ClothingColor>()
            .AsNoTracking()
            .OrderBy(color => color.SortOrder)
            .ThenBy(color => color.Id)
            .Select(color => new ClothingColorResponse(color.Id, color.Name, color.ColorValue, color.SortOrder))
            .ToArrayAsync(cancellationToken);
    }
}
