using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Analytics.Queries;

internal sealed class AnalyticsOverviewService(
    IEnumerable<IAnalyticsFinancialProjectionProvider> providers,
    ICurrencyExchangeRateProvider exchangeRates)
{
    public async Task<AnalyticsOverviewResponse> GetOverviewAsync(
        AnalyticsYearQuery query,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var previousRange = query.PreviousYearRange();
        var selectedRange = query.SelectedYearRange();
        var projections = await AnalyticsProjectionStream.LoadOrderedAsync(
            providers,
            previousRange.From,
            selectedRange.To,
            viewer,
            cancellationToken);

        var rates = await exchangeRates.ListCurrentExchangeRatesAsync(cancellationToken);
        var rateLookup = AnalyticsExchangeRates.BuildLookup(rates);
        var missingRates = AnalyticsExchangeRates.MissingCurrencyCodes(projections, rateLookup);

        if (missingRates.Count > 0)
        {
            return EmptyResponse(query, missingRates);
        }

        var selectedExpenses = new decimal[13];
        var selectedIncome = new decimal[13];
        var previousExpenses = new decimal[13];
        var previousIncome = new decimal[13];

        foreach (var projection in projections)
        {
            var normalizedAmount = AnalyticsExchangeRates.ToEur(projection, rateLookup);
            if (projection.AccountingDate.Year == query.SelectedYear)
            {
                AddProjection(projection, normalizedAmount, selectedExpenses, selectedIncome);
            }
            else if (projection.AccountingDate.Year == query.PreviousYear)
            {
                AddProjection(projection, normalizedAmount, previousExpenses, previousIncome);
            }
        }

        return new AnalyticsOverviewResponse(
            query.SelectedYear,
            query.PreviousYear,
            new AnalyticsOverviewTotals(
                Round(selectedExpenses.Sum()),
                Round(previousExpenses.Sum()),
                Round(selectedIncome.Sum()),
                Round(previousIncome.Sum()),
                Round(selectedIncome.Sum() - selectedExpenses.Sum()),
                Round(previousIncome.Sum() - previousExpenses.Sum())),
            [
                CreateMonthlyChart(AnalyticsChartIds.OverviewMonthlyExpense, selectedExpenses, previousExpenses),
                CreateMonthlyChart(AnalyticsChartIds.OverviewMonthlyIncome, selectedIncome, previousIncome),
                CreateMonthlyChart(AnalyticsChartIds.OverviewMonthlyNetBalance, Net(selectedIncome, selectedExpenses), Net(previousIncome, previousExpenses)),
            ],
            []);
    }

    private static AnalyticsOverviewResponse EmptyResponse(
        AnalyticsYearQuery query,
        IReadOnlyList<string> missingRates)
    {
        var zeroes = new decimal[13];
        return new AnalyticsOverviewResponse(
            query.SelectedYear,
            query.PreviousYear,
            new AnalyticsOverviewTotals(0m, 0m, 0m, 0m, 0m, 0m),
            [
                CreateMonthlyChart(AnalyticsChartIds.OverviewMonthlyExpense, zeroes, zeroes),
                CreateMonthlyChart(AnalyticsChartIds.OverviewMonthlyIncome, zeroes, zeroes),
                CreateMonthlyChart(AnalyticsChartIds.OverviewMonthlyNetBalance, zeroes, zeroes),
            ],
            missingRates);
    }

    private static void AddProjection(
        AnalyticsFinancialProjection projection,
        decimal normalizedAmount,
        decimal[] expenses,
        decimal[] income)
    {
        if (string.Equals(projection.MovementDirection, AnalyticsMovementDirections.Expense, StringComparison.Ordinal))
        {
            expenses[projection.AccountingDate.Month] += normalizedAmount;
        }
        else if (string.Equals(projection.MovementDirection, AnalyticsMovementDirections.Income, StringComparison.Ordinal))
        {
            income[projection.AccountingDate.Month] += normalizedAmount;
        }
    }

    private static AnalyticsChartResponse<AnalyticsMoneySeriesPoint> CreateMonthlyChart(
        string chartId,
        decimal[] selectedYearAmounts,
        decimal[] previousYearAmounts) =>
        new(
            chartId,
            Enumerable.Range(1, 12)
                .Select(month => new AnalyticsMoneySeriesPoint(
                    month,
                    Round(selectedYearAmounts[month]),
                    Round(previousYearAmounts[month])))
                .ToArray());

    private static decimal[] Net(decimal[] income, decimal[] expenses)
    {
        var values = new decimal[13];
        for (var month = 1; month <= 12; month++)
        {
            values[month] = income[month] - expenses[month];
        }

        return values;
    }

    private static decimal Round(decimal value) => AnalyticsAmounts.RoundEur(value);
}
