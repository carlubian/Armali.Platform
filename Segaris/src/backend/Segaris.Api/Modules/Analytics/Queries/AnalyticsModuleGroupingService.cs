using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Analytics.Queries;

/// <summary>
/// Builds the grouped expense/income charts for single-module Analytics tabs that
/// group by category, supplier, and cost centre. Capex and Opex share identical
/// grouping behavior; they differ only in their stable chart identifiers and the
/// source-module code used to isolate their projections.
/// </summary>
internal sealed class AnalyticsModuleGroupingService(
    IEnumerable<IAnalyticsFinancialProjectionProvider> providers,
    ICurrencyExchangeRateProvider exchangeRates)
{
    public Task<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>> GetCapexAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        CancellationToken cancellationToken) =>
        GetGroupedViewAsync(query, viewer, AnalyticsSourceModules.Capex, CapexCharts, cancellationToken);

    public Task<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>> GetOpexAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        CancellationToken cancellationToken) =>
        GetGroupedViewAsync(query, viewer, AnalyticsSourceModules.Opex, OpexCharts, cancellationToken);

    private async Task<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>> GetGroupedViewAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        string sourceModule,
        IReadOnlyList<GroupedChartSpec> specs,
        CancellationToken cancellationToken)
    {
        var previousRange = query.PreviousYearRange();
        var selectedRange = query.SelectedYearRange();

        var projections = (await AnalyticsProjectionStream.LoadOrderedAsync(
                providers,
                previousRange.From,
                selectedRange.To,
                viewer,
                cancellationToken))
            .Where(projection => string.Equals(projection.SourceModule, sourceModule, StringComparison.Ordinal))
            .ToArray();

        var rates = await exchangeRates.ListCurrentExchangeRatesAsync(cancellationToken);
        var rateLookup = AnalyticsExchangeRates.BuildLookup(rates);
        var missingRates = AnalyticsExchangeRates.MissingCurrencyCodes(projections, rateLookup);

        if (missingRates.Count > 0)
        {
            return new AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>(
                query.SelectedYear,
                query.PreviousYear,
                specs
                    .Select(spec => new AnalyticsChartResponse<AnalyticsGroupedAmountPoint>(spec.ChartId, []))
                    .ToArray(),
                missingRates);
        }

        var charts = specs
            .Select(spec => BuildChart(spec, projections, rateLookup, query))
            .ToArray();

        return new AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>(
            query.SelectedYear,
            query.PreviousYear,
            charts,
            []);
    }

    private static AnalyticsChartResponse<AnalyticsGroupedAmountPoint> BuildChart(
        GroupedChartSpec spec,
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyDictionary<string, decimal?> rateLookup,
        AnalyticsYearQuery query)
    {
        var selected = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var previous = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var projection in projections)
        {
            if (!string.Equals(projection.MovementDirection, spec.Direction, StringComparison.Ordinal))
            {
                continue;
            }

            Dictionary<string, decimal>? bucket = projection.AccountingDate.Year == query.SelectedYear
                ? selected
                : projection.AccountingDate.Year == query.PreviousYear
                    ? previous
                    : null;
            if (bucket is null)
            {
                continue;
            }

            var label = Label(spec.Dimension(projection));
            var amount = AnalyticsExchangeRates.ToEur(projection, rateLookup);
            bucket[label] = bucket.GetValueOrDefault(label) + amount;
        }

        var points = selected.Keys
            .Union(previous.Keys, StringComparer.Ordinal)
            .Select(label => new AnalyticsGroupedAmountPoint(
                label,
                AnalyticsAmounts.RoundEur(selected.GetValueOrDefault(label)),
                AnalyticsAmounts.RoundEur(previous.GetValueOrDefault(label))))
            .OrderByDescending(point => point.SelectedYearAmountEur)
            .ThenByDescending(point => point.PreviousYearAmountEur)
            .ThenBy(point => point.Label, StringComparer.Ordinal)
            .ToArray();

        return new AnalyticsChartResponse<AnalyticsGroupedAmountPoint>(spec.ChartId, points);
    }

    private static string Label(string? value) =>
        string.IsNullOrWhiteSpace(value) ? AnalyticsLabels.Unassigned : value;

    private sealed record GroupedChartSpec(
        string ChartId,
        string Direction,
        Func<AnalyticsFinancialProjection, string?> Dimension);

    private static readonly IReadOnlyList<GroupedChartSpec> CapexCharts =
    [
        new(AnalyticsChartIds.CapexExpenseByCategory, AnalyticsMovementDirections.Expense, projection => projection.CategoryLabel),
        new(AnalyticsChartIds.CapexExpenseBySupplier, AnalyticsMovementDirections.Expense, projection => projection.SupplierLabel),
        new(AnalyticsChartIds.CapexExpenseByCostCenter, AnalyticsMovementDirections.Expense, projection => projection.CostCenterLabel),
        new(AnalyticsChartIds.CapexIncomeByCategory, AnalyticsMovementDirections.Income, projection => projection.CategoryLabel),
        new(AnalyticsChartIds.CapexIncomeBySupplier, AnalyticsMovementDirections.Income, projection => projection.SupplierLabel),
        new(AnalyticsChartIds.CapexIncomeByCostCenter, AnalyticsMovementDirections.Income, projection => projection.CostCenterLabel),
    ];

    private static readonly IReadOnlyList<GroupedChartSpec> OpexCharts =
    [
        new(AnalyticsChartIds.OpexExpenseByCategory, AnalyticsMovementDirections.Expense, projection => projection.CategoryLabel),
        new(AnalyticsChartIds.OpexExpenseBySupplier, AnalyticsMovementDirections.Expense, projection => projection.SupplierLabel),
        new(AnalyticsChartIds.OpexExpenseByCostCenter, AnalyticsMovementDirections.Expense, projection => projection.CostCenterLabel),
        new(AnalyticsChartIds.OpexIncomeByCategory, AnalyticsMovementDirections.Income, projection => projection.CategoryLabel),
        new(AnalyticsChartIds.OpexIncomeBySupplier, AnalyticsMovementDirections.Income, projection => projection.SupplierLabel),
        new(AnalyticsChartIds.OpexIncomeByCostCenter, AnalyticsMovementDirections.Income, projection => projection.CostCenterLabel),
    ];
}
