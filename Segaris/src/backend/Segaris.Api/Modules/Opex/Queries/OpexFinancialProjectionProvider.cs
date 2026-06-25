using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Opex.Queries;

internal sealed class OpexFinancialProjectionProvider(SegarisDbContext database)
    : IOpexFinancialProjectionProvider
{
    public async Task<IReadOnlyList<OpexFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var accessibleContracts = database.Set<OpexContract>()
            .AsNoTracking()
            .Where(OpexContractPolicies.AccessibleTo(viewer));

        var rows = await database.Set<OpexOccurrence>()
            .AsNoTracking()
            .Where(occurrence => occurrence.EffectiveDate >= from && occurrence.EffectiveDate <= to)
            .Join(
                accessibleContracts,
                occurrence => occurrence.ContractId,
                contract => contract.Id,
                (occurrence, contract) => new { occurrence, contract })
            .OrderBy(row => row.occurrence.EffectiveDate)
            .ThenBy(row => row.contract.Id)
            .ThenBy(row => row.occurrence.Id)
            .Select(row => new ProjectionRow(
                row.contract.Id,
                row.occurrence.Id,
                row.occurrence.EffectiveDate,
                row.contract.MovementType,
                row.occurrence.ActualAmount,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == row.contract.CurrencyId)
                    .Select(currency => currency.Code)
                    .First(),
                database.Set<OpexCategory>()
                    .Where(category => category.Id == row.contract.CategoryId)
                    .Select(category => category.Name)
                    .First(),
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == row.contract.SupplierId)
                    .Select(supplier => supplier.Name)
                    .FirstOrDefault(),
                database.Set<SegarisCostCenter>()
                    .Where(costCenter => costCenter.Id == row.contract.CostCenterId)
                    .Select(costCenter => costCenter.Name)
                    .FirstOrDefault(),
                null,
                null,
                null))
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(row => new OpexFinancialProjection(
                $"opex:{row.ContractId}:{row.OccurrenceId}",
                "opex",
                "occurrence",
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
        int ContractId,
        int OccurrenceId,
        DateOnly AccountingDate,
        OpexMovementType MovementType,
        decimal Amount,
        string CurrencyCode,
        string? CategoryLabel,
        string? SupplierLabel,
        string? CostCenterLabel,
        string? ItemCategoryLabel,
        string? ItemLabel,
        string? DestinationLabel);
}
