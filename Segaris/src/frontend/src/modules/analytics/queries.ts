import { useQuery } from '@tanstack/react-query'

import {
  analyticsApi,
  type AnalyticsChartResponse,
  type AnalyticsGroupedAmountPoint,
  type AnalyticsInventoryResponse,
  type AnalyticsOverviewResponse,
  type AnalyticsViewResponse,
} from '@/app/api/analytics'

import { analyticsKeys } from './contracts'

type GroupedViewResponse = AnalyticsViewResponse<
  AnalyticsChartResponse<AnalyticsGroupedAmountPoint>
>

/**
 * Each tab owns a typed query keyed by year and tab. Only the active tab's panel
 * is mounted, so a tab is fetched lazily the first time it is opened and then
 * cached; switching years or tabs swaps to a separately cached entry.
 */
export function useAnalyticsOverview(year: number) {
  return useQuery({
    queryKey: analyticsKeys.tab(year, 'overview'),
    queryFn: ({ signal }): Promise<AnalyticsOverviewResponse> =>
      analyticsApi.overview({ year }, signal),
  })
}

export function useAnalyticsCapex(year: number) {
  return useQuery({
    queryKey: analyticsKeys.tab(year, 'capex'),
    queryFn: ({ signal }): Promise<GroupedViewResponse> =>
      analyticsApi.capex({ year }, signal),
  })
}

export function useAnalyticsOpex(year: number) {
  return useQuery({
    queryKey: analyticsKeys.tab(year, 'opex'),
    queryFn: ({ signal }): Promise<GroupedViewResponse> =>
      analyticsApi.opex({ year }, signal),
  })
}

export function useAnalyticsInventory(year: number) {
  return useQuery({
    queryKey: analyticsKeys.tab(year, 'inventory'),
    queryFn: ({ signal }): Promise<AnalyticsInventoryResponse> =>
      analyticsApi.inventory({ year }, signal),
  })
}

export function useAnalyticsTravel(year: number) {
  return useQuery({
    queryKey: analyticsKeys.tab(year, 'travel'),
    queryFn: ({ signal }): Promise<GroupedViewResponse> =>
      analyticsApi.travel({ year }, signal),
  })
}

export function useAnalyticsCrossModule(year: number) {
  return useQuery({
    queryKey: analyticsKeys.tab(year, 'cross-module'),
    queryFn: ({ signal }): Promise<GroupedViewResponse> =>
      analyticsApi.crossModule({ year }, signal),
  })
}

export { analyticsKeys }
