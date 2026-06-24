import { apiRequest } from './client'

export type CalendarSourceModule =
  | 'calendar'
  | 'firebird'
  | 'travel'
  | 'inventory'
  | 'assets'
  | 'maintenance'
  | 'processes'

export type CalendarInitialProjectionSource = Exclude<CalendarSourceModule, 'calendar'>

export type CalendarSourceType =
  | 'dailyNote'
  | 'birthday'
  | 'trip'
  | 'inventoryOrderExpectedReceipt'
  | 'assetExpectedEndOfLife'
  | 'maintenanceTaskDue'
  | 'processStepDue'

export type CalendarVisualFamily = 'Birthday' | 'Travel' | 'Note' | 'Other'
export type CalendarVisibility = 'Public' | 'Private'

export const calendarRoutePath = '/calendar' as const
export const calendarMaximumRangeDays = 366 as const
export const calendarSourceModules = [
  'calendar',
  'firebird',
  'travel',
  'inventory',
  'assets',
  'maintenance',
  'processes',
] as const satisfies readonly CalendarSourceModule[]
export const calendarInitialProjectionSources = [
  'firebird',
  'travel',
  'inventory',
  'assets',
  'maintenance',
  'processes',
] as const satisfies readonly CalendarInitialProjectionSource[]
export const calendarSourceTypes = [
  'dailyNote',
  'birthday',
  'trip',
  'inventoryOrderExpectedReceipt',
  'assetExpectedEndOfLife',
  'maintenanceTaskDue',
  'processStepDue',
] as const satisfies readonly CalendarSourceType[]
export const calendarVisualFamilies = [
  'Birthday',
  'Travel',
  'Note',
  'Other',
] as const satisfies readonly CalendarVisualFamily[]
export const calendarIndicatorPriority = [
  'Travel',
  'Birthday',
  'Note',
  'Other',
] as const satisfies readonly CalendarVisualFamily[]

export interface CalendarEntry {
  id: string
  sourceModule: CalendarSourceModule
  sourceType: CalendarSourceType
  visualFamily: CalendarVisualFamily
  title: string
  subtitle: string | null
  startDate: string
  endDate: string | null
  isAllDay: boolean
  status: string | null
  targetRoute: string | null
}

export interface CalendarDailyNote {
  id: number
  date: string
  title: string | null
  body: string
  visibility: CalendarVisibility
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface CalendarEntriesQuery {
  from: string
  to: string
  sourceModule?: readonly CalendarSourceModule[]
  visualFamily?: readonly CalendarVisualFamily[]
}

export interface CalendarNotesQuery {
  from: string
  to: string
}

export interface CalendarDailyNoteRequest {
  date: string
  title: string | null
  body: string
  visibility: CalendarVisibility
}

function buildQuery<T extends object>(query: T): string {
  const parameters = new URLSearchParams()
  Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
    if (value == null) return
    if (Array.isArray(value)) {
      value.forEach((item) => parameters.append(key, String(item)))
      return
    }
    const text = String(value).trim()
    if (text.length > 0) parameters.set(key, text)
  })
  const search = parameters.toString()
  return search ? `?${search}` : ''
}

export const calendarApi = {
  entries: (query: CalendarEntriesQuery, signal?: AbortSignal) =>
    apiRequest<CalendarEntry[]>(`/api/calendar/entries${buildQuery({ ...query })}`, {
      signal,
    }),
  notes: (query: CalendarNotesQuery, signal?: AbortSignal) =>
    apiRequest<CalendarDailyNote[]>(`/api/calendar/notes${buildQuery(query)}`, {
      signal,
    }),
  getNote: (noteId: number, signal?: AbortSignal) =>
    apiRequest<CalendarDailyNote>(`/api/calendar/notes/${noteId}`, { signal }),
  createNote: (request: CalendarDailyNoteRequest, signal?: AbortSignal) =>
    apiRequest<CalendarDailyNote>('/api/calendar/notes', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateNote: (
    noteId: number,
    request: CalendarDailyNoteRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<CalendarDailyNote>(`/api/calendar/notes/${noteId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteNote: (noteId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/calendar/notes/${noteId}`, {
      method: 'DELETE',
      signal,
    }),
}
