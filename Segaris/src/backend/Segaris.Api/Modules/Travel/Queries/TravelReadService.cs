using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

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

    public async Task<PaginatedResponse<TravelTripSummaryResponse>> ListTripsAsync(
        TravelTripFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var trips = ApplyFilters(
            database.Set<TravelTrip>().AsNoTracking().Where(TravelTripPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await trips.CountAsync(cancellationToken);

        var page = await ApplySort(trips, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(trip => new TravelTripSummaryResponse(
                trip.Id,
                trip.Name,
                trip.TripTypeId,
                database.Set<TravelTripType>()
                    .Where(tripType => tripType.Id == trip.TripTypeId).Select(tripType => tripType.Name).First(),
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                trip.Status.ToString(),
                trip.Visibility.ToString(),
                trip.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == trip.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<TravelTripSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<TravelTripResponse?> GetTripAsync(
        int tripId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<TravelTrip>()
            .AsNoTracking()
            .Where(TravelTripPolicies.AccessibleTo(userId))
            .Where(trip => trip.Id == tripId)
            .Select(trip => new TripDetailRow(
                trip.Id,
                trip.Name,
                trip.TripTypeId,
                database.Set<TravelTripType>()
                    .Where(tripType => tripType.Id == trip.TripTypeId).Select(tripType => tripType.Name).First(),
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                trip.Status,
                trip.Notes,
                trip.Visibility,
                trip.Itinerary
                    .OrderBy(entry => entry.Date)
                    .ThenBy(entry => entry.Time)
                    .ThenBy(entry => entry.SortOrder)
                    .ThenBy(entry => entry.Id)
                    .Select(entry => new TravelItineraryEntryResponse(
                        entry.Id,
                        entry.Date,
                        entry.Time,
                        entry.Title,
                        entry.Place,
                        entry.ReservationLocator,
                        entry.Note,
                        entry.SortOrder))
                    .ToList(),
                trip.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == trip.CreatedBy).Select(user => user.DisplayName).First(),
                trip.CreatedAt,
                trip.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == trip.UpdatedBy).Select(user => user.DisplayName).FirstOrDefault(),
                trip.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var totalRows = await database.Set<TravelExpense>()
            .AsNoTracking()
            .Where(expense => expense.TripId == tripId)
            .GroupBy(expense => expense.CurrencyId)
            .Select(group => new CurrencyTotalRow(group.Key, group.Sum(expense => expense.Amount)))
            .ToArrayAsync(cancellationToken);
        var currencyIds = totalRows.Select(total => total.CurrencyId).ToArray();
        var currencyCodes = await database.Set<SegarisCurrency>()
            .AsNoTracking()
            .Where(currency => currencyIds.Contains(currency.Id))
            .ToDictionaryAsync(currency => currency.Id, currency => currency.Code, cancellationToken);
        var totals = totalRows
            .Select(total => new TravelExpenseTotalResponse(
                total.CurrencyId,
                currencyCodes[total.CurrencyId],
                total.Amount))
            .OrderBy(total => total.CurrencyCode, StringComparer.Ordinal)
            .ThenBy(total => total.CurrencyId)
            .ToArray();

        return new TravelTripResponse(
            row.Id,
            row.Name,
            row.TripTypeId,
            row.TripTypeName,
            row.Destination,
            row.StartDate,
            row.EndDate,
            row.Status.ToString(),
            row.Notes,
            row.Visibility.ToString(),
            row.Itinerary,
            totals,
            [],
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    private IQueryable<TravelTrip> ApplyFilters(IQueryable<TravelTrip> trips, TravelTripFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            trips = trips.Where(trip =>
                EF.Functions.Like(trip.Name.ToLower(), pattern, "\\")
                || (trip.Destination != null && EF.Functions.Like(trip.Destination.ToLower(), pattern, "\\"))
                || (trip.Notes != null && EF.Functions.Like(trip.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.TripTypeId is { } tripTypeId)
        {
            trips = trips.Where(trip => trip.TripTypeId == tripTypeId);
        }

        if (filter.Status is { } status)
        {
            trips = trips.Where(trip => trip.Status == status);
        }

        if (filter.Visibility is { } visibility)
        {
            trips = trips.Where(trip => trip.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            trips = trips.Where(trip => trip.CreatedBy == creatorId);
        }

        return trips;
    }

    private IQueryable<TravelTrip> ApplySort(IQueryable<TravelTrip> trips, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<TravelTrip> ordered = sort.Field switch
        {
            TravelTripQuery.SortFields.Name => ascending
                ? trips.OrderBy(trip => trip.Name)
                : trips.OrderByDescending(trip => trip.Name),
            TravelTripQuery.SortFields.TripType => ascending
                ? trips.OrderBy(trip => database.Set<TravelTripType>()
                    .Where(tripType => tripType.Id == trip.TripTypeId).Select(tripType => tripType.Name).First())
                : trips.OrderByDescending(trip => database.Set<TravelTripType>()
                    .Where(tripType => tripType.Id == trip.TripTypeId).Select(tripType => tripType.Name).First()),
            TravelTripQuery.SortFields.Destination => ascending
                ? trips.OrderBy(trip => trip.Destination == null).ThenBy(trip => trip.Destination)
                : trips.OrderBy(trip => trip.Destination == null).ThenByDescending(trip => trip.Destination),
            TravelTripQuery.SortFields.StartDate => ascending
                ? trips.OrderBy(trip => trip.StartDate)
                : trips.OrderByDescending(trip => trip.StartDate),
            TravelTripQuery.SortFields.EndDate => ascending
                ? trips.OrderBy(trip => trip.EndDate)
                : trips.OrderByDescending(trip => trip.EndDate),
            TravelTripQuery.SortFields.Status => ascending
                ? trips.OrderBy(trip => trip.Status)
                : trips.OrderByDescending(trip => trip.Status),
            TravelTripQuery.SortFields.Visibility => ascending
                ? trips.OrderBy(trip => trip.Visibility)
                : trips.OrderByDescending(trip => trip.Visibility),
            TravelTripQuery.SortFields.TieBreaker => ascending
                ? trips.OrderBy(trip => trip.Id)
                : trips.OrderByDescending(trip => trip.Id),
            _ => ascending
                ? trips.OrderBy(trip => trip.StartDate)
                : trips.OrderByDescending(trip => trip.StartDate),
        };

        return ascending ? ordered.ThenBy(trip => trip.Id) : ordered.ThenByDescending(trip => trip.Id);
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record TripDetailRow(
        int Id,
        string Name,
        int TripTypeId,
        string TripTypeName,
        string? Destination,
        DateOnly StartDate,
        DateOnly EndDate,
        TravelTripStatus Status,
        string? Notes,
        RecordVisibility Visibility,
        IReadOnlyList<TravelItineraryEntryResponse> Itinerary,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int? UpdatedById,
        string? UpdatedByName,
        DateTimeOffset? UpdatedAt);

    private sealed record CurrencyTotalRow(int CurrencyId, decimal Amount);
}
