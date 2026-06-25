namespace Segaris.Api.Modules.Analytics.Contracts;

internal sealed record AnalyticsMoneySeriesPoint(
    int Month,
    decimal SelectedYearAmountEur,
    decimal PreviousYearAmountEur);

internal sealed record AnalyticsGroupedAmountPoint(
    string Label,
    decimal SelectedYearAmountEur,
    decimal PreviousYearAmountEur);

internal sealed record AnalyticsAverageAmountPoint(
    string Label,
    decimal SelectedYearAverageEur,
    decimal PreviousYearAverageEur,
    int SelectedYearCount,
    int PreviousYearCount);

internal sealed record AnalyticsTopAmountPoint(
    string Label,
    decimal SelectedYearAmountEur,
    decimal PreviousYearAmountEur,
    decimal SelectedYearPercent,
    decimal PreviousYearPercent);

internal sealed record AnalyticsChartResponse<TPoint>(
    string ChartId,
    IReadOnlyList<TPoint> Points);

internal sealed record AnalyticsOverviewTotals(
    decimal SelectedYearExpenseAmountEur,
    decimal PreviousYearExpenseAmountEur,
    decimal SelectedYearIncomeAmountEur,
    decimal PreviousYearIncomeAmountEur,
    decimal SelectedYearNetBalanceEur,
    decimal PreviousYearNetBalanceEur);

internal sealed record AnalyticsOverviewResponse(
    int SelectedYear,
    int PreviousYear,
    AnalyticsOverviewTotals Totals,
    IReadOnlyList<AnalyticsChartResponse<AnalyticsMoneySeriesPoint>> Charts,
    IReadOnlyList<string> MissingExchangeRateCurrencyCodes);

internal sealed record AnalyticsInventoryResponse(
    int SelectedYear,
    int PreviousYear,
    IReadOnlyList<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>> GroupedCharts,
    IReadOnlyList<AnalyticsChartResponse<AnalyticsAverageAmountPoint>> AverageCharts,
    IReadOnlyList<AnalyticsChartResponse<AnalyticsTopAmountPoint>> TopCharts,
    IReadOnlyList<string> MissingExchangeRateCurrencyCodes);

internal sealed record AnalyticsViewResponse<TChart>(
    int SelectedYear,
    int PreviousYear,
    IReadOnlyList<TChart> Charts,
    IReadOnlyList<string> MissingExchangeRateCurrencyCodes);
