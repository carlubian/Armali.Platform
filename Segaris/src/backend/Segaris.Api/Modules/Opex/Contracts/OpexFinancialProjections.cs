using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Opex.Contracts;

internal sealed record OpexFinancialProjection(
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

internal interface IOpexFinancialProjectionProvider
{
    Task<IReadOnlyList<OpexFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
