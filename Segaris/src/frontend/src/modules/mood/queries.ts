import { keepPreviousData, useQuery } from '@tanstack/react-query'

import { moodApi, type MoodEntryRangeQuery } from '@/app/api/mood'

import { moodKeys } from './contracts'

// Fixed criteria and emotion codes change only with a deployment, so the options
// payload can be cached aggressively.
const optionsStaleTime = 60 * 60 * 1000

export function useMoodOptions() {
  return useQuery({
    queryKey: moodKeys.options(),
    queryFn: ({ signal }) => moodApi.options(signal),
    staleTime: optionsStaleTime,
  })
}

/** Owner-only entries and daily averages for the selected Monday-to-Sunday week. */
export function useMoodWeek(range: MoodEntryRangeQuery) {
  return useQuery({
    queryKey: moodKeys.entryRange(range),
    queryFn: ({ signal }) => moodApi.listEntries(range, signal),
    // Keep the previous week visible while the next one loads so navigating does
    // not flash an empty board.
    placeholderData: keepPreviousData,
  })
}

export function useMoodEntry(entryId: number | null) {
  return useQuery({
    queryKey: moodKeys.entry(entryId ?? 0),
    queryFn: ({ signal }) => moodApi.getEntry(entryId as number, signal),
    enabled: entryId != null,
  })
}

export { moodKeys }
