import { apiRequest } from './client'

/** Fixed category vocabulary for a catalogue task, exchanged on the wire. */
export type WellnessCategory = 'HealthAndBody' | 'MindAndSleep' | 'PeopleAndWork'

export const wellnessRoutePath = '/wellness' as const

export const wellnessCategories: readonly WellnessCategory[] = [
  'HealthAndBody',
  'MindAndSleep',
  'PeopleAndWork',
]

/** Maximum persisted length of a catalogue task name. */
export const wellnessTaskNameMaxLength = 200

/** A module-owned catalogue task surfaced through Configuration. */
export interface WellnessTask {
  id: number
  name: string
  category: WellnessCategory
  sortOrder: number
}

export interface WellnessTaskRequest {
  name: string
  category: WellnessCategory
}

/** One selected task inside the current household day (a persisted snapshot). */
export interface WellnessDayTask {
  id: number
  name: string
  category: WellnessCategory
  completed: boolean
  position: number
}

/**
 * The current household day's selected tasks and score for the current user.
 * `score` is the integer percentage of completed tasks (0-100) and is `null` only
 * for a day with no tasks (an empty catalogue); a visited day with zero completed
 * tasks reports 0.
 */
export interface WellnessToday {
  date: string
  score: number | null
  tasks: WellnessDayTask[]
}

/** Per-day score for one existing day; the projection the Mood weekly log consumes. */
export interface WellnessDayScore {
  date: string
  score: number | null
}

export interface WellnessDayList {
  from: string
  to: string
  days: WellnessDayScore[]
}

/** Inclusive `from`/`to` civil-date range for the days read (YYYY-MM-DD). */
export interface WellnessDaysQuery {
  from: string
  to: string
}

export const wellnessApi = {
  today: (signal?: AbortSignal) =>
    apiRequest<WellnessToday>('/api/wellness/today', { signal }),
  toggleDayTask: (dayTaskId: number, signal?: AbortSignal) =>
    apiRequest<WellnessToday>(`/api/wellness/today/tasks/${dayTaskId}/toggle`, {
      method: 'POST',
      signal,
    }),
  days: (query: WellnessDaysQuery, signal?: AbortSignal) =>
    apiRequest<WellnessDayList>(
      `/api/wellness/days?from=${query.from}&to=${query.to}`,
      { signal },
    ),
  tasks: (signal?: AbortSignal) =>
    apiRequest<WellnessTask[]>('/api/wellness/tasks', { signal }),
  createTask: (request: WellnessTaskRequest, signal?: AbortSignal) =>
    apiRequest<WellnessTask>('/api/wellness/tasks', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  deleteTask: (taskId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/wellness/tasks/${taskId}`, {
      method: 'DELETE',
      signal,
    }),
}
