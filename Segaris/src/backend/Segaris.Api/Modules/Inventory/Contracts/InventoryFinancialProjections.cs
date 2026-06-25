using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Contracts;

internal sealed record InventoryFinancialProjection(
    string SourceId,
    string SourceModule,
    string SourceType,
    DateOnly AccountingDate,
    string MovementDirection,
    decimal Amount,
    string CurrencyCode,
    string? CategoryLabel,
    string? SupplierLabel,
    string? CostCenterLabel,
    string? ItemCategoryLabel,
    string? ItemLabel,
    string? DestinationLabel);

internal interface IInventoryFinancialProjectionProvider
{
    Task<IReadOnlyList<InventoryFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
