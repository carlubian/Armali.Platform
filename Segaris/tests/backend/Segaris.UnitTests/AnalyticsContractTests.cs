using System.Text.Json;
using Segaris.Api.Modules.Analytics;
using Segaris.Api.Modules.Analytics.Contracts;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Shared.Time;

namespace Segaris.UnitTests;

public sealed class AnalyticsContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Routes_tabs_and_query_parameters_are_frozen()
    {
        Assert.Equal("Analytics", AnalyticsApiRoutes.Tag);
        Assert.Equal("analytics", AnalyticsApiRoutes.Analytics);
        Assert.Equal("/overview", AnalyticsApiRoutes.Overview);
        Assert.Equal("/capex", AnalyticsApiRoutes.Capex);
        Assert.Equal("/opex", AnalyticsApiRoutes.Opex);
        Assert.Equal("/inventory", AnalyticsApiRoutes.Inventory);
        Assert.Equal("/travel", AnalyticsApiRoutes.Travel);
        Assert.Equal("/cross-module", AnalyticsApiRoutes.CrossModule);
        Assert.Equal("year", AnalyticsApiRoutes.QueryParameters.Year);
        Assert.Equal("tab", AnalyticsApiRoutes.QueryParameters.Tab);
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "overview", "capex", "opex", "inventory", "travel", "cross-module",
            },
            AnalyticsTabs.All);
    }

    [Fact]
    public void Source_modules_and_movement_directions_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "capex", "opex", "inventory", "travel" },
            AnalyticsSourceModules.InitialProjectionSources);
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "Income", "Expense" },
            AnalyticsMovementDirections.All);
    }

    [Fact]
    public void Chart_identifiers_are_frozen()
    {
        Assert.Equal("overview.monthlyExpense", AnalyticsChartIds.OverviewMonthlyExpense);
        Assert.Equal("overview.monthlyIncome", AnalyticsChartIds.OverviewMonthlyIncome);
        Assert.Equal("overview.monthlyNetBalance", AnalyticsChartIds.OverviewMonthlyNetBalance);
        Assert.Equal("capex.expenseByCategory", AnalyticsChartIds.CapexExpenseByCategory);
        Assert.Equal("opex.incomeBySupplier", AnalyticsChartIds.OpexIncomeBySupplier);
        Assert.Equal("inventory.averageOrderBySupplier", AnalyticsChartIds.InventoryAverageOrderBySupplier);
        Assert.Equal("travel.expenseByDestination", AnalyticsChartIds.TravelExpenseByDestination);
        Assert.Equal("crossModule.expenseByCostCenter", AnalyticsChartIds.CrossModuleExpenseByCostCenter);
    }

    [Fact]
    public void Error_codes_and_launcher_attention_contract_are_stable()
    {
        Assert.Equal("analytics.year.invalid", AnalyticsErrorCodes.YearInvalid.Value);
        Assert.Equal("analytics.tab.unsupported", AnalyticsErrorCodes.TabUnsupported.Value);
        Assert.Equal("analytics.chart.unsupported", AnalyticsErrorCodes.ChartUnsupported.Value);
        Assert.Equal("analytics.currency.exchange_rate_missing", AnalyticsErrorCodes.MissingExchangeRate.Value);
        Assert.False(AnalyticsModuleContract.ContributesLauncherAttention);
    }

    [Fact]
    public void Year_query_parses_defaults_and_ranges_in_household_time()
    {
        var clock = new MutableClock
        {
            UtcNow = new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero),
        };

        var current = AnalyticsYearQuery.Parse(null, clock);
        var explicitYear = AnalyticsYearQuery.Parse("2026", clock);

        Assert.Equal("Europe/Madrid", AnalyticsYearQuery.HouseholdTimeZoneId);
        Assert.Equal((2026, 2025), (current.SelectedYear, current.PreviousYear));
        Assert.Equal((2026, 2025), (explicitYear.SelectedYear, explicitYear.PreviousYear));
        Assert.Equal(
            (new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            explicitYear.SelectedYearRange());
        Assert.Equal(
            (new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            explicitYear.PreviousYearRange());
        Assert.Throws<ArgumentException>(() => AnalyticsYearQuery.Parse("20x6", clock));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnalyticsYearQuery.Parse("1999", clock));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnalyticsYearQuery.Parse("2101", clock));
    }

    [Fact]
    public void Response_dtos_serialize_to_frontend_oriented_shapes()
    {
        var response = new AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsMoneySeriesPoint>>(
            2026,
            2025,
            [new AnalyticsChartResponse<AnalyticsMoneySeriesPoint>(
                AnalyticsChartIds.OverviewMonthlyExpense,
                [new AnalyticsMoneySeriesPoint(1, 123.45m, 67.89m)])],
            ["USD"]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, Web));

        Assert.Equal(2026, document.RootElement.GetProperty("selectedYear").GetInt32());
        Assert.Equal(2025, document.RootElement.GetProperty("previousYear").GetInt32());
        Assert.Equal("overview.monthlyExpense", document.RootElement.GetProperty("charts")[0].GetProperty("chartId").GetString());
        Assert.Equal(123.45m, document.RootElement.GetProperty("charts")[0].GetProperty("points")[0].GetProperty("selectedYearAmountEur").GetDecimal());
        Assert.Equal("USD", document.RootElement.GetProperty("missingExchangeRateCurrencyCodes")[0].GetString());
    }

    [Fact]
    public void Source_owned_projection_contracts_and_configuration_currency_contract_are_explicit()
    {
        Assert.Equal(
            [
                typeof(ICapexFinancialProjectionProvider),
                typeof(IOpexFinancialProjectionProvider),
                typeof(IInventoryFinancialProjectionProvider),
                typeof(ITravelFinancialProjectionProvider),
            ],
            AnalyticsProjectionContracts.InitialProviderContracts);
        Assert.Equal(typeof(ICurrencyExchangeRateProvider), AnalyticsProjectionContracts.CurrencyExchangeRateContract);
    }

    [Fact]
    public void Financial_projection_shape_carries_required_labels_without_source_entities()
    {
        var projection = new CapexFinancialProjection(
            "capex:42",
            "capex",
            "entry",
            new DateOnly(2026, 6, 25),
            "Expense",
            12.34m,
            "EUR",
            "Home",
            "IKEA",
            "Household",
            null,
            null,
            null);

        Assert.Equal("capex:42", projection.SourceId);
        Assert.Equal("Expense", projection.MovementDirection);
        Assert.Equal("EUR", projection.CurrencyCode);
        Assert.Equal("Home", projection.CategoryLabel);
        Assert.Equal("IKEA", projection.SupplierLabel);
        Assert.Equal("Household", projection.CostCenterLabel);
    }

    private sealed class MutableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
