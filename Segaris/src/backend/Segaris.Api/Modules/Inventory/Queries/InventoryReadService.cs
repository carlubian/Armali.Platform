using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Inventory.Queries;

/// <summary>
/// Read-side queries for Inventory. Wave 1 exposes the module-owned category and
/// location catalogs in their deterministic order; later Waves extend this service
/// with the paginated item and order reads.
/// </summary>
internal sealed class InventoryReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<InventoryCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<InventoryCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new InventoryCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryLocationResponse>> ListLocationsAsync(CancellationToken cancellationToken)
    {
        return await database.Set<InventoryLocation>()
            .AsNoTracking()
            .OrderBy(location => location.SortOrder)
            .ThenBy(location => location.Id)
            .Select(location => new InventoryLocationResponse(location.Id, location.Name, location.SortOrder))
            .ToArrayAsync(cancellationToken);
    }
}
