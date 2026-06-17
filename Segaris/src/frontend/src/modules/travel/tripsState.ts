import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  travelPageSizes,
  type TravelPageSize,
  type TravelSortDirection,
  type TravelTripListQuery,
  type TravelTripSortField,
  type TravelTripStatus,
  type TravelVisibility,
} from '@/app/api/travel'

const sortFields: readonly TravelTripSortField[] = [
  'name',
  'tripType',
  'destination',
  'startDate',
  'endDate',
  'status',
  'visibility',
]
const statuses: readonly TravelTripStatus[] = [
  'Planned',
  'Ongoing',
  'Completed',
  'Cancelled',
]
const visibilities: readonly TravelVisibility[] = ['Public', 'Private']

export const defaultSort: TravelTripSortField = 'startDate'
export const defaultSortDirection: TravelSortDirection = 'desc'
export const defaultPageSize: TravelPageSize = 25

export interface TripsState {
  search: string
  tripType: number | null
  status: TravelTripStatus | ''
  visibility: TravelVisibility | ''
  mine: boolean
  sort: TravelTripSortField
  sortDirection: TravelSortDirection
  page: number
  pageSize: TravelPageSize
}

export type TripsFilterPatch = Partial<
  Omit<TripsState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

export type TripDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; tripId: number }

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

export function parseTripsState(
  params: URLSearchParams,
  currentUserId: number | null,
): TripsState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    tripType: intOrNull(params.get('tripType')),
    status: oneOf(params.get('status'), statuses),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (travelPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as TravelPageSize)
      : defaultPageSize,
  }
}

export function parseTripDialogState(params: URLSearchParams): TripDialogState {
  if (params.get('newTrip') === 'true') return { mode: 'create' }
  const tripId = intOrNull(params.get('tripId'))
  return tripId == null ? { mode: 'closed' } : { mode: 'edit', tripId }
}

export function toListQuery(
  state: TripsState,
  currentUserId: number | null,
): TravelTripListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    tripType: state.tripType,
    status: state.status === '' ? null : state.status,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(state: TripsState, currentUserId: number | null): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('tripType', state.tripType)
  set('status', state.status === '' ? null : state.status)
  set('visibility', state.visibility === '' ? null : state.visibility)
  set('creator', state.mine && currentUserId != null ? currentUserId : null)
  if (state.sort !== defaultSort) set('sort', state.sort)
  if (state.sortDirection !== defaultSortDirection)
    set('sortDirection', state.sortDirection)
  if (state.page !== 1) set('page', state.page)
  if (state.pageSize !== defaultPageSize) set('pageSize', state.pageSize)

  return params
}

const LIST_PARAMS = new Set([
  'search',
  'tripType',
  'status',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export interface UseTripsState {
  state: TripsState
  dialog: TripDialogState
  listQuery: TravelTripListQuery
  setFilters: (patch: TripsFilterPatch) => void
  setSort: (sort: TravelTripSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: TravelPageSize) => void
  openCreateDialog: () => void
  openEditDialog: (tripId: number) => void
  closeDialog: () => void
  clearFilters: () => void
}

export function useTripsState(currentUserId: number | null): UseTripsState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseTripsState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(() => parseTripDialogState(searchParams), [searchParams])

  const commit = useCallback(
    (next: TripsState, options?: { replace?: boolean }) => {
      setSearchParams(
        (current) => {
          const merged = writeState(next, currentUserId)
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

  const setFilters = useCallback(
    (patch: TripsFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: TravelTripSortField) => {
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
    (pageSize: TravelPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const setDialogParams = useCallback(
    (patch: { newTrip?: string | null; tripId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newTrip == null) next.delete('newTrip')
        else next.set('newTrip', patch.newTrip)
        if (patch.tripId == null) next.delete('tripId')
        else next.set('tripId', patch.tripId)
        return next
      })
    },
    [setSearchParams],
  )

  const openCreateDialog = useCallback(
    () => setDialogParams({ newTrip: 'true', tripId: null }),
    [setDialogParams],
  )

  const openEditDialog = useCallback(
    (tripId: number) => setDialogParams({ newTrip: null, tripId: String(tripId) }),
    [setDialogParams],
  )

  const closeDialog = useCallback(
    () => setDialogParams({ newTrip: null, tripId: null }),
    [setDialogParams],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        tripType: null,
        status: '',
        visibility: '',
        mine: false,
        page: 1,
      }),
    [commit, state],
  )

  const listQuery = useMemo(
    () => toListQuery(state, currentUserId),
    [state, currentUserId],
  )

  return {
    state,
    dialog,
    listQuery,
    setFilters,
    setSort,
    setPage,
    setPageSize,
    openCreateDialog,
    openEditDialog,
    closeDialog,
    clearFilters,
  }
}

export function activeTripFilterCount(state: TripsState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.tripType != null) count += 1
  if (state.status !== '') count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
