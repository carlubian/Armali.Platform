using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel.Queries;

/// <summary>
/// Read-side queries for Travel. Wave 1 exposes the module-owned trip-type and
/// expense-category catalogs in their deterministic order; later waves add the
/// paginated trip list, trip detail with per-currency totals, and the trip-scoped
/// expense reads.
/// </summary>
internal sealed class TravelReadService(SegarisDbContext database, IAttachmentService attachments)
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

        var attachmentResponses = (await attachments.ListByOwnerAsync(TravelAttachments.TripOwner(tripId), cancellationToken))
            .Select(ToAttachment)
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
            attachmentResponses,
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    public async Task<bool> TripAccessibleAsync(
        int tripId,
        UserId userId,
        CancellationToken cancellationToken) =>
        await database.Set<TravelTrip>()
            .AsNoTracking()
            .Where(TravelTripPolicies.AccessibleTo(userId))
            .AnyAsync(trip => trip.Id == tripId, cancellationToken);

    public async Task<bool> ExpenseAccessibleAsync(
        int tripId,
        int expenseId,
        UserId userId,
        CancellationToken cancellationToken) =>
        await database.Set<TravelExpense>()
            .AsNoTracking()
            .Where(expense => expense.Id == expenseId && expense.TripId == tripId)
            .Where(expense => database.Set<TravelTrip>()
                .Where(TravelTripPolicies.AccessibleTo(userId))
                .Any(trip => trip.Id == expense.TripId))
            .AnyAsync(cancellationToken);

    public async Task<PaginatedResponse<TravelExpenseSummaryResponse>?> ListExpensesAsync(
        int tripId,
        TravelExpenseFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await TripAccessibleAsync(tripId, userId, cancellationToken))
        {
            return null;
        }

        var expenses = ApplyExpenseFilters(
            database.Set<TravelExpense>().AsNoTracking().Where(expense => expense.TripId == tripId),
            filter);

        var totalCount = await expenses.CountAsync(cancellationToken);
        var page = await ProjectExpenseSummaries(ApplyExpenseSort(expenses, sort))
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<TravelExpenseSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<TravelExpenseResponse?> GetExpenseAsync(
        int tripId,
        int expenseId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<TravelExpense>()
            .AsNoTracking()
            .Where(expense => expense.Id == expenseId && expense.TripId == tripId)
            .Where(expense => database.Set<TravelTrip>()
                .Where(TravelTripPolicies.AccessibleTo(userId))
                .Any(trip => trip.Id == expense.TripId))
            .Select(expense => new ExpenseDetailRow(
                expense.Id,
                expense.ExpenseCategoryId,
                database.Set<TravelExpenseCategory>()
                    .Where(category => category.Id == expense.ExpenseCategoryId).Select(category => category.Name).First(),
                expense.Description,
                expense.Date,
                expense.Amount,
                expense.CurrencyId,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == expense.CurrencyId).Select(currency => currency.Code).First(),
                expense.SupplierId,
                expense.SupplierId == null
                    ? null
                    : database.Set<SegarisSupplier>()
                        .Where(supplier => supplier.Id == expense.SupplierId).Select(supplier => supplier.Name).FirstOrDefault(),
                expense.CostCenterId,
                expense.CostCenterId == null
                    ? null
                    : database.Set<SegarisCostCenter>()
                        .Where(costCenter => costCenter.Id == expense.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault(),
                expense.Notes,
                expense.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == expense.CreatedBy).Select(user => user.DisplayName).First(),
                expense.CreatedAt,
                expense.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == expense.UpdatedBy).Select(user => user.DisplayName).FirstOrDefault(),
                expense.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var attachmentResponses = (await attachments.ListByOwnerAsync(TravelAttachments.ExpenseOwner(expenseId), cancellationToken))
            .Select(ToAttachment)
            .ToArray();

        return new TravelExpenseResponse(
            row.Id,
            row.ExpenseCategoryId,
            row.ExpenseCategoryName,
            row.Description,
            row.Date,
            row.Amount,
            row.CurrencyId,
            row.CurrencyCode,
            row.SupplierId,
            row.SupplierName,
            row.CostCenterId,
            row.CostCenterName,
            row.Notes,
            attachmentResponses,
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

    private IQueryable<TravelExpense> ApplyExpenseFilters(
        IQueryable<TravelExpense> expenses,
        TravelExpenseFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            expenses = expenses.Where(expense =>
                EF.Functions.Like(expense.Description.ToLower(), pattern, "\\")
                || (expense.Notes != null && EF.Functions.Like(expense.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.CategoryId is { } categoryId)
        {
            expenses = expenses.Where(expense => expense.ExpenseCategoryId == categoryId);
        }

        if (filter.CurrencyId is { } currencyId)
        {
            expenses = expenses.Where(expense => expense.CurrencyId == currencyId);
        }

        if (filter.SupplierId is { } supplierId)
        {
            expenses = expenses.Where(expense => expense.SupplierId == supplierId);
        }

        if (filter.CostCenterId is { } costCenterId)
        {
            expenses = expenses.Where(expense => expense.CostCenterId == costCenterId);
        }

        return expenses;
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

    private IQueryable<TravelExpense> ApplyExpenseSort(IQueryable<TravelExpense> expenses, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<TravelExpense> ordered = sort.Field switch
        {
            TravelExpenseQuery.SortFields.Category => ascending
                ? expenses.OrderBy(expense => database.Set<TravelExpenseCategory>()
                    .Where(category => category.Id == expense.ExpenseCategoryId).Select(category => category.Name).First())
                : expenses.OrderByDescending(expense => database.Set<TravelExpenseCategory>()
                    .Where(category => category.Id == expense.ExpenseCategoryId).Select(category => category.Name).First()),
            TravelExpenseQuery.SortFields.Description => ascending
                ? expenses.OrderBy(expense => expense.Description)
                : expenses.OrderByDescending(expense => expense.Description),
            TravelExpenseQuery.SortFields.Amount => ascending
                ? expenses.OrderBy(expense => expense.Amount)
                : expenses.OrderByDescending(expense => expense.Amount),
            TravelExpenseQuery.SortFields.Currency => ascending
                ? expenses.OrderBy(expense => database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == expense.CurrencyId).Select(currency => currency.Code).First())
                : expenses.OrderByDescending(expense => database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == expense.CurrencyId).Select(currency => currency.Code).First()),
            TravelExpenseQuery.SortFields.Supplier => ascending
                ? expenses.OrderBy(expense => expense.SupplierId == null)
                    .ThenBy(expense => database.Set<SegarisSupplier>()
                        .Where(supplier => supplier.Id == expense.SupplierId).Select(supplier => supplier.Name).FirstOrDefault())
                : expenses.OrderBy(expense => expense.SupplierId == null)
                    .ThenByDescending(expense => database.Set<SegarisSupplier>()
                        .Where(supplier => supplier.Id == expense.SupplierId).Select(supplier => supplier.Name).FirstOrDefault()),
            TravelExpenseQuery.SortFields.CostCenter => ascending
                ? expenses.OrderBy(expense => expense.CostCenterId == null)
                    .ThenBy(expense => database.Set<SegarisCostCenter>()
                        .Where(costCenter => costCenter.Id == expense.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault())
                : expenses.OrderBy(expense => expense.CostCenterId == null)
                    .ThenByDescending(expense => database.Set<SegarisCostCenter>()
                        .Where(costCenter => costCenter.Id == expense.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault()),
            TravelExpenseQuery.SortFields.TieBreaker => ascending
                ? expenses.OrderBy(expense => expense.Id)
                : expenses.OrderByDescending(expense => expense.Id),
            _ => ascending
                ? expenses.OrderBy(expense => expense.Date)
                : expenses.OrderByDescending(expense => expense.Date),
        };

        return ascending ? ordered.ThenBy(expense => expense.Id) : ordered.ThenByDescending(expense => expense.Id);
    }

    private IQueryable<TravelExpenseSummaryResponse> ProjectExpenseSummaries(IQueryable<TravelExpense> expenses) =>
        expenses.Select(expense => new TravelExpenseSummaryResponse(
            expense.Id,
            expense.ExpenseCategoryId,
            database.Set<TravelExpenseCategory>()
                .Where(category => category.Id == expense.ExpenseCategoryId).Select(category => category.Name).First(),
            expense.Description,
            expense.Date,
            expense.Amount,
            expense.CurrencyId,
            database.Set<SegarisCurrency>()
                .Where(currency => currency.Id == expense.CurrencyId).Select(currency => currency.Code).First(),
            expense.SupplierId,
            expense.SupplierId == null
                ? null
                : database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == expense.SupplierId).Select(supplier => supplier.Name).FirstOrDefault(),
            expense.CostCenterId,
            expense.CostCenterId == null
                ? null
                : database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == expense.CostCenterId).Select(costCenter => costCenter.Name).FirstOrDefault()));

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static TravelAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt);

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

    private sealed record ExpenseDetailRow(
        int Id,
        int ExpenseCategoryId,
        string ExpenseCategoryName,
        string Description,
        DateOnly Date,
        decimal Amount,
        int CurrencyId,
        string CurrencyCode,
        int? SupplierId,
        string? SupplierName,
        int? CostCenterId,
        string? CostCenterName,
        string? Notes,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int? UpdatedById,
        string? UpdatedByName,
        DateTimeOffset? UpdatedAt);
}
