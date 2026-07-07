import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

/** Fixed platform vocabulary for a catalogue game, exchanged on the wire. */
export type GamePlatform =
  | 'PC'
  | 'Console'
  | 'Mobile'
  | 'BoardGame'
  | 'TabletopRpg'
  | 'Other'

/** Fixed, manually controlled playthrough status. */
export type PlaythroughStatus = 'Planning' | 'Active' | 'Completed'

/** Fixed highlight-colour palette token for a section. */
export type SectionColor =
  | 'Blue'
  | 'Green'
  | 'Amber'
  | 'Red'
  | 'Purple'
  | 'Pink'
  | 'Teal'
  | 'Indigo'
  | 'Slate'
  | 'Orange'

/** Platform visibility values used by playthroughs. */
export type GamesVisibility = 'Public' | 'Private'
export type GamesSortDirection = 'asc' | 'desc'
export type PlaythroughSortField = 'name' | 'game' | 'startDate' | 'status' | 'progress'

export const gamesRoutePath = '/games' as const
export const gamesPageSizes = [10, 25, 50, 100] as const
export type GamesPageSize = (typeof gamesPageSizes)[number]

export const gamePlatforms: readonly GamePlatform[] = [
  'PC',
  'Console',
  'Mobile',
  'BoardGame',
  'TabletopRpg',
  'Other',
]

export const playthroughStatuses: readonly PlaythroughStatus[] = [
  'Planning',
  'Active',
  'Completed',
]

export const sectionColors: readonly SectionColor[] = [
  'Blue',
  'Green',
  'Amber',
  'Red',
  'Purple',
  'Pink',
  'Teal',
  'Indigo',
  'Slate',
  'Orange',
]

export interface Game {
  id: number
  name: string
  platform: GamePlatform
  sortOrder: number
}

export interface GameRequest {
  name: string
  platform: GamePlatform
}

/** Derived, never-persisted progress projection. */
export interface Progress {
  completedGoals: number
  totalGoals: number
}

export interface PlaythroughSummary {
  id: number
  name: string
  gameId: number
  gameName: string
  platform: GamePlatform
  status: PlaythroughStatus
  startYear: number
  startMonth: number
  tags: string[]
  progress: Progress
  visibility: GamesVisibility
  creatorId: number
  creatorName: string
}

export interface Playthrough extends PlaythroughSummary {
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface Section {
  id: number
  name: string
  color: SectionColor
  sortOrder: number
  progress: Progress
}

export interface Goal {
  id: number
  text: string
  completed: boolean
  position: number
}

export interface PlaythroughListQuery {
  search?: string | null
  game?: number | null
  platform?: GamePlatform | null
  status?: PlaythroughStatus | null
  tag?: string | null
  creator?: number | null
  visibility?: GamesVisibility | null
  page?: number
  pageSize?: number
  sort?: PlaythroughSortField
  sortDirection?: GamesSortDirection
}

export interface PlaythroughRequest {
  name: string
  gameId: number
  startYear: number
  startMonth: number
  status: PlaythroughStatus
  tags: string[]
  visibility: GamesVisibility
}

export interface SectionRequest {
  name: string
  color: SectionColor
}

export interface SectionOrderRequest {
  sectionIds: number[]
}

export interface GoalRequest {
  text: string
}

export interface GoalCompletionRequest {
  completed: boolean
}

function buildQuery<T extends object>(query: T): string {
  const parameters = new URLSearchParams()
  Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
    if (value == null) return
    const text =
      typeof value === 'string'
        ? value.trim()
        : typeof value === 'number' || typeof value === 'boolean'
          ? String(value)
          : ''
    if (text.length > 0) parameters.set(key, text)
  })
  const search = parameters.toString()
  return search ? `?${search}` : ''
}

export const gamesApi = {
  games: (signal?: AbortSignal) => apiRequest<Game[]>('/api/games/games', { signal }),
  listPlaythroughs: (query: PlaythroughListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<PlaythroughSummary>>(
      `/api/games/playthroughs${buildQuery(query)}`,
      { signal },
    ),
  getPlaythrough: (playthroughId: number, signal?: AbortSignal) =>
    apiRequest<Playthrough>(`/api/games/playthroughs/${playthroughId}`, { signal }),
  createPlaythrough: (request: PlaythroughRequest, signal?: AbortSignal) =>
    apiRequest<Playthrough>('/api/games/playthroughs', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updatePlaythrough: (
    playthroughId: number,
    request: PlaythroughRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Playthrough>(`/api/games/playthroughs/${playthroughId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deletePlaythrough: (playthroughId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/games/playthroughs/${playthroughId}`, {
      method: 'DELETE',
      signal,
    }),
  listSections: (playthroughId: number, signal?: AbortSignal) =>
    apiRequest<Section[]>(`/api/games/playthroughs/${playthroughId}/sections`, {
      signal,
    }),
  createSection: (
    playthroughId: number,
    request: SectionRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Section>(`/api/games/playthroughs/${playthroughId}/sections`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  reorderSections: (
    playthroughId: number,
    request: SectionOrderRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/games/playthroughs/${playthroughId}/sections/order`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  updateSection: (
    playthroughId: number,
    sectionId: number,
    request: SectionRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Section>(
      `/api/games/playthroughs/${playthroughId}/sections/${sectionId}`,
      { method: 'PUT', body: JSON.stringify(request), signal },
    ),
  deleteSection: (playthroughId: number, sectionId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/games/playthroughs/${playthroughId}/sections/${sectionId}`, {
      method: 'DELETE',
      signal,
    }),
  listGoals: (playthroughId: number, sectionId: number, signal?: AbortSignal) =>
    apiRequest<Goal[]>(
      `/api/games/playthroughs/${playthroughId}/sections/${sectionId}/goals`,
      { signal },
    ),
  createGoal: (
    playthroughId: number,
    sectionId: number,
    request: GoalRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Goal>(
      `/api/games/playthroughs/${playthroughId}/sections/${sectionId}/goals`,
      { method: 'POST', body: JSON.stringify(request), signal },
    ),
  updateGoal: (
    playthroughId: number,
    sectionId: number,
    goalId: number,
    request: GoalRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Goal>(
      `/api/games/playthroughs/${playthroughId}/sections/${sectionId}/goals/${goalId}`,
      { method: 'PUT', body: JSON.stringify(request), signal },
    ),
  setGoalCompletion: (
    playthroughId: number,
    sectionId: number,
    goalId: number,
    request: GoalCompletionRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Goal>(
      `/api/games/playthroughs/${playthroughId}/sections/${sectionId}/goals/${goalId}/completion`,
      { method: 'PUT', body: JSON.stringify(request), signal },
    ),
  deleteGoal: (
    playthroughId: number,
    sectionId: number,
    goalId: number,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(
      `/api/games/playthroughs/${playthroughId}/sections/${sectionId}/goals/${goalId}`,
      { method: 'DELETE', signal },
    ),
}

export const gamesManagementApi: CatalogManagementClient<Game, GameRequest> =
  catalogManagementClient<Game, GameRequest>('/api/games/games')
