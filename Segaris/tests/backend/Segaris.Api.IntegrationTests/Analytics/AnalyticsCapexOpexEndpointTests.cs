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

public sealed class AnalyticsCapexOpexEndpointTests
{
    [Fact]
    public async Task Capex_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/analytics/capex?year=2026", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Opex_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/analytics/opex?year=2026", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Capex_groups_expenses_and_incomes_with_year_over_year_values()
    {
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IAnalyticsFinancialProjectionProvider>();
            services.RemoveAll<ICurrencyExchangeRateProvider>();
            services.AddSingleton<IAnalyticsFinancialProjectionProvider>(
                new FakeProjectionProvider(
                [
                    Projection("capex:1", "capex", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 100m, "EUR", "Home", "IKEA", "House"),
                    Projection("capex:2", "capex", new DateOnly(2025, 1, 1), AnalyticsMovementDirections.Expense, 40m, "EUR", "Home", "IKEA", "House"),
                    Projection("capex:3", "capex", new DateOnly(2026, 2, 1), AnalyticsMovementDirections.Income, 200m, "EUR", "Salary", null, null),
                    // A different module must not leak into the Capex view.
                    Projection("opex:1", "opex", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 999m, "EUR", "Rent", "Landlord", "House"),
                ]));
            services.AddSingleton<ICurrencyExchangeRateProvider>(
                new FakeExchangeRateProvider([new("EUR", 1m)]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>>(
            "/api/analytics/capex?year=2026",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal((2026, 2025), (response!.SelectedYear, response.PreviousYear));
        Assert.Empty(response.MissingExchangeRateCurrencyCodes);

        var expenseByCategory = Chart(response, AnalyticsChartIds.CapexExpenseByCategory);
        var home = expenseByCategory.Points.Single(point => point.Label == "Home");
        Assert.Equal((100m, 40m), (home.SelectedYearAmountEur, home.PreviousYearAmountEur));
        Assert.DoesNotContain(expenseByCategory.Points, point => point.Label == "Rent");

        var incomeBySupplier = Chart(response, AnalyticsChartIds.CapexIncomeBySupplier);
        Assert.Equal(AnalyticsLabels.Unassigned, incomeBySupplier.Points.Single().Label);
        Assert.Equal(200m, incomeBySupplier.Points.Single().SelectedYearAmountEur);
    }

    [Fact]
    public async Task Opex_returns_grouped_charts()
    {
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IAnalyticsFinancialProjectionProvider>();
            services.RemoveAll<ICurrencyExchangeRateProvider>();
            services.AddSingleton<IAnalyticsFinancialProjectionProvider>(
                new FakeProjectionProvider(
                [
                    Projection("opex:1", "opex", new DateOnly(2026, 5, 1), AnalyticsMovementDirections.Expense, 75m, "USD", "Rent", "Landlord", "House"),
                ]));
            services.AddSingleton<ICurrencyExchangeRateProvider>(
                new FakeExchangeRateProvider([new("EUR", 1m), new("USD", 0.5m)]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>>(
            "/api/analytics/opex?year=2026",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(
            [
                AnalyticsChartIds.OpexExpenseByCategory,
                AnalyticsChartIds.OpexExpenseBySupplier,
                AnalyticsChartIds.OpexExpenseByCostCenter,
                AnalyticsChartIds.OpexIncomeByCategory,
                AnalyticsChartIds.OpexIncomeBySupplier,
                AnalyticsChartIds.OpexIncomeByCostCenter,
            ],
            response!.Charts.Select(chart => chart.ChartId));
        Assert.Equal(37.5m, Chart(response, AnalyticsChartIds.OpexExpenseBySupplier).Points.Single().SelectedYearAmountEur);
        Assert.Empty(Chart(response, AnalyticsChartIds.OpexIncomeByCategory).Points);
    }

    [Fact]
    public async Task Capex_reports_configuration_incomplete_for_missing_rate()
    {
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IAnalyticsFinancialProjectionProvider>();
            services.RemoveAll<ICurrencyExchangeRateProvider>();
            services.AddSingleton<IAnalyticsFinancialProjectionProvider>(
                new FakeProjectionProvider(
                [
                    Projection("capex:1", "capex", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 100m, "USD", "Home", "IKEA", "House"),
                ]));
            services.AddSingleton<ICurrencyExchangeRateProvider>(
                new FakeExchangeRateProvider([new("EUR", 1m)]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>>(
            "/api/analytics/capex?year=2026",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(["USD"], response!.MissingExchangeRateCurrencyCodes);
        Assert.All(response.Charts, chart => Assert.Empty(chart.Points));
    }

    [Fact]
    public async Task Capex_rejects_invalid_year_with_stable_problem_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/analytics/capex?year=1999", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AnalyticsErrorCodes.YearInvalid.Value, problem.GetProperty("code").GetString());
    }

    private static AnalyticsChartResponse<AnalyticsGroupedAmountPoint> Chart(
        AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>> response,
        string chartId) =>
        response.Charts.Single(chart => chart.ChartId == chartId);

    private static AnalyticsFinancialProjection Projection(
        string id,
        string sourceModule,
        DateOnly date,
        string direction,
        decimal amount,
        string currencyCode,
        string? category,
        string? supplier,
        string? costCenter) =>
        new(id, sourceModule, "test", date, direction, amount, currencyCode, category, supplier, costCenter, null, null, null);

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
