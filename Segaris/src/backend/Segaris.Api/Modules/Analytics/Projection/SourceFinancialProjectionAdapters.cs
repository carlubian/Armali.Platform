using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Analytics.Projection;

internal sealed class CapexAnalyticsFinancialProjectionAdapter(ICapexFinancialProjectionProvider provider)
    : IAnalyticsFinancialProjectionProvider
{
    public async Task<IReadOnlyList<AnalyticsFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken) =>
        (await provider.ListFinancialProjectionsAsync(from, to, viewer, cancellationToken))
            .Select(row => new AnalyticsFinancialProjection(
                row.SourceId,
                row.SourceModule,
                row.SourceType,
                row.AccountingDate,
                row.MovementDirection,
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

internal sealed class OpexAnalyticsFinancialProjectionAdapter(IOpexFinancialProjectionProvider provider)
    : IAnalyticsFinancialProjectionProvider
{
    public async Task<IReadOnlyList<AnalyticsFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken) =>
        (await provider.ListFinancialProjectionsAsync(from, to, viewer, cancellationToken))
            .Select(row => new AnalyticsFinancialProjection(
                row.SourceId,
                row.SourceModule,
                row.SourceType,
                row.AccountingDate,
                row.MovementDirection,
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

internal sealed class InventoryAnalyticsFinancialProjectionAdapter(IInventoryFinancialProjectionProvider provider)
    : IAnalyticsFinancialProjectionProvider
{
    public async Task<IReadOnlyList<AnalyticsFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken) =>
        (await provider.ListFinancialProjectionsAsync(from, to, viewer, cancellationToken))
            .Select(row => new AnalyticsFinancialProjection(
                row.SourceId,
                row.SourceModule,
                row.SourceType,
                row.AccountingDate,
                row.MovementDirection,
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

internal sealed class TravelAnalyticsFinancialProjectionAdapter(ITravelFinancialProjectionProvider provider)
    : IAnalyticsFinancialProjectionProvider
{
    public async Task<IReadOnlyList<AnalyticsFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken) =>
        (await provider.ListFinancialProjectionsAsync(from, to, viewer, cancellationToken))
            .Select(row => new AnalyticsFinancialProjection(
                row.SourceId,
                row.SourceModule,
                row.SourceType,
                row.AccountingDate,
                row.MovementDirection,
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
