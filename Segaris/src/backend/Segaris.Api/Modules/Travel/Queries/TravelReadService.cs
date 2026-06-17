using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Travel.Queries;

/// <summary>
/// Read-side queries for Travel. Wave 1 exposes the module-owned trip-type and
/// expense-category catalogs in their deterministic order; later waves add the
/// paginated trip list, trip detail with per-currency totals, and the trip-scoped
/// expense reads.
/// </summary>
internal sealed class TravelReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<TravelTripTypeResponse>> ListTripTypesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<TravelTripType>()
            .AsNoTracking()
            .OrderBy(tripType => tripType.SortOrder)
            .ThenBy(tripType => tripType.Id)
            .Select(tripType => new TravelTripTypeResponse(tripType.Id, tripType.Name, tripType.SortOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TravelExpenseCategoryResponse>> ListExpenseCategoriesAsync(CancellationToken cancellationToken)
    {
        return await database.Set<TravelExpenseCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new TravelExpenseCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
    }
}
