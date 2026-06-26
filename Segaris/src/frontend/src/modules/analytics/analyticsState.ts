import {
  analyticsHouseholdTimeZone,
  analyticsMaximumYear,
  analyticsMinimumYear,
  analyticsTabs,
  type AnalyticsTab,
} from '@/app/api/analytics'

export const defaultAnalyticsTab = 'overview' as const satisfies AnalyticsTab

export interface AnalyticsRouteState {
  year: number
  tab: AnalyticsTab
}

/** Current civil year in the household time zone (`Europe/Madrid`). */
export function currentAnalyticsYear(today = new Date()): number {
  return Number(
    new Intl.DateTimeFormat('en-GB', {
      timeZone: analyticsHouseholdTimeZone,
      year: 'numeric',
    }).format(today),
  )
}

function parseYear(value: string | null, today: Date): number {
  if (value == null || !/^\d{4}$/.test(value)) return currentAnalyticsYear(today)
  const year = Number(value)
  if (year < analyticsMinimumYear || year > analyticsMaximumYear) {
    return currentAnalyticsYear(today)
  }
  return year
}

function parseTab(value: string | null): AnalyticsTab {
  return value != null && (analyticsTabs as readonly string[]).includes(value)
    ? (value as AnalyticsTab)
    : defaultAnalyticsTab
}

export function parseAnalyticsState(
  params: URLSearchParams,
  today = new Date(),
): AnalyticsRouteState {
  return {
    year: parseYear(params.get('year'), today),
    tab: parseTab(params.get('tab')),
  }
}

export function nextAnalyticsYear(year: number): number {
  return Math.min(analyticsMaximumYear, year + 1)
}

export function previousAnalyticsYear(year: number): number {
  return Math.max(analyticsMinimumYear, year - 1)
}
