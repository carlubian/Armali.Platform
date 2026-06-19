using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Assets.Queries;

/// <summary>
/// Read surface for Assets. Wave 1 exposes only the module-owned category and
/// location catalog reads in deterministic sort order; later waves add the paginated
/// asset list, asset detail, and accessibility checks.
/// </summary>
internal sealed class AssetReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<AssetCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<AssetCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new AssetCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssetLocationResponse>> ListLocationsAsync(CancellationToken cancellationToken)
    {
        return await database.Set<AssetLocation>()
            .AsNoTracking()
            .OrderBy(location => location.SortOrder)
            .ThenBy(location => location.Id)
            .Select(location => new AssetLocationResponse(location.Id, location.Name, location.SortOrder))
            .ToArrayAsync(cancellationToken);
    }
}
