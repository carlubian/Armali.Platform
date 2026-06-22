import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  placePageSizes,
  type PlaceListQuery,
  type PlacePageSize,
  type PlaceRating,
  type PlaceSortDirection,
  type PlaceSortField,
} from '@/app/api/destinations'

import { placeRatings } from './contracts'

const sortFields: readonly PlaceSortField[] = ['name', 'category', 'rating']

export const defaultPlaceSort: PlaceSortField = 'name'
export const defaultPlaceSortDirection: PlaceSortDirection = 'asc'
export const defaultPlacePageSize: PlacePageSize = 25

export interface PlacesState {
  search: string
  category: number | null
  rating: PlaceRating | null
  sort: PlaceSortField
  sortDirection: PlaceSortDirection
  page: number
  pageSize: PlacePageSize
}

export type PlacesFilterPatch = Partial<
  Omit<PlacesState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

export type PlaceDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; placeId: number }

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

function ratingOrNull(value: string | null): PlaceRating | null {
  const parsed = intOrNull(value)
  return parsed != null && (placeRatings as readonly number[]).includes(parsed)
    ? (parsed as PlaceRating)
    : null
}

export function parsePlacesState(params: URLSearchParams): PlacesState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)

  return {
    search: params.get('search') ?? '',
    category: intOrNull(params.get('category')),
    rating: ratingOrNull(params.get('rating')),
    sort: sort === '' ? defaultPlaceSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc'
        ? direction
        : defaultPlaceSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (placePageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as PlacePageSize)
      : defaultPlacePageSize,
  }
}

export function parsePlaceDialogState(params: URLSearchParams): PlaceDialogState {
  if (params.get('newPlace') === 'true') return { mode: 'create' }
  const placeId = intOrNull(params.get('placeId'))
  return placeId == null ? { mode: 'closed' } : { mode: 'edit', placeId }
}

export function toPlaceListQuery(state: PlacesState): PlaceListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    rating: state.rating,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(state: PlacesState): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('category', state.category)
  set('rating', state.rating)
  if (state.sort !== defaultPlaceSort) set('sort', state.sort)
  if (state.sortDirection !== defaultPlaceSortDirection)
    set('sortDirection', state.sortDirection)
  if (state.page !== 1) set('page', state.page)
  if (state.pageSize !== defaultPlacePageSize) set('pageSize', state.pageSize)

  return params
}

const LIST_PARAMS = new Set([
  'search',
  'category',
  'rating',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export interface UsePlacesState {
  state: PlacesState
  dialog: PlaceDialogState
  listQuery: PlaceListQuery
  setFilters: (patch: PlacesFilterPatch) => void
  setSort: (sort: PlaceSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: PlacePageSize) => void
  clearFilters: () => void
  openCreateDialog: () => void
  openEditDialog: (placeId: number) => void
  closeDialog: () => void
}

export function usePlacesState(): UsePlacesState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(() => parsePlacesState(searchParams), [searchParams])
  const dialog = useMemo(() => parsePlaceDialogState(searchParams), [searchParams])

  const commit = useCallback(
    (next: PlacesState, options?: { replace?: boolean }) => {
      setSearchParams(
        (current) => {
          const merged = writeState(next)
          for (const [key, value] of current.entries()) {
            if (!LIST_PARAMS.has(key)) merged.set(key, value)
          }
          return merged
        },
        { replace: options?.replace ?? false },
      )
    },
    [setSearchParams],
  )

  const setFilters = useCallback(
    (patch: PlacesFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: PlaceSortField) => {
      const sortDirection =
        state.sort === sort && state.sortDirection === 'asc' ? 'desc' : 'asc'
      commit({ ...state, sort, sortDirection, page: 1 })
    },
    [commit, state],
  )

  const setPage = useCallback(
    (page: number) => commit({ ...state, page }),
    [commit, state],
  )

  const setPageSize = useCallback(
    (pageSize: PlacePageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        category: null,
        rating: null,
        page: 1,
      }),
    [commit, state],
  )

  const listQuery = useMemo(() => toPlaceListQuery(state), [state])

  const setDialogParams = useCallback(
    (patch: { newPlace?: string | null; placeId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newPlace == null) next.delete('newPlace')
        else next.set('newPlace', patch.newPlace)
        if (patch.placeId == null) next.delete('placeId')
        else next.set('placeId', patch.placeId)
        return next
      })
    },
    [setSearchParams],
  )

  return {
    state,
    dialog,
    listQuery,
    setFilters,
    setSort,
    setPage,
    setPageSize,
    clearFilters,
    openCreateDialog: () => setDialogParams({ newPlace: 'true', placeId: null }),
    openEditDialog: (placeId: number) =>
      setDialogParams({ newPlace: null, placeId: String(placeId) }),
    closeDialog: () => setDialogParams({ newPlace: null, placeId: null }),
  }
}
