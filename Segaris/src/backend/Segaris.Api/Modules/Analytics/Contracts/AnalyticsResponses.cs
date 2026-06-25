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

internal sealed record AnalyticsViewResponse<TChart>(
    int SelectedYear,
    int PreviousYear,
    IReadOnlyList<TChart> Charts,
    IReadOnlyList<string> MissingExchangeRateCurrencyCodes);
