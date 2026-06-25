using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Analytics;

/// <summary>Stable machine-readable Analytics failures.</summary>
internal static class AnalyticsErrorCodes
{
    public static readonly ErrorCode YearInvalid = new("analytics.year.invalid");
    public static readonly ErrorCode TabUnsupported = new("analytics.tab.unsupported");
    public static readonly ErrorCode ChartUnsupported = new("analytics.chart.unsupported");
    public static readonly ErrorCode MissingExchangeRate = new("analytics.currency.exchange_rate_missing");
}
