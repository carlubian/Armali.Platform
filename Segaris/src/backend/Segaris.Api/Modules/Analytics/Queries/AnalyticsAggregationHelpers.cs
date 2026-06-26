using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Analytics.Queries;

/// <summary>Shared monetary rounding rules for Analytics aggregation.</summary>
internal static class AnalyticsAmounts
{
    public const int EurScale = 2;

    public static decimal RoundEur(decimal value) =>
        decimal.Round(value, EurScale, MidpointRounding.AwayFromZero);

    public static int DecimalPlaces(decimal value)
    {
        var bits = decimal.GetBits(value);
        return (bits[3] >> 16) & 0x7F;
    }
}

/// <summary>
/// Shared current exchange-rate resolution and missing-rate detection used by
/// every Analytics view. Rates are read-time Configuration values; a currency is
/// considered usable only when it has a positive rate with at most eight decimal
/// places.
/// </summary>
internal static class AnalyticsExchangeRates
{
    public static IReadOnlyDictionary<string, decimal?> BuildLookup(
        IReadOnlyList<CurrencyExchangeRateSnapshot> rates) =>
        rates.ToDictionary(
            rate => rate.CurrencyCode,
            rate => rate.ExchangeRateToEur,
            StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> MissingCurrencyCodes(
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyDictionary<string, decimal?> rates) =>
        projections
            .Select(projection => projection.CurrencyCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Where(code => !rates.TryGetValue(code, out var rate)
                || rate is null
                || rate <= 0m
                || AnalyticsAmounts.DecimalPlaces(rate.Value) > 8)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public static decimal ToEur(
        AnalyticsFinancialProjection projection,
        IReadOnlyDictionary<string, decimal?> rates) =>
        projection.Amount * rates[projection.CurrencyCode]!.Value;
}

/// <summary>
/// Loads and deterministically orders financial projections from every registered
/// source provider for an inclusive accounting-date range.
/// </summary>
internal static class AnalyticsProjectionStream
{
    public static async Task<IReadOnlyList<AnalyticsFinancialProjection>> LoadOrderedAsync(
        IEnumerable<IAnalyticsFinancialProjectionProvider> providers,
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var rows = new List<AnalyticsFinancialProjection>();
        foreach (var provider in providers)
        {
            rows.AddRange(await provider.ListFinancialProjectionsAsync(from, to, viewer, cancellationToken));
        }

        return rows
            .OrderBy(row => row.AccountingDate)
            .ThenBy(row => row.SourceModule, StringComparer.Ordinal)
            .ThenBy(row => row.SourceType, StringComparer.Ordinal)
            .ThenBy(row => row.SourceId, StringComparer.Ordinal)
            .ToArray();
    }
}
