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

/** Filter fields that reset pagination back to the first page when they change. */
export type PlaythroughFilterPatch = Partial<
  Omit<PlaythroughListState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

/** Count of filters currently narrowing the collection. */
export function activePlaythroughFilterCount(state: PlaythroughListState): number {
  return [
    state.search.trim() !== '',
    state.game != null,
    state.platform !== '',
    state.status !== '',
    state.tag.trim() !== '',
    state.visibility !== '',
    state.mine,
  ].filter(Boolean).length
}

/** Search parameters owned by the collection list; everything else is preserved. */
const LIST_PARAMS = new Set([
  'search',
  'game',
  'platform',
  'status',
  'tag',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

function writeCollectionState(
  state: PlaythroughListState,
  currentUserId: number | null,
): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('game', state.game)
  set('platform', state.platform === '' ? null : state.platform)
  set('status', state.status === '' ? null : state.status)
  set('tag', state.tag.trim() === '' ? null : state.tag.trim())
  set('visibility', state.visibility === '' ? null : state.visibility)
  set('creator', state.mine && currentUserId != null ? currentUserId : null)
  // Persist sort/page only when they diverge from the defaults to keep links clean.
  if (state.sort !== defaultPlaythroughSort) set('sort', state.sort)
  if (state.sortDirection !== defaultSortDirection)
    set('sortDirection', state.sortDirection)
  if (state.page !== 1) set('page', state.page)
  if (state.pageSize !== defaultPageSize) set('pageSize', state.pageSize)

  return params
}

/**
 * Reads and writes the playthrough collection query and editor-dialog mode through
 * the URL so the card list is linkable and survives the dialog opening and closing.
 * Filter changes reset to the first page; dialog parameters are preserved across
 * list changes and vice versa.
 */
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

  const commit = useCallback(
    (next: PlaythroughListState, options?: { replace?: boolean }) => {
      setSearchParams(
        (current) => {
          const merged = writeCollectionState(next, currentUserId)
          // Preserve non-list params such as the editor dialog's newPlaythrough/id.
          for (const [key, value] of current.entries()) {
            if (!LIST_PARAMS.has(key)) merged.set(key, value)
          }
          return merged
        },
        { replace: options?.replace ?? false },
      )
    },
    [setSearchParams, currentUserId],
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

  const setFilters = useCallback(
    (patch: PlaythroughFilterPatch) => {
      // Any filter change returns to the first page; searching replaces history so
      // rapid typing does not flood the back button.
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: PlaythroughSortField) => {
      const sortDirection =
        state.sort === sort && state.sortDirection === 'asc' ? 'desc' : 'asc'
      commit({ ...state, sort, sortDirection, page: 1 })
    },
    [commit, state],
  )

  const setSortField = useCallback(
    (sort: PlaythroughSortField) => commit({ ...state, sort, page: 1 }),
    [commit, state],
  )

  const toggleSortDirection = useCallback(() => {
    const sortDirection = state.sortDirection === 'asc' ? 'desc' : 'asc'
    commit({ ...state, sortDirection, page: 1 })
  }, [commit, state])

  const setPage = useCallback(
    (page: number) => commit({ ...state, page }),
    [commit, state],
  )

  const setPageSize = useCallback(
    (pageSize: GamesPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        game: null,
        platform: '',
        status: '',
        tag: '',
        visibility: '',
        mine: false,
        page: 1,
      }),
    [commit, state],
  )

  return {
    state,
    dialog,
    listQuery,
    setFilters,
    setSort,
    setSortField,
    toggleSortDirection,
    setPage,
    setPageSize,
    clearFilters,
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
