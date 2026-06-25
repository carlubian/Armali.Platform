using Segaris.Api.Modules.Analytics;
using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Analytics.Queries;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class AnalyticsInventoryTravelServiceTests
{
    private static readonly UserId Owner = new(1);

    [Fact]
    public async Task Inventory_returns_grouped_average_and_top_charts()
    {
        var service = CreateService(
            [
                Inventory("inventory:1:1", new DateOnly(2026, 1, 5), 60m, "EUR", "Supplier A", "Appliances", "Washer"),
                Inventory("inventory:1:2", new DateOnly(2026, 1, 5), 40m, "EUR", "Supplier A", "Appliances", "Dryer"),
                Inventory("inventory:2:1", new DateOnly(2026, 2, 5), 50m, "EUR", "Supplier A", "Supplies", "Filters"),
                Inventory("inventory:3:1", new DateOnly(2025, 3, 5), 30m, "EUR", "Supplier A", "Supplies", "Washer"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetInventoryAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Empty(response.MissingExchangeRateCurrencyCodes);
        Assert.Equal(
            [AnalyticsChartIds.InventoryExpenseByItemCategory, AnalyticsChartIds.InventoryExpenseBySupplier],
            response.GroupedCharts.Select(chart => chart.ChartId));
        Assert.Equal(
            [AnalyticsChartIds.InventoryAverageOrderBySupplier],
            response.AverageCharts.Select(chart => chart.ChartId));
        Assert.Equal(
            [AnalyticsChartIds.InventoryTopItems, AnalyticsChartIds.InventoryTopSuppliers],
            response.TopCharts.Select(chart => chart.ChartId));

        var appliances = Point(Grouped(response, AnalyticsChartIds.InventoryExpenseByItemCategory), "Appliances");
        Assert.Equal((100m, 0m), (appliances.SelectedYearAmountEur, appliances.PreviousYearAmountEur));

        var supplierAverage = Assert.Single(response.AverageCharts.Single().Points);
        Assert.Equal("Supplier A", supplierAverage.Label);
        Assert.Equal((75m, 30m), (supplierAverage.SelectedYearAverageEur, supplierAverage.PreviousYearAverageEur));
        Assert.Equal((2, 1), (supplierAverage.SelectedYearCount, supplierAverage.PreviousYearCount));

        var washer = Point(Top(response, AnalyticsChartIds.InventoryTopItems), "Washer");
        Assert.Equal((60m, 30m), (washer.SelectedYearAmountEur, washer.PreviousYearAmountEur));
        Assert.Equal((40m, 100m), (washer.SelectedYearPercent, washer.PreviousYearPercent));
    }

    [Fact]
    public async Task Inventory_top_charts_keep_five_selected_year_entries_with_stable_tie_breakers()
    {
        var service = CreateService(
            [
                Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 100m, "EUR", "Supplier C", "Category", "C"),
                Inventory("inventory:2:1", new DateOnly(2026, 1, 1), 100m, "EUR", "Supplier A", "Category", "A"),
                Inventory("inventory:3:1", new DateOnly(2026, 1, 1), 80m, "EUR", "Supplier B", "Category", "B"),
                Inventory("inventory:4:1", new DateOnly(2026, 1, 1), 70m, "EUR", "Supplier D", "Category", "D"),
                Inventory("inventory:5:1", new DateOnly(2026, 1, 1), 60m, "EUR", "Supplier E", "Category", "E"),
                Inventory("inventory:6:1", new DateOnly(2026, 1, 1), 50m, "EUR", "Supplier F", "Category", "F"),
                Inventory("inventory:prev:1", new DateOnly(2025, 1, 1), 999m, "EUR", "Supplier F", "Category", "F"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetInventoryAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(
            ["A", "C", "B", "D", "E"],
            Top(response, AnalyticsChartIds.InventoryTopItems).Points.Select(point => point.Label));
        Assert.DoesNotContain(Top(response, AnalyticsChartIds.InventoryTopItems).Points, point => point.Label == "F");
    }

    [Fact]
    public async Task Inventory_normalizes_mixed_currencies_and_reports_missing_rates_without_partial_results()
    {
        var normalized = CreateService(
            [
                Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 100m, "USD", "Supplier A", "Category", "Item"),
                Inventory("inventory:2:1", new DateOnly(2026, 1, 1), 100m, "EUR", "Supplier A", "Category", "Other"),
            ],
            [new("EUR", 1m), new("USD", 0.5m)]);

        var normalizedResponse = await normalized.GetInventoryAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);
        Assert.Equal(150m, Point(Grouped(normalizedResponse, AnalyticsChartIds.InventoryExpenseBySupplier), "Supplier A").SelectedYearAmountEur);

        var incomplete = CreateService(
            [Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 100m, "USD", "Supplier A", "Category", "Item")],
            [new("EUR", 1m)]);

        var incompleteResponse = await incomplete.GetInventoryAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);
        Assert.Equal(["USD"], incompleteResponse.MissingExchangeRateCurrencyCodes);
        Assert.All(incompleteResponse.GroupedCharts, chart => Assert.Empty(chart.Points));
        Assert.All(incompleteResponse.AverageCharts, chart => Assert.Empty(chart.Points));
        Assert.All(incompleteResponse.TopCharts, chart => Assert.Empty(chart.Points));
    }

    [Fact]
    public async Task Travel_groups_expenses_and_excludes_missing_destinations_from_destination_chart()
    {
        var service = CreateService(
            [
                Travel("travel:1", new DateOnly(2026, 1, 1), 100m, "EUR", "Flights", "Airline", "Vacation", "Madrid"),
                Travel("travel:2", new DateOnly(2026, 2, 1), 50m, "EUR", "Meals", null, null, null),
                Travel("travel:3", new DateOnly(2025, 1, 1), 20m, "EUR", "Flights", "Airline", "Vacation", "Madrid"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetTravelAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(
            [
                AnalyticsChartIds.TravelExpenseByCategory,
                AnalyticsChartIds.TravelExpenseBySupplier,
                AnalyticsChartIds.TravelExpenseByCostCenter,
                AnalyticsChartIds.TravelExpenseByDestination,
            ],
            response.Charts.Select(chart => chart.ChartId));

        Assert.Equal((100m, 20m), Amounts(Point(Grouped(response, AnalyticsChartIds.TravelExpenseByCategory), "Flights")));
        Assert.Equal(50m, Point(Grouped(response, AnalyticsChartIds.TravelExpenseBySupplier), AnalyticsLabels.Unassigned).SelectedYearAmountEur);
        Assert.Single(Grouped(response, AnalyticsChartIds.TravelExpenseByDestination).Points);
        Assert.Equal("Madrid", Grouped(response, AnalyticsChartIds.TravelExpenseByDestination).Points.Single().Label);
    }

    [Fact]
    public async Task Travel_reports_configuration_incomplete_for_missing_rates()
    {
        var service = CreateService(
            [Travel("travel:1", new DateOnly(2026, 1, 1), 100m, "USD", "Flights", "Airline", "Vacation", "Madrid")],
            [new("EUR", 1m)]);

        var response = await service.GetTravelAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(["USD"], response.MissingExchangeRateCurrencyCodes);
        Assert.All(response.Charts, chart => Assert.Empty(chart.Points));
    }

    [Fact]
    public async Task Cross_module_groups_expenses_by_supplier_category_and_cost_centre()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 100m, "EUR", "Home", "IKEA", "House"),
                Capex("capex:2", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Income, 999m, "EUR", "Salary", "Employer", "House"),
                Opex("opex:1", new DateOnly(2025, 1, 1), AnalyticsMovementDirections.Expense, 50m, "EUR", "Home", "IKEA", "House"),
                Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 40m, "USD", "IKEA", "Home", "Chair"),
                Travel("travel:1", new DateOnly(2026, 1, 1), 30m, "EUR", "Flights", "Airline", "Vacation", "Madrid"),
            ],
            [new("EUR", 1m), new("USD", 0.5m)]);

        var response = await service.GetCrossModuleAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Empty(response.MissingExchangeRateCurrencyCodes);
        Assert.Equal(
            [
                AnalyticsChartIds.CrossModuleExpenseBySupplier,
                AnalyticsChartIds.CrossModuleExpenseByCategory,
                AnalyticsChartIds.CrossModuleExpenseByCostCenter,
            ],
            response.Charts.Select(chart => chart.ChartId));

        Assert.Equal((120m, 50m), Amounts(Point(Grouped(response, AnalyticsChartIds.CrossModuleExpenseBySupplier), "IKEA")));
        Assert.Equal((120m, 50m), Amounts(Point(Grouped(response, AnalyticsChartIds.CrossModuleExpenseByCategory), "Home")));
        Assert.Equal((100m, 50m), Amounts(Point(Grouped(response, AnalyticsChartIds.CrossModuleExpenseByCostCenter), "House")));
        Assert.DoesNotContain(Grouped(response, AnalyticsChartIds.CrossModuleExpenseBySupplier).Points, point => point.Label == "Employer");
    }

    [Fact]
    public async Task Cross_module_normalizes_category_labels_and_excludes_missing_dimensions()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 10m, "EUR", " Home  Supplies ", null, null),
                Opex("opex:1", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 20m, "EUR", "home supplies", "Vendor", "Operations"),
                Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 30m, "EUR", null, "HOME SUPPLIES", "Box"),
                Travel("travel:1", new DateOnly(2026, 1, 1), 40m, "EUR", null, "Airline", null, "Madrid"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetCrossModuleAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        var category = Assert.Single(Grouped(response, AnalyticsChartIds.CrossModuleExpenseByCategory).Points);
        Assert.Equal("Home Supplies", category.Label, ignoreCase: true);
        Assert.Equal(60m, category.SelectedYearAmountEur);

        Assert.Equal(40m, Point(Grouped(response, AnalyticsChartIds.CrossModuleExpenseBySupplier), AnalyticsLabels.Unassigned).SelectedYearAmountEur);
        Assert.Equal("Operations", Assert.Single(Grouped(response, AnalyticsChartIds.CrossModuleExpenseByCostCenter).Points).Label);
    }

    [Fact]
    public async Task Cross_module_reports_configuration_incomplete_for_missing_rates()
    {
        var service = CreateService(
            [Inventory("inventory:1:1", new DateOnly(2026, 1, 1), 100m, "USD", "Supplier A", "Category", "Item")],
            [new("EUR", 1m)]);

        var response = await service.GetCrossModuleAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(["USD"], response.MissingExchangeRateCurrencyCodes);
        Assert.All(response.Charts, chart => Assert.Empty(chart.Points));
    }

    [Fact]
    public async Task Cross_module_missing_income_rate_does_not_block_expense_charts()
    {
        var service = CreateService(
            [
                Capex("capex:expense", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 100m, "EUR", "Home", "IKEA", "House"),
                Capex("capex:income", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Income, 999m, "USD", "Salary", "Employer", "House"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetCrossModuleAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Empty(response.MissingExchangeRateCurrencyCodes);
        Assert.Equal(100m, Point(Grouped(response, AnalyticsChartIds.CrossModuleExpenseBySupplier), "IKEA").SelectedYearAmountEur);
    }

    private static AnalyticsModuleGroupingService CreateService(
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyList<CurrencyExchangeRateSnapshot> rates) =>
        new([new FakeProjectionProvider(projections)], new FakeExchangeRateProvider(rates));

    private static AnalyticsFinancialProjection Inventory(
        string id,
        DateOnly date,
        decimal amount,
        string currencyCode,
        string? supplier,
        string? itemCategory,
        string? item) =>
        new(id, "inventory", "orderLine", date, AnalyticsMovementDirections.Expense, amount, currencyCode, null, supplier, null, itemCategory, item, null);

    private static AnalyticsFinancialProjection Capex(
        string id,
        DateOnly date,
        string direction,
        decimal amount,
        string currencyCode,
        string? category,
        string? supplier,
        string? costCenter) =>
        Projection(id, "capex", date, direction, amount, currencyCode, category, supplier, costCenter);

    private static AnalyticsFinancialProjection Opex(
        string id,
        DateOnly date,
        string direction,
        decimal amount,
        string currencyCode,
        string? category,
        string? supplier,
        string? costCenter) =>
        Projection(id, "opex", date, direction, amount, currencyCode, category, supplier, costCenter);

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

    private static AnalyticsGroupedAmountPoint Point(
        AnalyticsChartResponse<AnalyticsGroupedAmountPoint> chart,
        string label) =>
        chart.Points.Single(point => point.Label == label);

    private static AnalyticsTopAmountPoint Point(
        AnalyticsChartResponse<AnalyticsTopAmountPoint> chart,
        string label) =>
        chart.Points.Single(point => point.Label == label);

    private static (decimal Selected, decimal Previous) Amounts(AnalyticsGroupedAmountPoint point) =>
        (point.SelectedYearAmountEur, point.PreviousYearAmountEur);

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
