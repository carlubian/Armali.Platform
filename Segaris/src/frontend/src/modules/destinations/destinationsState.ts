import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  destinationPageSizes,
  type DestinationListQuery,
  type DestinationPageSize,
  type DestinationSortDirection,
  type DestinationSortField,
} from '@/app/api/destinations'

const sortFields: readonly DestinationSortField[] = ['name', 'category']

export const defaultDestinationSort: DestinationSortField = 'name'
export const defaultDestinationSortDirection: DestinationSortDirection = 'asc'
export const defaultDestinationPageSize: DestinationPageSize = 25

export interface DestinationsState {
  search: string
  category: number | null
  isSchengenArea: boolean | null
  sort: DestinationSortField
  sortDirection: DestinationSortDirection
  page: number
  pageSize: DestinationPageSize
}

export type DestinationsFilterPatch = Partial<
  Omit<DestinationsState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

export type DestinationDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; destinationId: number }

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

function boolOrNull(value: string | null): boolean | null {
  if (value === 'true') return true
  if (value === 'false') return false
  return null
}

export function parseDestinationsState(params: URLSearchParams): DestinationsState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)

  return {
    search: params.get('search') ?? '',
    category: intOrNull(params.get('category')),
    isSchengenArea: boolOrNull(params.get('isSchengenArea')),
    sort: sort === '' ? defaultDestinationSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc'
        ? direction
        : defaultDestinationSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (destinationPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as DestinationPageSize)
      : defaultDestinationPageSize,
  }
}

export function parseDestinationDialogState(
  params: URLSearchParams,
): DestinationDialogState {
  if (params.get('newDestination') === 'true') return { mode: 'create' }
  const destinationId = intOrNull(params.get('destinationId'))
  return destinationId == null ? { mode: 'closed' } : { mode: 'edit', destinationId }
}

export function toDestinationListQuery(state: DestinationsState): DestinationListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    isSchengenArea: state.isSchengenArea,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(state: DestinationsState): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | boolean | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('category', state.category)
  set('isSchengenArea', state.isSchengenArea)
  if (state.sort !== defaultDestinationSort) set('sort', state.sort)
  if (state.sortDirection !== defaultDestinationSortDirection)
    set('sortDirection', state.sortDirection)
  if (state.page !== 1) set('page', state.page)
  if (state.pageSize !== defaultDestinationPageSize) set('pageSize', state.pageSize)

  return params
}

const LIST_PARAMS = new Set([
  'search',
  'category',
  'isSchengenArea',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export interface UseDestinationsState {
  state: DestinationsState
  dialog: DestinationDialogState
  listQuery: DestinationListQuery
  setFilters: (patch: DestinationsFilterPatch) => void
  setSort: (sort: DestinationSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: DestinationPageSize) => void
  clearFilters: () => void
  openCreateDialog: () => void
  openEditDialog: (destinationId: number) => void
  closeDialog: () => void
}

export function useDestinationsState(): UseDestinationsState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(() => parseDestinationsState(searchParams), [searchParams])
  const dialog = useMemo(
    () => parseDestinationDialogState(searchParams),
    [searchParams],
  )

  const commit = useCallback(
    (next: DestinationsState, options?: { replace?: boolean }) => {
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
    (patch: DestinationsFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: DestinationSortField) => {
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
    (pageSize: DestinationPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        category: null,
        isSchengenArea: null,
        page: 1,
      }),
    [commit, state],
  )

  const listQuery = useMemo(() => toDestinationListQuery(state), [state])

  const setDialogParams = useCallback(
    (patch: { newDestination?: string | null; destinationId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newDestination == null) next.delete('newDestination')
        else next.set('newDestination', patch.newDestination)
        if (patch.destinationId == null) next.delete('destinationId')
        else next.set('destinationId', patch.destinationId)
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
    openCreateDialog: () =>
      setDialogParams({ newDestination: 'true', destinationId: null }),
    openEditDialog: (destinationId: number) =>
      setDialogParams({ newDestination: null, destinationId: String(destinationId) }),
    closeDialog: () => setDialogParams({ newDestination: null, destinationId: null }),
  }
}
