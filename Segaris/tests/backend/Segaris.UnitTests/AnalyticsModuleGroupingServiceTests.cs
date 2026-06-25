using Segaris.Api.Modules.Analytics;
using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Analytics.Queries;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class AnalyticsModuleGroupingServiceTests
{
    private static readonly UserId Owner = new(1);
    private static readonly UserId Collaborator = new(2);

    [Fact]
    public async Task Capex_groups_expense_and_income_by_dimension_for_both_years()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 100m, "EUR", category: "Home", supplier: "IKEA", costCenter: "House"),
                Capex("capex:2", new DateOnly(2026, 3, 5), AnalyticsMovementDirections.Expense, 50m, "EUR", category: "Home", supplier: "IKEA", costCenter: "House"),
                Capex("capex:3", new DateOnly(2026, 4, 1), AnalyticsMovementDirections.Income, 200m, "EUR", category: "Salary", supplier: "Employer", costCenter: "House"),
                Capex("capex:prev", new DateOnly(2025, 6, 1), AnalyticsMovementDirections.Expense, 40m, "EUR", category: "Home", supplier: "IKEA", costCenter: "House"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Empty(response.MissingExchangeRateCurrencyCodes);
        Assert.Equal((2026, 2025), (response.SelectedYear, response.PreviousYear));

        var expenseByCategory = Chart(response, AnalyticsChartIds.CapexExpenseByCategory);
        var home = Point(expenseByCategory, "Home");
        Assert.Equal((150m, 40m), (home.SelectedYearAmountEur, home.PreviousYearAmountEur));

        var incomeByCategory = Chart(response, AnalyticsChartIds.CapexIncomeByCategory);
        var salary = Point(incomeByCategory, "Salary");
        Assert.Equal((200m, 0m), (salary.SelectedYearAmountEur, salary.PreviousYearAmountEur));
        Assert.DoesNotContain(incomeByCategory.Points, point => point.Label == "Home");

        var expenseByCostCenter = Chart(response, AnalyticsChartIds.CapexExpenseByCostCenter);
        var house = Point(expenseByCostCenter, "House");
        Assert.Equal((150m, 40m), (house.SelectedYearAmountEur, house.PreviousYearAmountEur));
    }

    [Fact]
    public async Task Capex_returns_all_six_charts_in_stable_order()
    {
        var service = CreateService([], [new("EUR", 1m)]);

        var response = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(
            [
                AnalyticsChartIds.CapexExpenseByCategory,
                AnalyticsChartIds.CapexExpenseBySupplier,
                AnalyticsChartIds.CapexExpenseByCostCenter,
                AnalyticsChartIds.CapexIncomeByCategory,
                AnalyticsChartIds.CapexIncomeBySupplier,
                AnalyticsChartIds.CapexIncomeByCostCenter,
            ],
            response.Charts.Select(chart => chart.ChartId));
        Assert.All(response.Charts, chart => Assert.Empty(chart.Points));
    }

    [Fact]
    public async Task Opex_returns_all_six_charts_in_stable_order()
    {
        var service = CreateService([], [new("EUR", 1m)]);

        var response = await service.GetOpexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(
            [
                AnalyticsChartIds.OpexExpenseByCategory,
                AnalyticsChartIds.OpexExpenseBySupplier,
                AnalyticsChartIds.OpexExpenseByCostCenter,
                AnalyticsChartIds.OpexIncomeByCategory,
                AnalyticsChartIds.OpexIncomeBySupplier,
                AnalyticsChartIds.OpexIncomeByCostCenter,
            ],
            response.Charts.Select(chart => chart.ChartId));
    }

    [Fact]
    public async Task Missing_optional_supplier_and_cost_centre_fall_back_to_unassigned()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 100m, "EUR", category: "Home", supplier: null, costCenter: null),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        var supplier = Single(Chart(response, AnalyticsChartIds.CapexExpenseBySupplier));
        var costCenter = Single(Chart(response, AnalyticsChartIds.CapexExpenseByCostCenter));
        Assert.Equal(AnalyticsLabels.Unassigned, supplier.Label);
        Assert.Equal(AnalyticsLabels.Unassigned, costCenter.Label);
        Assert.Equal(100m, supplier.SelectedYearAmountEur);
    }

    [Fact]
    public async Task Points_are_ordered_by_selected_year_amount_descending()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 30m, "EUR", category: "Low", supplier: "S", costCenter: "C"),
                Capex("capex:2", new DateOnly(2026, 1, 6), AnalyticsMovementDirections.Expense, 90m, "EUR", category: "High", supplier: "S", costCenter: "C"),
                Capex("capex:3", new DateOnly(2026, 1, 7), AnalyticsMovementDirections.Expense, 60m, "EUR", category: "Mid", supplier: "S", costCenter: "C"),
            ],
            [new("EUR", 1m)]);

        var response = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(
            ["High", "Mid", "Low"],
            Chart(response, AnalyticsChartIds.CapexExpenseByCategory).Points.Select(point => point.Label));
    }

    [Fact]
    public async Task Mixed_currencies_are_normalized_to_eur()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 100m, "USD", category: "Home", supplier: "S", costCenter: "C"),
                Capex("capex:2", new DateOnly(2026, 1, 6), AnalyticsMovementDirections.Expense, 100m, "EUR", category: "Home", supplier: "S", costCenter: "C"),
            ],
            [new("EUR", 1m), new("USD", 0.5m)]);

        var response = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(150m, Single(Chart(response, AnalyticsChartIds.CapexExpenseByCategory)).SelectedYearAmountEur);
    }

    [Fact]
    public async Task Missing_rate_returns_configuration_incomplete_without_partial_aggregation()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 100m, "EUR", category: "Home", supplier: "S", costCenter: "C"),
                Capex("capex:2", new DateOnly(2026, 1, 6), AnalyticsMovementDirections.Expense, 100m, "USD", category: "Home", supplier: "S", costCenter: "C"),
            ],
            [new("EUR", 1m), new("USD", null)]);

        var response = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(["USD"], response.MissingExchangeRateCurrencyCodes);
        Assert.All(response.Charts, chart => Assert.Empty(chart.Points));
    }

    [Fact]
    public async Task Capex_view_ignores_other_source_modules()
    {
        var service = CreateService(
            [
                Capex("capex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 100m, "EUR", category: "Home", supplier: "S", costCenter: "C"),
                Opex("opex:1", new DateOnly(2026, 1, 5), AnalyticsMovementDirections.Expense, 999m, "EUR", category: "Rent", supplier: "S", costCenter: "C"),
            ],
            [new("EUR", 1m)]);

        var capex = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);
        var opex = await service.GetOpexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);

        Assert.Equal(100m, Single(Chart(capex, AnalyticsChartIds.CapexExpenseByCategory)).SelectedYearAmountEur);
        Assert.Equal("Home", Single(Chart(capex, AnalyticsChartIds.CapexExpenseByCategory)).Label);
        Assert.Equal(999m, Single(Chart(opex, AnalyticsChartIds.OpexExpenseByCategory)).SelectedYearAmountEur);
        Assert.Equal("Rent", Single(Chart(opex, AnalyticsChartIds.OpexExpenseByCategory)).Label);
    }

    [Fact]
    public async Task View_passes_viewer_to_source_providers()
    {
        var provider = new ViewerFilteringProvider(
            [
                (Owner, Capex("owner", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 10m, "EUR", category: "Home", supplier: "S", costCenter: "C")),
                (Collaborator, Capex("collaborator", new DateOnly(2026, 1, 1), AnalyticsMovementDirections.Expense, 99m, "EUR", category: "Home", supplier: "S", costCenter: "C")),
            ]);
        var service = new AnalyticsModuleGroupingService([provider], new FakeExchangeRateProvider([new("EUR", 1m)]));

        var owner = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Owner, CancellationToken.None);
        var collaborator = await service.GetCapexAsync(AnalyticsYearQuery.Create(2026), Collaborator, CancellationToken.None);

        Assert.Equal(10m, Single(Chart(owner, AnalyticsChartIds.CapexExpenseByCategory)).SelectedYearAmountEur);
        Assert.Equal(99m, Single(Chart(collaborator, AnalyticsChartIds.CapexExpenseByCategory)).SelectedYearAmountEur);
    }

    private static AnalyticsModuleGroupingService CreateService(
        IReadOnlyList<AnalyticsFinancialProjection> projections,
        IReadOnlyList<CurrencyExchangeRateSnapshot> rates) =>
        new([new FakeProjectionProvider(projections)], new FakeExchangeRateProvider(rates));

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

    private static AnalyticsChartResponse<AnalyticsGroupedAmountPoint> Chart(
        AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>> response,
        string chartId) =>
        response.Charts.Single(chart => chart.ChartId == chartId);

    private static AnalyticsGroupedAmountPoint Point(
        AnalyticsChartResponse<AnalyticsGroupedAmountPoint> chart,
        string label) =>
        chart.Points.Single(point => point.Label == label);

    private static AnalyticsGroupedAmountPoint Single(
        AnalyticsChartResponse<AnalyticsGroupedAmountPoint> chart) =>
        Assert.Single(chart.Points);

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
