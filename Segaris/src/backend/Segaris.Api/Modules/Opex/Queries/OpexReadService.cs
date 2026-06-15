using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Opex.Queries;

/// <summary>
/// Read-side queries for the Opex module. Wave 1 exposes the category catalog;
/// later Waves add the contract list, detail, and current-year aggregation.
/// </summary>
internal sealed class OpexReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<OpexCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<OpexCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new OpexCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }
}
