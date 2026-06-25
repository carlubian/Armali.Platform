import { useQuery } from '@tanstack/react-query'

import { analyticsApi, type AnalyticsTab } from '@/app/api/analytics'

import { analyticsKeys } from './contracts'

type AnalyticsTabQuery = (
  query: { year: number },
  signal?: AbortSignal,
) => Promise<unknown>

const analyticsTabQueries: Record<AnalyticsTab, AnalyticsTabQuery> = {
  overview: analyticsApi.overview,
  capex: analyticsApi.capex,
  opex: analyticsApi.opex,
  inventory: analyticsApi.inventory,
  travel: analyticsApi.travel,
  'cross-module': analyticsApi.crossModule,
}

export function useAnalyticsTab(year: number, tab: AnalyticsTab, enabled = true) {
  return useQuery({
    queryKey: analyticsKeys.tab(year, tab),
    queryFn: ({ signal }) => analyticsTabQueries[tab]({ year }, signal),
    enabled,
  })
}

export { analyticsKeys }
