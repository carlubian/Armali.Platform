import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import { wellnessApi } from '@/app/api/wellness'

import { wellnessKeys } from './contracts'

export function useWellnessToday() {
  return useQuery({
    queryKey: wellnessKeys.today(),
    queryFn: ({ signal }) => wellnessApi.today(signal),
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
