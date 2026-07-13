import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'

import { wellnessApi, type WellnessDaysQuery } from '@/app/api/wellness'

import { wellnessKeys } from './contracts'

export function useWellnessToday() {
  return useQuery({
    queryKey: wellnessKeys.today(),
    queryFn: ({ signal }) => wellnessApi.today(signal),
  })
}

/**
 * Per-day Wellness scores for a civil-date range, the seam the Mood weekly log
 * consumes to overlay Wellness on its own chart. `query` is `null` until the visible
 * week resolves, keeping the query idle so Mood renders without it. A failure surfaces
 * as an errored query the caller treats as "no Wellness this week", never degrading
 * the mood chart.
 */
export function useWellnessDays(query: WellnessDaysQuery | null) {
  return useQuery({
    queryKey:
      query == null ? [...wellnessKeys.all, 'days', 'idle'] : wellnessKeys.days(query),
    queryFn: ({ signal }) => wellnessApi.days(query as WellnessDaysQuery, signal),
    enabled: query != null,
    placeholderData: keepPreviousData,
  })
}

/**
 * Toggles one day-task's completion. The backend recomputes and returns the whole
 * day, so the response is written straight into the today cache and the Mood-facing
 * days range is invalidated because that day's persisted score has just changed.
 */
export function useToggleWellnessDayTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (dayTaskId: number) => wellnessApi.toggleDayTask(dayTaskId),
    onSuccess: (updated) => {
      queryClient.setQueryData(wellnessKeys.today(), updated)
      void queryClient.invalidateQueries({ queryKey: [...wellnessKeys.all, 'days'] })
    },
  })
}

export { wellnessKeys }
