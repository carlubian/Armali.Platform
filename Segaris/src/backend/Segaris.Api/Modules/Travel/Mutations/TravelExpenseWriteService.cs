using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Travel.Mutations;

internal sealed class TravelExpenseWriteService(
    SegarisDbContext database,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<int?> CreateAsync(
        int tripId,
        CreateTravelExpenseRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tripExists = await database.Set<TravelTrip>()
            .Where(TravelTripPolicies.MutableBy(actorId))
            .AnyAsync(trip => trip.Id == tripId, cancellationToken);
        if (!tripExists)
        {
            return null;
        }

        var values = await MapCreateAsync(request, cancellationToken);
        var expense = TravelExpense.Create(tripId, values, actorId, clock.UtcNow);
        await ValidateCatalogReferencesAsync(values, cancellationToken);

        database.Add(expense);
        await database.SaveChangesAsync(cancellationToken);
        return expense.Id;
    }

    public async Task<bool> UpdateAsync(
        int tripId,
        int expenseId,
        UpdateTravelExpenseRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expense = await database.Set<TravelExpense>()
            .Where(expense => expense.Id == expenseId && expense.TripId == tripId)
            .Where(expense => database.Set<TravelTrip>()
                .Where(TravelTripPolicies.MutableBy(actorId))
                .Any(trip => trip.Id == expense.TripId))
            .FirstOrDefaultAsync(cancellationToken);
        if (expense is null)
        {
            return false;
        }

        var values = MapUpdate(request);
        expense.Update(values, actorId, clock.UtcNow);
        await ValidateCatalogReferencesAsync(values, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int tripId,
        int expenseId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var expense = await database.Set<TravelExpense>()
            .Where(expense => expense.Id == expenseId && expense.TripId == tripId)
            .Where(expense => database.Set<TravelTrip>()
                .Where(TravelTripPolicies.MutableBy(actorId))
                .Any(trip => trip.Id == expense.TripId))
            .FirstOrDefaultAsync(cancellationToken);
        if (expense is null)
        {
            return false;
        }

        database.Remove(expense);
        await database.SaveChangesAsync(cancellationToken);

        var owner = TravelAttachments.ExpenseOwner(expenseId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    private async Task<TravelExpenseValues> MapCreateAsync(
        CreateTravelExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = request.ExpenseCategoryId > 0
            ? request.ExpenseCategoryId
            : await DefaultExpenseCategoryIdAsync(cancellationToken);
        var date = request.Date == default ? TravelDefaults.Today(clock.UtcNow) : request.Date;

        return new(
            categoryId,
            request.Description ?? string.Empty,
            date,
            request.Amount,
            request.CurrencyId,
            request.SupplierId,
            request.CostCenterId,
            request.Notes);
    }

    private static TravelExpenseValues MapUpdate(UpdateTravelExpenseRequest request) => new(
        request.ExpenseCategoryId,
        request.Description ?? string.Empty,
        request.Date,
        request.Amount,
        request.CurrencyId,
        request.SupplierId,
        request.CostCenterId,
        request.Notes);

    private async Task<int> DefaultExpenseCategoryIdAsync(CancellationToken cancellationToken)
    {
        var id = await database.Set<TravelExpenseCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => (int?)category.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id ?? throw new TravelValidationException(
            "An expense category is required.",
            TravelValidationReason.CatalogReference);
    }

    private async Task ValidateCatalogReferencesAsync(
        TravelExpenseValues values,
        CancellationToken cancellationToken)
    {
        if (!await database.Set<TravelExpenseCategory>()
            .AnyAsync(category => category.Id == values.ExpenseCategoryId, cancellationToken))
        {
            throw new TravelValidationException(
                "The selected expense category does not exist.",
                TravelValidationReason.CatalogReference);
        }

        if (!await database.Set<SegarisCurrency>()
            .AnyAsync(currency => currency.Id == values.CurrencyId, cancellationToken))
        {
            throw new TravelValidationException(
                "The selected currency does not exist.",
                TravelValidationReason.CatalogReference);
        }

        if (values.SupplierId is { } supplierId
            && !await database.Set<SegarisSupplier>()
                .AnyAsync(supplier => supplier.Id == supplierId, cancellationToken))
        {
            throw new TravelValidationException(
                "The selected supplier does not exist.",
                TravelValidationReason.CatalogReference);
        }

        if (values.CostCenterId is { } costCenterId
            && !await database.Set<SegarisCostCenter>()
                .AnyAsync(costCenter => costCenter.Id == costCenterId, cancellationToken))
        {
            throw new TravelValidationException(
                "The selected cost centre does not exist.",
                TravelValidationReason.CatalogReference);
        }
    }
}
