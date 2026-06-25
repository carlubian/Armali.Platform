using Segaris.Api.Modules.Analytics;
using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Analytics.Queries;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class AnalyticsOverviewServiceTests
{
    private static readonly UserId Owner = new(1);
    private static readonly UserId Collaborator = new(2);

    [Fact]
    public async Task Overview_aggregates_selected_and_previous_year_months_and_totals()
    {
        var service = CreateService(
            [
                Projection("capex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 100m, "EUR"),
                Projection("opex:1", new DateOnly(2026, 1, 10), AnalyticsMovementDirections.Income, 250m, "USD"),
                Projection("travel:1", new DateOnly(2026, 2, 2), AnalyticsMovementDirections.Expense, 50m, "USD"),
                Projection("capex:previous", new DateOnly(2025, 1, 20), AnalyticsMovementDirections.Expense, 40m, "EUR"),
                Projection("opex:previous", new DateOnly(2025, 2, 20), AnalyticsMovementDirections.Income, 10m, "EUR"),
            ],
            [new("EUR", 1m), new("USD", 0.5m)]);

        var response = await service.GetOverviewAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Empty(response.MissingExchangeRateCurrencyCodes);
        Assert.Equal((2026, 2025), (response.SelectedYear, response.PreviousYear));
        Assert.Equal(125m, response.Totals.SelectedYearExpenseAmountEur);
        Assert.Equal(40m, response.Totals.PreviousYearExpenseAmountEur);
        Assert.Equal(125m, response.Totals.SelectedYearIncomeAmountEur);
        Assert.Equal(10m, response.Totals.PreviousYearIncomeAmountEur);
        Assert.Equal(0m, response.Totals.SelectedYearNetBalanceEur);
        Assert.Equal(-30m, response.Totals.PreviousYearNetBalanceEur);

        var expense = Chart(response, AnalyticsChartIds.OverviewMonthlyExpense);
        var income = Chart(response, AnalyticsChartIds.OverviewMonthlyIncome);
        var net = Chart(response, AnalyticsChartIds.OverviewMonthlyNetBalance);

        Assert.Equal(12, expense.Points.Count);
        Assert.Equal((100m, 40m), Amounts(expense, 1));
        Assert.Equal((25m, 0m), Amounts(expense, 2));
        Assert.Equal((125m, 0m), Amounts(income, 1));
        Assert.Equal((0m, 10m), Amounts(income, 2));
        Assert.Equal((25m, -40m), Amounts(net, 1));
        Assert.Equal((-25m, 10m), Amounts(net, 2));
    }

    [Fact]
    public async Task Overview_rounds_after_summing_normalized_amounts()
    {
        var service = CreateService(
            [
                Projection("a", new DateOnly(2026, 3, 1), AnalyticsMovementDirections.Expense, 0.015m, "EUR"),
                Projection("b", new DateOnly(2026, 3, 2), AnalyticsMovementDirections.Expense, 0.015m, "EUR"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetOverviewAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(0.03m, response.Totals.SelectedYearExpenseAmountEur);
        Assert.Equal((0.03m, 0m), Amounts(Chart(response, AnalyticsChartIds.OverviewMonthlyExpense), 3));
    }

    [Fact]
    public async Task Overview_returns_configuration_incomplete_without_partial_aggregation()
    {
        var service = CreateService(
            [
                Projection("eur", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 10m, "EUR"),
                Projection("usd", new DateOnly(2026, 1, 2), AnalyticsMovementDirections.Expense, 20m, "USD"),
                Projection("gbp", new DateOnly(2025, 1, 2), AnalyticsMovementDirections.Income, 30m, "GBP"),
            ],
            [new("EUR", 1m), new("USD", null), new("GBP", 0m)]);

        var response = await service.GetOverviewAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(["GBP", "USD"], response.MissingExchangeRateCurrencyCodes);
        Assert.Equal(0m, response.Totals.SelectedYearExpenseAmountEur);
        Assert.All(response.Charts.SelectMany(chart => chart.Points), point =>
        {
            Assert.Equal(0m, point.SelectedYearAmountEur);
            Assert.Equal(0m, point.PreviousYearAmountEur);
        });
    }

    [Fact]
    public async Task Overview_passes_viewer_to_source_providers()
    {
        var provider = new ViewerFilteringProvider(
            [
                (Owner, Projection("owner", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 10m, "EUR")),
                (Collaborator, Projection("collaborator", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 99m, "EUR")),
            ]);
        var service = new AnalyticsOverviewService([provider], new FakeExchangeRateProvider([new("EUR", 1m)]));

        var owner = await service.GetOverviewAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);
        var collaborator = await service.GetOverviewAsync(AnalyticsYearQuery.Create(2026), Collaborator, CancellationToken.None);

        Assert.Equal(10m, owner.Totals.SelectedYearExpenseAmountEur);
        Assert.Equal(99m, collaborator.Totals.SelectedYearExpenseAmountEur);
    }

    private static AnalyticsOverviewService CreateService(
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyList<CurrencyExchangeRateSnapshot> rates) =>
        new([new FakeProjectionProvider(projections)], new FakeExchangeRateProvider(rates));

    private static AnalyticsFinancialProjection Projection(
        string id,
        DateOnly date,
        string direction,
        decimal amount,
        string currencyCode) =>
        new(
            id,
            id.Split(':')[0],
            "test",
            date,
            direction,
            amount,
            currencyCode,
            null,
            null,
            null,
            null,
            null,
            null);

    private static AnalyticsChartResponse<AnalyticsMoneySeriesPoint> Chart(
        AnalyticsOverviewResponse response,
        string chartId) =>
        response.Charts.Single(chart => chart.ChartId == chartId);

    private static (decimal Selected, decimal Previous) Amounts(
        AnalyticsChartResponse<AnalyticsMoneySeriesPoint> chart,
        int month)
    {
        var point = chart.Points.Single(value => value.Month == month);
        return (point.SelectedYearAmountEur, point.PreviousYearAmountEur);
    }

    private sealed class FakeProjectionProvider(IReadOnlyList<AnalyticsFinancialProjection> projections)
        : IAnalyticsFinancialProjectionProvider
    {
        public Task<IReadOnlyList<AnalyticsFinancialProjection>> ListFinancialProjectionsAsync(
            DateOnly from,
            DateOnly to,
            UserId viewer,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AnalyticsFinancialProjection>>(
                projections
                    .Where(projection => projection.AccountingDate >= from && projection.AccountingDate <= to)
                    .ToArray());
    }

    private sealed class ViewerFilteringProvider(
        IReadOnlyList<(UserId Viewer, AnalyticsFinancialProjection Projection)> projections)
        : IAnalyticsFinancialProjectionProvider
    {
        public Task<IReadOnlyList<AnalyticsFinancialProjection>> ListFinancialProjectionsAsync(
            DateOnly from,
            DateOnly to,
            UserId viewer,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AnalyticsFinancialProjection>>(
                projections
                    .Where(row => row.Viewer == viewer)
                    .Select(row => row.Projection)
                    .Where(projection => projection.AccountingDate >= from && projection.AccountingDate <= to)
                    .ToArray());
    }

    private sealed class FakeExchangeRateProvider(IReadOnlyList<CurrencyExchangeRateSnapshot> rates)
        : ICurrencyExchangeRateProvider
    {
        public Task<IReadOnlyList<CurrencyExchangeRateSnapshot>> ListCurrentExchangeRatesAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(rates);
    }
}
