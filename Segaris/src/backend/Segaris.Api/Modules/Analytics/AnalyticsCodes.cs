namespace Segaris.Api.Modules.Analytics;

internal static class AnalyticsTabs
{
    public const string Overview = "overview";
    public const string Capex = "capex";
    public const string Opex = "opex";
    public const string Inventory = "inventory";
    public const string Travel = "travel";
    public const string CrossModule = "cross-module";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Overview,
            Capex,
            Opex,
            Inventory,
            Travel,
            CrossModule,
        };
}

internal static class AnalyticsChartIds
{
    public const string OverviewMonthlyExpense = "overview.monthlyExpense";
    public const string OverviewMonthlyIncome = "overview.monthlyIncome";
    public const string OverviewMonthlyNetBalance = "overview.monthlyNetBalance";
    public const string CapexExpenseByCategory = "capex.expenseByCategory";
    public const string CapexExpenseBySupplier = "capex.expenseBySupplier";
    public const string CapexExpenseByCostCenter = "capex.expenseByCostCenter";
    public const string CapexIncomeByCategory = "capex.incomeByCategory";
    public const string CapexIncomeBySupplier = "capex.incomeBySupplier";
    public const string CapexIncomeByCostCenter = "capex.incomeByCostCenter";
    public const string OpexExpenseByCategory = "opex.expenseByCategory";
    public const string OpexExpenseBySupplier = "opex.expenseBySupplier";
    public const string OpexExpenseByCostCenter = "opex.expenseByCostCenter";
    public const string OpexIncomeByCategory = "opex.incomeByCategory";
    public const string OpexIncomeBySupplier = "opex.incomeBySupplier";
    public const string OpexIncomeByCostCenter = "opex.incomeByCostCenter";
    public const string InventoryExpenseByItemCategory = "inventory.expenseByItemCategory";
    public const string InventoryExpenseBySupplier = "inventory.expenseBySupplier";
    public const string InventoryAverageOrderBySupplier = "inventory.averageOrderBySupplier";
    public const string InventoryTopItems = "inventory.topItems";
    public const string InventoryTopSuppliers = "inventory.topSuppliers";
    public const string TravelExpenseByCategory = "travel.expenseByCategory";
    public const string TravelExpenseBySupplier = "travel.expenseBySupplier";
    public const string TravelExpenseByCostCenter = "travel.expenseByCostCenter";
    public const string TravelExpenseByDestination = "travel.expenseByDestination";
    public const string CrossModuleExpenseBySupplier = "crossModule.expenseBySupplier";
    public const string CrossModuleExpenseByCategory = "crossModule.expenseByCategory";
    public const string CrossModuleExpenseByCostCenter = "crossModule.expenseByCostCenter";
}

internal static class AnalyticsSourceModules
{
    public const string Capex = "capex";
    public const string Opex = "opex";
    public const string Inventory = "inventory";
    public const string Travel = "travel";

    public static readonly IReadOnlySet<string> InitialProjectionSources =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Capex,
            Opex,
            Inventory,
            Travel,
        };
}

internal static class AnalyticsMovementDirections
{
    public const string Income = "Income";
    public const string Expense = "Expense";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Income, Expense };
}

internal static class AnalyticsModuleContract
{
    public const bool ContributesLauncherAttention = false;
}

internal static class AnalyticsLabels
{
    /// <summary>
    /// Stable label used when a grouped projection has no optional supplier or
    /// cost-centre value. Backend-owned because Analytics has no Spanish
    /// translations in the initial version.
    /// </summary>
    public const string Unassigned = "Unassigned";
}
