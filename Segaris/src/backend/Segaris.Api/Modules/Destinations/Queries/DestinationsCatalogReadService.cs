using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Destinations.Queries;

/// <summary>
/// Read-side queries for the Destinations-owned category catalogues. Both catalogues
/// are returned in deterministic catalogue order (sort order, then identifier).
/// </summary>
internal sealed class DestinationsCatalogReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<DestinationCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken) =>
        await database.Set<DestinationCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new DestinationCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<PlaceCategoryResponse>> ListPlaceCategoriesAsync(CancellationToken cancellationToken) =>
        await database.Set<PlaceCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new PlaceCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
}
