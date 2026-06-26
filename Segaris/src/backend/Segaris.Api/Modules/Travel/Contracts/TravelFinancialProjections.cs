using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel.Contracts;

internal sealed record TravelFinancialProjection(
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

internal interface ITravelFinancialProjectionProvider
{
    Task<IReadOnlyList<TravelFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
