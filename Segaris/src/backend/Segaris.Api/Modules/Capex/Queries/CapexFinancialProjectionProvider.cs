using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Capex.Queries;

internal sealed class CapexFinancialProjectionProvider(SegarisDbContext database)
    : ICapexFinancialProjectionProvider
{
    public async Task<IReadOnlyList<CapexFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var rows = await database.Set<CapexEntry>()
            .AsNoTracking()
            .Where(CapexEntryPolicies.AccessibleTo(viewer))
            .Where(entry => entry.Status == CapexEntryStatus.Completed)
            .Where(entry => entry.DueDate >= from && entry.DueDate <= to)
            .OrderBy(entry => entry.DueDate)
            .ThenBy(entry => entry.Id)
            .Select(entry => new ProjectionRow(
                entry.Id,
                entry.DueDate,
                entry.MovementType,
                entry.TotalAmount,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == entry.CurrencyId)
                    .Select(currency => currency.Code)
                    .First(),
                database.Set<CapexCategory>()
                    .Where(category => category.Id == entry.CategoryId)
                    .Select(category => category.Name)
                    .First(),
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == entry.SupplierId)
                    .Select(supplier => supplier.Name)
                    .FirstOrDefault(),
                database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == entry.CostCenterId)
                    .Select(costCenter => costCenter.Name)
                    .FirstOrDefault(),
                null,
                null,
                null))
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(row => new CapexFinancialProjection(
                $"capex:{row.Id}",
                "capex",
                "entry",
                row.AccountingDate,
                row.MovementType.ToString(),
                row.Amount,
                row.CurrencyCode,
                row.CategoryLabel,
                row.SupplierLabel,
                row.CostCenterLabel,
                row.ItemCategoryLabel,
                row.ItemLabel,
                row.DestinationLabel))
            .ToArray();
    }

    private sealed record ProjectionRow(
        int Id,
        DateOnly AccountingDate,
        CapexMovementType MovementType,
        decimal Amount,
        string CurrencyCode,
        string? CategoryLabel,
        string? SupplierLabel,
        string? CostCenterLabel,
        string? ItemCategoryLabel,
        string? ItemLabel,
        string? DestinationLabel);
}
