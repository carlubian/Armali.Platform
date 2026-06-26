using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Analytics;
using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Analytics;

public sealed class AnalyticsOverviewEndpointTests
{
    [Fact]
    public async Task Overview_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/analytics/overview?year=2026", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Overview_returns_aggregated_charts_from_registered_projection_providers()
    {
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IAnalyticsFinancialProjectionProvider>();
            services.RemoveAll<ICurrencyExchangeRateProvider>();
            services.AddSingleton<IAnalyticsFinancialProjectionProvider>(
                new FakeProjectionProvider(
                [
                    Projection("capex:1", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 10m, "EUR"),
                    Projection("opex:1", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Income, 20m, "USD"),
                    Projection("travel:1", new DateOnly(2025, 1, 1), AnalyticsMovementDirections.Expense, 5m, "USD"),
                ]));
            services.AddSingleton<ICurrencyExchangeRateProvider>(
                new FakeExchangeRateProvider([new("EUR", 1m), new("USD", 0.5m)]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<AnalyticsOverviewResponse>(
            "/api/analytics/overview?year=2026",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(2026, response!.SelectedYear);
        Assert.Equal(10m, response.Totals.SelectedYearExpenseAmountEur);
        Assert.Equal(10m, response.Totals.SelectedYearIncomeAmountEur);
        Assert.Equal(2.5m, response.Totals.PreviousYearExpenseAmountEur);
        Assert.Empty(response.MissingExchangeRateCurrencyCodes);
        Assert.Equal(
            [AnalyticsChartIds.OverviewMonthlyExpense, AnalyticsChartIds.OverviewMonthlyIncome, AnalyticsChartIds.OverviewMonthlyNetBalance],
            response.Charts.Select(chart => chart.ChartId));
    }

    [Fact]
    public async Task Overview_rejects_invalid_year_with_stable_problem_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/analytics/overview?year=1999", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AnalyticsErrorCodes.YearInvalid.Value, problem.GetProperty("code").GetString());
    }

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

    private sealed class FakeExchangeRateProvider(IReadOnlyList<CurrencyExchangeRateSnapshot> rates)
        : ICurrencyExchangeRateProvider
    {
        public Task<IReadOnlyList<CurrencyExchangeRateSnapshot>> ListCurrentExchangeRatesAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(rates);
    }
}
