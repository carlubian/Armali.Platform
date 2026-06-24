import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  calendarSourceModules,
  calendarVisualFamilies,
  type CalendarEntriesQuery,
  type CalendarSourceModule,
  type CalendarVisualFamily,
} from '@/app/api/calendar'

const monthPattern = /^\d{4}-\d{2}$/
const datePattern = /^\d{4}-\d{2}-\d{2}$/

export const defaultCalendarMonth = 'current' as const

export interface CalendarFilterState {
  sourceModules: CalendarSourceModule[]
  visualFamilies: CalendarVisualFamily[]
}

export interface CalendarRouteState {
  month: string | typeof defaultCalendarMonth
  day: string | null
  filters: CalendarFilterState
}

export type CalendarDialogState =
  | { mode: 'closed' }
  | { mode: 'createNote' }
  | { mode: 'editNote'; noteId: number }

function allowedValues<T extends string>(
  values: string[],
  allowed: readonly T[],
): T[] {
  const unique = new Set<T>()
  for (const value of values) {
    if ((allowed as readonly string[]).includes(value)) unique.add(value as T)
  }
  return [...unique]
}

function intOrNull(value: string | null): number | null {
  if (value == null) return null
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

export function parseCalendarState(params: URLSearchParams): CalendarRouteState {
  const month = params.get('month')
  const day = params.get('day')
  return {
    month: month != null && monthPattern.test(month) ? month : defaultCalendarMonth,
    day: day != null && datePattern.test(day) ? day : null,
    filters: {
      sourceModules: allowedValues(params.getAll('sourceModule'), calendarSourceModules),
      visualFamilies: allowedValues(params.getAll('visualFamily'), calendarVisualFamilies),
    },
  }
}

export function parseCalendarDialogState(
  params: URLSearchParams,
): CalendarDialogState {
  if (params.get('newNote') === 'true') return { mode: 'createNote' }
  const noteId = intOrNull(params.get('noteId'))
  return noteId == null ? { mode: 'closed' } : { mode: 'editNote', noteId }
}

export function toCalendarEntriesQuery(
  from: string,
  to: string,
  state: CalendarRouteState,
): CalendarEntriesQuery {
  return {
    from,
    to,
    sourceModule:
      state.filters.sourceModules.length === 0
        ? undefined
        : state.filters.sourceModules,
    visualFamily:
      state.filters.visualFamilies.length === 0
        ? undefined
        : state.filters.visualFamilies,
  }
}

export function useCalendarState() {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(() => parseCalendarState(searchParams), [searchParams])
  const dialog = useMemo(
    () => parseCalendarDialogState(searchParams),
    [searchParams],
  )

  const patchParams = useCallback(
    (patch: Record<string, string | readonly string[] | null>) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        Object.entries(patch).forEach(([key, value]) => {
          next.delete(key)
          if (value == null) return
          if (Array.isArray(value)) {
            value.forEach((item) => next.append(key, item))
            return
          }
          if (typeof value === 'string') next.set(key, value)
        })
        return next
      })
    },
    [setSearchParams],
  )

  return {
    state,
    dialog,
    setMonth: (month: string) => patchParams({ month, day: null }),
    setDay: (day: string | null) => patchParams({ day }),
    setSourceModules: (sourceModules: readonly CalendarSourceModule[]) =>
      patchParams({ sourceModule: sourceModules }),
    setVisualFamilies: (visualFamilies: readonly CalendarVisualFamily[]) =>
      patchParams({ visualFamily: visualFamilies }),
    openCreateNote: () => patchParams({ newNote: 'true', noteId: null }),
    openEditNote: (noteId: number) =>
      patchParams({ noteId: String(noteId), newNote: null }),
    closeDialog: () => patchParams({ newNote: null, noteId: null }),
  }
}
