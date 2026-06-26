import { describe, expect, it } from 'vitest'

import {
  analyticsChartIds,
  analyticsChartLibrary,
  analyticsHouseholdTimeZone,
  analyticsInitialProjectionSources,
  analyticsMaximumYear,
  analyticsMinimumYear,
  analyticsMovementDirections,
  analyticsRoutePath,
  analyticsTabs,
} from '@/app/api/analytics'

import {
  defaultAnalyticsTab,
  nextAnalyticsYear,
  parseAnalyticsState,
  previousAnalyticsYear,
} from './analyticsState'
import { analyticsKeys, analyticsTabSchema, analyticsYearSchema } from './contracts'

describe('analytics contracts', () => {
  it('freezes route, tab, source, movement, chart-library, and chart-id contracts', () => {
    expect(analyticsRoutePath).toBe('/analytics')
    expect(analyticsChartLibrary).toBe('recharts')
    expect(analyticsHouseholdTimeZone).toBe('Europe/Madrid')
    expect(analyticsMinimumYear).toBe(2000)
    expect(analyticsMaximumYear).toBe(2100)
    expect(analyticsTabs).toEqual([
      'overview',
      'capex',
      'opex',
      'inventory',
      'travel',
      'cross-module',
    ])
    expect(analyticsInitialProjectionSources).toEqual([
      'capex',
      'opex',
      'inventory',
      'travel',
    ])
    expect(analyticsMovementDirections).toEqual(['Income', 'Expense'])
    expect(analyticsChartIds.overviewMonthlyExpense).toBe('overview.monthlyExpense')
    expect(analyticsChartIds.inventoryAverageOrderBySupplier).toBe(
      'inventory.averageOrderBySupplier',
    )
    expect(analyticsChartIds.crossModuleExpenseByCostCenter).toBe(
      'crossModule.expenseByCostCenter',
    )
  })

  it('freezes query keys by year and tab', () => {
    expect(analyticsKeys.tab(2026, 'inventory')).toEqual([
      'analytics',
      2026,
      'inventory',
    ])
  })

  it('parses URL-backed year and tab with current-year and overview fallbacks', () => {
    const today = new Date('2025-12-31T23:30:00.000Z')

    expect(
      parseAnalyticsState(new URLSearchParams('year=2026&tab=travel'), today),
    ).toEqual({
      year: 2026,
      tab: 'travel',
    })
    expect(
      parseAnalyticsState(new URLSearchParams('year=1999&tab=unknown'), today),
    ).toEqual({
      year: 2026,
      tab: defaultAnalyticsTab,
    })
  })

  it('clamps year navigation to the supported range', () => {
    expect(previousAnalyticsYear(2000)).toBe(2000)
    expect(previousAnalyticsYear(2026)).toBe(2025)
    expect(nextAnalyticsYear(2026)).toBe(2027)
    expect(nextAnalyticsYear(2100)).toBe(2100)
  })

  it('validates year and tab schemas', () => {
    expect(analyticsYearSchema.safeParse(2026).success).toBe(true)
    expect(analyticsYearSchema.safeParse(1999).success).toBe(false)
    expect(analyticsTabSchema.safeParse('cross-module').success).toBe(true)
    expect(analyticsTabSchema.safeParse('summary').success).toBe(false)
  })
})
