import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  gamesPageSizes,
  type GamePlatform,
  type GamesPageSize,
  type GamesSortDirection,
  type GamesVisibility,
  type PlaythroughListQuery,
  type PlaythroughSortField,
  type PlaythroughStatus,
} from '@/app/api/games'

const platforms: readonly GamePlatform[] = [
  'PC',
  'Console',
  'Mobile',
  'BoardGame',
  'TabletopRpg',
  'Other',
]
const statuses: readonly PlaythroughStatus[] = ['Planning', 'Active', 'Completed']
const sortFields: readonly PlaythroughSortField[] = [
  'name',
  'game',
  'startDate',
  'status',
  'progress',
]
const visibilities: readonly GamesVisibility[] = ['Public', 'Private']

export const defaultPlaythroughSort: PlaythroughSortField = 'name'
export const defaultSortDirection: GamesSortDirection = 'asc'
export const defaultPageSize: GamesPageSize = 25

export interface PlaythroughListState {
  search: string
  game: number | null
  platform: GamePlatform | ''
  status: PlaythroughStatus | ''
  tag: string
  visibility: GamesVisibility | ''
  mine: boolean
  sort: PlaythroughSortField
  sortDirection: GamesSortDirection
  page: number
  pageSize: GamesPageSize
}

export type PlaythroughDialogState =
  | { mode: 'closed' }
  | { mode: 'createPlaythrough' }
  | { mode: 'editPlaythrough'; playthroughId: number }

function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | '' {
  return value != null && (allowed as readonly string[]).includes(value)
    ? (value as T)
    : ''
}

function intOrNull(value: string | null): number | null {
  if (value == null) return null
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

function pageSize(value: string | null): GamesPageSize {
  const parsed = Number.parseInt(value ?? '', 10)
  return (gamesPageSizes as readonly number[]).includes(parsed)
    ? (parsed as GamesPageSize)
    : defaultPageSize
}

function page(value: string | null): number {
  const parsed = Number.parseInt(value ?? '', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1
}

function direction(value: string | null): GamesSortDirection {
  return value === 'desc' ? 'desc' : defaultSortDirection
}

export function parsePlaythroughListState(
  params: URLSearchParams,
  currentUserId: number | null,
): PlaythroughListState {
  const creator = intOrNull(params.get('creator'))
  const sort = oneOf(params.get('sort'), sortFields)
  return {
    search: params.get('search') ?? '',
    game: intOrNull(params.get('game')),
    platform: oneOf(params.get('platform'), platforms),
    status: oneOf(params.get('status'), statuses),
    tag: params.get('tag') ?? '',
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultPlaythroughSort : sort,
    sortDirection: direction(params.get('sortDirection')),
    page: page(params.get('page')),
    pageSize: pageSize(params.get('pageSize')),
  }
}

export function parsePlaythroughDialogState(
  params: URLSearchParams,
): PlaythroughDialogState {
  if (params.get('newPlaythrough') === 'true') return { mode: 'createPlaythrough' }
  const playthroughId = intOrNull(params.get('playthroughId'))
  return playthroughId == null
    ? { mode: 'closed' }
    : { mode: 'editPlaythrough', playthroughId }
}

/** Selected section on the playthrough-scoped progress page, or null when none. */
export function parseSelectedSection(params: URLSearchParams): number | null {
  return intOrNull(params.get('sectionId'))
}

/** Whether the section-management popup/mode is open on the progress page. */
export function parseManageSections(params: URLSearchParams): boolean {
  return params.get('manageSections') === 'true'
}

export function toPlaythroughListQuery(
  state: PlaythroughListState,
  currentUserId: number | null,
): PlaythroughListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    game: state.game,
    platform: state.platform === '' ? null : state.platform,
    status: state.status === '' ? null : state.status,
    tag: state.tag.trim() === '' ? null : state.tag.trim(),
    creator: state.mine ? currentUserId : null,
    visibility: state.visibility === '' ? null : state.visibility,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

export function useGamesCollectionState(currentUserId: number | null) {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parsePlaythroughListState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(
    () => parsePlaythroughDialogState(searchParams),
    [searchParams],
  )
  const listQuery = useMemo(
    () => toPlaythroughListQuery(state, currentUserId),
    [state, currentUserId],
  )

  const patchParams = useCallback(
    (patch: Record<string, string | null>) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        Object.entries(patch).forEach(([key, value]) => {
          if (value == null) next.delete(key)
          else next.set(key, value)
        })
        return next
      })
    },
    [setSearchParams],
  )

  return {
    state,
    dialog,
    listQuery,
    openCreatePlaythrough: () =>
      patchParams({ newPlaythrough: 'true', playthroughId: null }),
    openEditPlaythrough: (playthroughId: number) =>
      patchParams({ playthroughId: String(playthroughId), newPlaythrough: null }),
    closeDialog: () => patchParams({ newPlaythrough: null, playthroughId: null }),
  }
}

/** Progress-page route state scoped to a single playthrough. */
export function useProgressPageState() {
  const [searchParams, setSearchParams] = useSearchParams()
  const selectedSectionId = useMemo(
    () => parseSelectedSection(searchParams),
    [searchParams],
  )
  const manageSections = useMemo(
    () => parseManageSections(searchParams),
    [searchParams],
  )

  const patchParams = useCallback(
    (patch: Record<string, string | null>) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        Object.entries(patch).forEach(([key, value]) => {
          if (value == null) next.delete(key)
          else next.set(key, value)
        })
        return next
      })
    },
    [setSearchParams],
  )

  return {
    selectedSectionId,
    manageSections,
    selectSection: (sectionId: number | null) =>
      patchParams({ sectionId: sectionId == null ? null : String(sectionId) }),
    openManageSections: () => patchParams({ manageSections: 'true' }),
    closeManageSections: () => patchParams({ manageSections: null }),
  }
}
