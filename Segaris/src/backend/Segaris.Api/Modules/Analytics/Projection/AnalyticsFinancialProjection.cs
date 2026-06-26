using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Analytics.Projection;

internal sealed record AnalyticsFinancialProjection(
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

internal interface IAnalyticsFinancialProjectionProvider
{
    Task<IReadOnlyList<AnalyticsFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
