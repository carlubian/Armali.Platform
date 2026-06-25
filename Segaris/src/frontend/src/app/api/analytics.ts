import { apiRequest } from './client'

export type AnalyticsTab =
  | 'overview'
  | 'capex'
  | 'opex'
  | 'inventory'
  | 'travel'
  | 'cross-module'

export type AnalyticsSourceModule = 'capex' | 'opex' | 'inventory' | 'travel'
export type AnalyticsMovementDirection = 'Income' | 'Expense'

export const analyticsRoutePath = '/analytics' as const
export const analyticsChartLibrary = 'recharts' as const
export const analyticsMinimumYear = 2000 as const
export const analyticsMaximumYear = 2100 as const
export const analyticsHouseholdTimeZone = 'Europe/Madrid' as const

export const analyticsTabs = [
  'overview',
  'capex',
  'opex',
  'inventory',
  'travel',
  'cross-module',
] as const satisfies readonly AnalyticsTab[]

export const analyticsInitialProjectionSources = [
  'capex',
  'opex',
  'inventory',
  'travel',
] as const satisfies readonly AnalyticsSourceModule[]

export const analyticsMovementDirections = [
  'Income',
  'Expense',
] as const satisfies readonly AnalyticsMovementDirection[]

export const analyticsChartIds = {
  overviewMonthlyExpense: 'overview.monthlyExpense',
  overviewMonthlyIncome: 'overview.monthlyIncome',
  overviewMonthlyNetBalance: 'overview.monthlyNetBalance',
  capexExpenseByCategory: 'capex.expenseByCategory',
  capexExpenseBySupplier: 'capex.expenseBySupplier',
  capexExpenseByCostCenter: 'capex.expenseByCostCenter',
  capexIncomeByCategory: 'capex.incomeByCategory',
  capexIncomeBySupplier: 'capex.incomeBySupplier',
  capexIncomeByCostCenter: 'capex.incomeByCostCenter',
  opexExpenseByCategory: 'opex.expenseByCategory',
  opexExpenseBySupplier: 'opex.expenseBySupplier',
  opexExpenseByCostCenter: 'opex.expenseByCostCenter',
  opexIncomeByCategory: 'opex.incomeByCategory',
  opexIncomeBySupplier: 'opex.incomeBySupplier',
  opexIncomeByCostCenter: 'opex.incomeByCostCenter',
  inventoryExpenseByItemCategory: 'inventory.expenseByItemCategory',
  inventoryExpenseBySupplier: 'inventory.expenseBySupplier',
  inventoryAverageOrderBySupplier: 'inventory.averageOrderBySupplier',
  inventoryTopItems: 'inventory.topItems',
  inventoryTopSuppliers: 'inventory.topSuppliers',
  travelExpenseByCategory: 'travel.expenseByCategory',
  travelExpenseBySupplier: 'travel.expenseBySupplier',
  travelExpenseByCostCenter: 'travel.expenseByCostCenter',
  travelExpenseByDestination: 'travel.expenseByDestination',
  crossModuleExpenseBySupplier: 'crossModule.expenseBySupplier',
  crossModuleExpenseByCategory: 'crossModule.expenseByCategory',
  crossModuleExpenseByCostCenter: 'crossModule.expenseByCostCenter',
} as const

export interface AnalyticsMoneySeriesPoint {
  month: number
  selectedYearAmountEur: number
  previousYearAmountEur: number
}

export interface AnalyticsGroupedAmountPoint {
  label: string
  selectedYearAmountEur: number
  previousYearAmountEur: number
}

export interface AnalyticsAverageAmountPoint {
  label: string
  selectedYearAverageEur: number
  previousYearAverageEur: number
  selectedYearCount: number
  previousYearCount: number
}

export interface AnalyticsTopAmountPoint {
  label: string
  selectedYearAmountEur: number
  previousYearAmountEur: number
  selectedYearPercent: number
  previousYearPercent: number
}

export interface AnalyticsChartResponse<TPoint> {
  chartId: string
  points: TPoint[]
}

export interface AnalyticsViewResponse<TChart> {
  selectedYear: number
  previousYear: number
  charts: TChart[]
  missingExchangeRateCurrencyCodes: string[]
}

export interface AnalyticsOverviewTotals {
  selectedYearExpenseAmountEur: number
  previousYearExpenseAmountEur: number
  selectedYearIncomeAmountEur: number
  previousYearIncomeAmountEur: number
  selectedYearNetBalanceEur: number
  previousYearNetBalanceEur: number
}

export interface AnalyticsOverviewResponse {
  selectedYear: number
  previousYear: number
  totals: AnalyticsOverviewTotals
  charts: AnalyticsChartResponse<AnalyticsMoneySeriesPoint>[]
  missingExchangeRateCurrencyCodes: string[]
}

export interface AnalyticsInventoryResponse {
  selectedYear: number
  previousYear: number
  groupedCharts: AnalyticsChartResponse<AnalyticsGroupedAmountPoint>[]
  averageCharts: AnalyticsChartResponse<AnalyticsAverageAmountPoint>[]
  topCharts: AnalyticsChartResponse<AnalyticsTopAmountPoint>[]
  missingExchangeRateCurrencyCodes: string[]
}

export interface AnalyticsYearQuery {
  year: number
}

function buildYearQuery(query: AnalyticsYearQuery): string {
  return `?year=${encodeURIComponent(query.year)}`
}

export const analyticsApi = {
  overview: (query: AnalyticsYearQuery, signal?: AbortSignal) =>
    apiRequest<AnalyticsOverviewResponse>(
      `/api/analytics/overview${buildYearQuery(query)}`,
      { signal },
    ),
  capex: (query: AnalyticsYearQuery, signal?: AbortSignal) =>
    apiRequest<
      AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>
    >(`/api/analytics/capex${buildYearQuery(query)}`, { signal }),
  opex: (query: AnalyticsYearQuery, signal?: AbortSignal) =>
    apiRequest<
      AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>
    >(`/api/analytics/opex${buildYearQuery(query)}`, { signal }),
  inventory: (query: AnalyticsYearQuery, signal?: AbortSignal) =>
    apiRequest<AnalyticsInventoryResponse>(
      `/api/analytics/inventory${buildYearQuery(query)}`,
      { signal },
    ),
  travel: (query: AnalyticsYearQuery, signal?: AbortSignal) =>
    apiRequest<
      AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>
    >(`/api/analytics/travel${buildYearQuery(query)}`, { signal }),
  crossModule: (query: AnalyticsYearQuery, signal?: AbortSignal) =>
    apiRequest<
      AnalyticsViewResponse<AnalyticsChartResponse<AnalyticsGroupedAmountPoint>>
    >(`/api/analytics/cross-module${buildYearQuery(query)}`, { signal }),
}
