import { useQuery } from '@tanstack/react-query'

import { calendarApi, type CalendarEntriesQuery } from '@/app/api/calendar'

import { calendarKeys } from './contracts'

export function useCalendarEntries(query: CalendarEntriesQuery) {
  return useQuery({
    queryKey: calendarKeys.entriesRange(query),
    queryFn: ({ signal }) => calendarApi.entries(query, signal),
  })
}

export { calendarKeys }
