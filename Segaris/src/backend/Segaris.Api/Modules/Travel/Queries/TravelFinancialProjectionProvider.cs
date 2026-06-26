using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel.Queries;

/// <summary>
/// Publishes accessible Travel expense spending for Analytics. Expenses contribute when
/// their parent trip is accessible to the viewer and not
/// <see cref="TravelTripStatus.Cancelled"/>; planned, ongoing, and completed trips all
/// contribute. The accounting date is the expense date and the amount is the expense
/// amount in the expense currency. Travel expenses are always outgoing, so every
/// projection is an <c>Expense</c>.
///
/// Each projection carries the expense category, optional supplier and cost centre, and
/// the parent trip's linked destination name. Destination names are resolved through the
/// Destinations read contract so private destinations are never disclosed, matching how
/// the Travel Calendar projection resolves them.
/// </summary>
internal sealed class TravelFinancialProjectionProvider(
    SegarisDbContext database,
    IDestinationReferenceReader destinationReferences) : ITravelFinancialProjectionProvider
{
    public async Task<IReadOnlyList<TravelFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var accessibleTrips = database.Set<TravelTrip>()
            .AsNoTracking()
            .Where(TravelTripPolicies.AccessibleTo(viewer))
            .Where(trip => trip.Status != TravelTripStatus.Cancelled);

        var rows = await database.Set<TravelExpense>()
            .AsNoTracking()
            .Where(expense => expense.Date >= from && expense.Date <= to)
            .Join(
                accessibleTrips,
                expense => expense.TripId,
                trip => trip.Id,
                (expense, trip) => new { expense, trip })
            .OrderBy(row => row.expense.Date)
            .ThenBy(row => row.trip.Id)
            .ThenBy(row => row.expense.Id)
            .Select(row => new ExpenseRow(
                row.expense.Id,
                row.expense.Date,
                row.expense.Amount,
                row.trip.DestinationId,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == row.expense.CurrencyId)
                    .Select(currency => currency.Code)
                    .First(),
                database.Set<TravelExpenseCategory>()
                    .Where(category => category.Id == row.expense.ExpenseCategoryId)
                    .Select(category => category.Name)
                    .First(),
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == row.expense.SupplierId)
                    .Select(supplier => supplier.Name)
                    .FirstOrDefault(),
                database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == row.expense.CostCenterId)
                    .Select(costCenter => costCenter.Name)
                    .FirstOrDefault()))
            .ToArrayAsync(cancellationToken);

        var destinationIds = rows
            .Where(row => row.DestinationId is not null)
            .Select(row => row.DestinationId!.Value)
            .Distinct()
            .ToArray();
        var destinations = destinationIds.Length == 0
            ? new Dictionary<int, DestinationReference>()
            : await destinationReferences.ResolveAccessibleAsync(destinationIds, viewer, cancellationToken);

        return rows
            .Select(row => new TravelFinancialProjection(
                $"travel:{row.ExpenseId}",
                "travel",
                "expense",
                row.AccountingDate,
                "Expense",
                row.Amount,
                row.CurrencyCode,
                row.CategoryLabel,
                row.SupplierLabel,
                row.CostCenterLabel,
                null,
                null,
                row.DestinationId is { } destinationId
                    && destinations.TryGetValue(destinationId, out var destination)
                        ? destination.Name
                        : null))
            .ToArray();
    }

    private sealed record ExpenseRow(
        int ExpenseId,
        DateOnly AccountingDate,
        decimal Amount,
        int? DestinationId,
        string CurrencyCode,
        string CategoryLabel,
        string? SupplierLabel,
        string? CostCenterLabel);
}
