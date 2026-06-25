using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Analytics.Queries;

/// <summary>
/// Builds the single-module Analytics tabs from source-owned financial
/// projections. Capex, Opex, and Travel use grouped amount charts; Inventory
/// additionally calculates average order amounts and selected-year top lists.
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

    public async Task<AnalyticsInventoryResponse> GetInventoryAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var context = await LoadModuleContextAsync(query, viewer, AnalyticsSourceModules.Inventory, cancellationToken);

        if (context.MissingRates.Count > 0)
        {
            return new AnalyticsInventoryResponse(
                query.SelectedYear,
                query.PreviousYear,
                InventoryGroupedCharts
                    .Select(spec => new AnalyticsChartResponse<AnalyticsGroupedAmountPoint>(spec.ChartId, []))
                    .ToArray(),
                [new AnalyticsChartResponse<AnalyticsAverageAmountPoint>(AnalyticsChartIds.InventoryAverageOrderBySupplier, [])],
                InventoryTopCharts
                    .Select(spec => new AnalyticsChartResponse<AnalyticsTopAmountPoint>(spec.ChartId, []))
                    .ToArray(),
                context.MissingRates);
        }

        return new AnalyticsInventoryResponse(
            query.SelectedYear,
            query.PreviousYear,
            InventoryGroupedCharts
                .Select(spec => BuildGroupedChart(spec, context.Projections, context.RateLookup, query))
                .ToArray(),
            [BuildInventoryAverageOrderChart(context.Projections, context.RateLookup, query)],
            InventoryTopCharts
                .Select(spec => BuildTopChart(spec, context.Projections, context.RateLookup, query))
                .ToArray(),
            []);
    }

    public Task<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>> GetTravelAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        CancellationToken cancellationToken) =>
        GetGroupedViewAsync(query, viewer, AnalyticsSourceModules.Travel, TravelCharts, cancellationToken);

    private async Task<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>> GetGroupedViewAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        string sourceModule,
        IReadOnlyList<GroupedChartSpec> specs,
        CancellationToken cancellationToken)
    {
        var context = await LoadModuleContextAsync(query, viewer, sourceModule, cancellationToken);

        if (context.MissingRates.Count > 0)
        {
            return new AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>(
                query.SelectedYear,
                query.PreviousYear,
                specs
                    .Select(spec => new AnalyticsChartResponse<AnalyticsGroupedAmountPoint>(spec.ChartId, []))
                    .ToArray(),
                context.MissingRates);
        }

        var charts = specs
            .Select(spec => BuildGroupedChart(spec, context.Projections, context.RateLookup, query))
            .ToArray();

        return new AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>(
            query.SelectedYear,
            query.PreviousYear,
            charts,
            []);
    }

    private async Task<ModuleAggregationContext> LoadModuleContextAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        string sourceModule,
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

        return new ModuleAggregationContext(projections, rateLookup, missingRates);
    }

    private static AnalyticsChartResponse<AnalyticsGroupedAmountPoint> BuildGroupedChart(
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

            var rawLabel = spec.Dimension(projection);
            if (spec.ExcludeMissing && string.IsNullOrWhiteSpace(rawLabel))
            {
                continue;
            }

            var label = Label(rawLabel);
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

    private static AnalyticsChartResponse<AnalyticsAverageAmountPoint> BuildInventoryAverageOrderChart(
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyDictionary<string, decimal?> rateLookup,
        AnalyticsYearQuery query)
    {
        var selected = BuildOrderAverages(projections, rateLookup, query.SelectedYear);
        var previous = BuildOrderAverages(projections, rateLookup, query.PreviousYear);

        var points = selected.Keys
            .Union(previous.Keys, StringComparer.Ordinal)
            .Select(label => new AnalyticsAverageAmountPoint(
                label,
                AnalyticsAmounts.RoundEur(selected.GetValueOrDefault(label).Average),
                AnalyticsAmounts.RoundEur(previous.GetValueOrDefault(label).Average),
                selected.GetValueOrDefault(label).Count,
                previous.GetValueOrDefault(label).Count))
            .OrderByDescending(point => point.SelectedYearAverageEur)
            .ThenByDescending(point => point.PreviousYearAverageEur)
            .ThenBy(point => point.Label, StringComparer.Ordinal)
            .ToArray();

        return new AnalyticsChartResponse<AnalyticsAverageAmountPoint>(
            AnalyticsChartIds.InventoryAverageOrderBySupplier,
            points);
    }

    private static Dictionary<string, AverageBucket> BuildOrderAverages(
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyDictionary<string, decimal?> rateLookup,
        int year)
    {
        var orderTotals = projections
            .Where(projection => projection.AccountingDate.Year == year
                && string.Equals(projection.MovementDirection, AnalyticsMovementDirections.Expense, StringComparison.Ordinal))
            .GroupBy(
                projection => new InventoryOrderBucket(
                    Label(projection.SupplierLabel),
                    InventoryOrderKey(projection.SourceId)))
            .Select(group => new
            {
                group.Key.Supplier,
                Total = group.Sum(projection => AnalyticsExchangeRates.ToEur(projection, rateLookup)),
            });

        return orderTotals
            .GroupBy(order => order.Supplier, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var totals = group.Select(order => order.Total).ToArray();
                    return new AverageBucket(totals.Sum() / totals.Length, totals.Length);
                },
                StringComparer.Ordinal);
    }

    private static AnalyticsChartResponse<AnalyticsTopAmountPoint> BuildTopChart(
        TopChartSpec spec,
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyDictionary<string, decimal?> rateLookup,
        AnalyticsYearQuery query)
    {
        var selected = BuildTotals(projections, rateLookup, query.SelectedYear, spec.Dimension);
        var previous = BuildTotals(projections, rateLookup, query.PreviousYear, spec.Dimension);
        var selectedTotal = selected.Values.Sum();
        var previousTotal = previous.Values.Sum();

        var points = selected
            .Select(row => new AnalyticsTopAmountPoint(
                row.Key,
                AnalyticsAmounts.RoundEur(row.Value),
                AnalyticsAmounts.RoundEur(previous.GetValueOrDefault(row.Key)),
                Percentage(row.Value, selectedTotal),
                Percentage(previous.GetValueOrDefault(row.Key), previousTotal)))
            .OrderByDescending(point => point.SelectedYearAmountEur)
            .ThenByDescending(point => point.PreviousYearAmountEur)
            .ThenBy(point => point.Label, StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        return new AnalyticsChartResponse<AnalyticsTopAmountPoint>(spec.ChartId, points);
    }

    private static Dictionary<string, decimal> BuildTotals(
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyDictionary<string, decimal?> rateLookup,
        int year,
        Func<AnalyticsFinancialProjection, string?> dimension)
    {
        var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var projection in projections)
        {
            if (projection.AccountingDate.Year != year
                || !string.Equals(projection.MovementDirection, AnalyticsMovementDirections.Expense, StringComparison.Ordinal))
            {
                continue;
            }

            var label = Label(dimension(projection));
            totals[label] = totals.GetValueOrDefault(label) + AnalyticsExchangeRates.ToEur(projection, rateLookup);
        }

        return totals;
    }

    private static decimal Percentage(decimal amount, decimal total) =>
        total == 0m ? 0m : AnalyticsAmounts.RoundEur(amount / total * 100m);

    private static string InventoryOrderKey(string sourceId)
    {
        var lastSeparator = sourceId.LastIndexOf(':');
        return lastSeparator > 0 ? sourceId[..lastSeparator] : sourceId;
    }

    private static string Label(string? value) =>
        string.IsNullOrWhiteSpace(value) ? AnalyticsLabels.Unassigned : value;

    private sealed record ModuleAggregationContext(
        IReadOnlyList<AnalyticsFinancialProjection> Projections,
        IReadOnlyDictionary<string, decimal?> RateLookup,
        IReadOnlyList<string> MissingRates);

    private readonly record struct AverageBucket(decimal Average, int Count);

    private readonly record struct InventoryOrderBucket(string Supplier, string Order);

    private sealed record GroupedChartSpec(
        string ChartId,
        string Direction,
        Func<AnalyticsFinancialProjection, string?> Dimension,
        bool ExcludeMissing = false);

    private sealed record TopChartSpec(
        string ChartId,
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

    private static readonly IReadOnlyList<GroupedChartSpec> InventoryGroupedCharts =
    [
        new(AnalyticsChartIds.InventoryExpenseByItemCategory, AnalyticsMovementDirections.Expense, projection => projection.ItemCategoryLabel),
        new(AnalyticsChartIds.InventoryExpenseBySupplier, AnalyticsMovementDirections.Expense, projection => projection.SupplierLabel),
    ];

    private static readonly IReadOnlyList<TopChartSpec> InventoryTopCharts =
    [
        new(AnalyticsChartIds.InventoryTopItems, projection => projection.ItemLabel),
        new(AnalyticsChartIds.InventoryTopSuppliers, projection => projection.SupplierLabel),
    ];

    private static readonly IReadOnlyList<GroupedChartSpec> TravelCharts =
    [
        new(AnalyticsChartIds.TravelExpenseByCategory, AnalyticsMovementDirections.Expense, projection => projection.CategoryLabel),
        new(AnalyticsChartIds.TravelExpenseBySupplier, AnalyticsMovementDirections.Expense, projection => projection.SupplierLabel),
        new(AnalyticsChartIds.TravelExpenseByCostCenter, AnalyticsMovementDirections.Expense, projection => projection.CostCenterLabel),
        new(AnalyticsChartIds.TravelExpenseByDestination, AnalyticsMovementDirections.Expense, projection => projection.DestinationLabel, ExcludeMissing: true),
    ];
}
