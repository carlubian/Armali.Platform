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

public sealed class AnalyticsInventoryTravelEndpointTests
{
    [Fact]
    public async Task Inventory_and_travel_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var inventory = await client.GetAsync("/api/analytics/inventory?year=2026", CancellationToken.None);
        using var travel = await client.GetAsync("/api/analytics/travel?year=2026", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, inventory.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, travel.StatusCode);
    }

    [Fact]
    public async Task Inventory_returns_grouped_average_and_top_charts()
    {
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IAnalyticsFinancialProjectionProvider>();
            services.RemoveAll<ICurrencyExchangeRateProvider>();
            services.AddSingleton<IAnalyticsFinancialProjectionProvider>(
                new FakeProjectionProvider(
                [
                    Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 60m, "EUR", "Supplier A", "Appliances", "Washer"),
                    Inventory("inventory:1:2", new DateOnly(2026, 1, 1), 40m, "EUR", "Supplier A", "Appliances", "Dryer"),
                    Inventory("inventory:2:1", new DateOnly(2025, 1, 1), 50m, "EUR", "Supplier A", "Appliances", "Washer"),
                    // A different module must not leak into the Inventory view.
                    Travel("travel:1", new DateOnly(2026, 1, 1), 999m, "EUR", "Flights", "Airline", "Vacation", "Madrid"),
                ]));
            services.AddSingleton<ICurrencyExchangeRateProvider>(
                new FakeExchangeRateProvider([new("EUR", 1m)]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<AnalyticsInventoryResponse>(
            "/api/analytics/inventory?year=2026",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal((2026, 2025), (response!.SelectedYear, response.PreviousYear));
        Assert.Empty(response.MissingExchangeRateCurrencyCodes);
        Assert.Equal(
            [AnalyticsChartIds.InventoryExpenseByItemCategory, AnalyticsChartIds.InventoryExpenseBySupplier],
            response.GroupedCharts.Select(chart => chart.ChartId));
        Assert.Equal(100m, Grouped(response, AnalyticsChartIds.InventoryExpenseBySupplier).Points.Single().SelectedYearAmountEur);
        Assert.Equal(50m, Grouped(response, AnalyticsChartIds.InventoryExpenseBySupplier).Points.Single().PreviousYearAmountEur);
        Assert.Equal(100m, response.AverageCharts.Single().Points.Single().SelectedYearAverageEur);
        Assert.Equal("Washer", Top(response, AnalyticsChartIds.InventoryTopItems).Points.First().Label);
        Assert.Equal(60m, Top(response, AnalyticsChartIds.InventoryTopItems).Points.First().SelectedYearPercent);
    }

    [Fact]
    public async Task Travel_returns_grouped_charts()
    {
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IAnalyticsFinancialProjectionProvider>();
            services.RemoveAll<ICurrencyExchangeRateProvider>();
            services.AddSingleton<IAnalyticsFinancialProjectionProvider>(
                new FakeProjectionProvider(
                [
                    Travel("travel:1", new DateOnly(2026, 5, 1), 75m, "USD", "Flights", "Airline", "Vacation", "Madrid"),
                    Travel("travel:2", new DateOnly(2026, 6, 1), 20m, "EUR", "Meals", null, null, null),
                ]));
            services.AddSingleton<ICurrencyExchangeRateProvider>(
                new FakeExchangeRateProvider([new("EUR", 1m), new("USD", 0.5m)]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>>(
            "/api/analytics/travel?year=2026",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(
            [
                AnalyticsChartIds.TravelExpenseByCategory,
                AnalyticsChartIds.TravelExpenseBySupplier,
                AnalyticsChartIds.TravelExpenseByCostCenter,
                AnalyticsChartIds.TravelExpenseByDestination,
            ],
            response!.Charts.Select(chart => chart.ChartId));
        Assert.Equal(37.5m, Grouped(response, AnalyticsChartIds.TravelExpenseBySupplier).Points.Single(point => point.Label == "Airline").SelectedYearAmountEur);
        Assert.Equal(20m, Grouped(response, AnalyticsChartIds.TravelExpenseBySupplier).Points.Single(point => point.Label == AnalyticsLabels.Unassigned).SelectedYearAmountEur);
        Assert.Single(Grouped(response, AnalyticsChartIds.TravelExpenseByDestination).Points);
    }

    [Fact]
    public async Task Inventory_reports_configuration_incomplete_for_missing_rate()
    {
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.RemoveAll<IAnalyticsFinancialProjectionProvider>();
            services.RemoveAll<ICurrencyExchangeRateProvider>();
            services.AddSingleton<IAnalyticsFinancialProjectionProvider>(
                new FakeProjectionProvider(
                [
                    Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 100m, "USD", "Supplier A", "Category", "Item"),
                ]));
            services.AddSingleton<ICurrencyExchangeRateProvider>(
                new FakeExchangeRateProvider([new("EUR", 1m)]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<AnalyticsInventoryResponse>(
            "/api/analytics/inventory?year=2026",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(["USD"], response!.MissingExchangeRateCurrencyCodes);
        Assert.All(response.GroupedCharts, chart => Assert.Empty(chart.Points));
        Assert.All(response.AverageCharts, chart => Assert.Empty(chart.Points));
        Assert.All(response.TopCharts, chart => Assert.Empty(chart.Points));
    }

    [Fact]
    public async Task Travel_rejects_invalid_year_with_stable_problem_code()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/analytics/travel?year=1999", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AnalyticsErrorCodes.YearInvalid.Value, problem.GetProperty("code").GetString());
    }

    private static AnalyticsChartResponse<AnalyticsGroupedAmountPoint> Grouped(
        AnalyticsInventoryResponse response,
        string chartId) =>
        response.GroupedCharts.Single(chart => chart.ChartId == chartId);

    private static AnalyticsChartResponse<AnalyticsGroupedAmountPoint> Grouped(
        AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>> response,
        string chartId) =>
        response.Charts.Single(chart => chart.ChartId == chartId);

    private static AnalyticsChartResponse<AnalyticsTopAmountPoint> Top(
        AnalyticsInventoryResponse response,
        string chartId) =>
        response.TopCharts.Single(chart => chart.ChartId == chartId);

    private static AnalyticsFinancialProjection Inventory(
        string id,
        DateOnly date,
        decimal amount,
        string currencyCode,
        string? supplier,
        string? itemCategory,
        string? item) =>
        new(id, "inventory", "orderLine", date, AnalyticsMovementDirections.Expense, amount, currencyCode, null, supplier, null, itemCategory, item, null);

    private static AnalyticsFinancialProjection Travel(
        string id,
        DateOnly date,
        decimal amount,
        string currencyCode,
        string? category,
        string? supplier,
        string? costCenter,
        string? destination) =>
        new(id, "travel", "expense", date, AnalyticsMovementDirections.Expense, amount, currencyCode, category, supplier, costCenter, null, null, destination);

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
