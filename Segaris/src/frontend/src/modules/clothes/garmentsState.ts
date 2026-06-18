import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  clothesPageSizes,
  type ClothesGarmentListQuery,
  type ClothesGarmentSortField,
  type ClothesGarmentStatus,
  type ClothesPageSize,
  type ClothesSortDirection,
  type ClothesVisibility,
} from '@/app/api/clothes'

const sortFields: readonly ClothesGarmentSortField[] = [
  'name',
  'category',
  'status',
  'visibility',
]
const statuses: readonly ClothesGarmentStatus[] = [
  'Active',
  'Unavailable',
  'Deprecated',
]
const visibilities: readonly ClothesVisibility[] = ['Public', 'Private']

export const defaultSort: ClothesGarmentSortField = 'name'
export const defaultSortDirection: ClothesSortDirection = 'asc'
export const defaultPageSize: ClothesPageSize = 25

export interface GarmentsState {
  search: string
  category: number | null
  status: ClothesGarmentStatus | ''
  color: number | null
  visibility: ClothesVisibility | ''
  mine: boolean
  sort: ClothesGarmentSortField
  sortDirection: ClothesSortDirection
  page: number
  pageSize: ClothesPageSize
}

export type GarmentsFilterPatch = Partial<
  Omit<GarmentsState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

export type GarmentDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; garmentId: number }

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

export function parseGarmentsState(
  params: URLSearchParams,
  currentUserId: number | null,
): GarmentsState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    category: intOrNull(params.get('category')),
    status: oneOf(params.get('status'), statuses),
    color: intOrNull(params.get('color')),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (clothesPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as ClothesPageSize)
      : defaultPageSize,
  }
}

export function parseGarmentDialogState(params: URLSearchParams): GarmentDialogState {
  if (params.get('newGarment') === 'true') return { mode: 'create' }
  const garmentId = intOrNull(params.get('garmentId'))
  return garmentId == null ? { mode: 'closed' } : { mode: 'edit', garmentId }
}

export function toListQuery(
  state: GarmentsState,
  currentUserId: number | null,
): ClothesGarmentListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    status: state.status === '' ? null : state.status,
    color: state.color,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(
  state: GarmentsState,
  currentUserId: number | null,
): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('category', state.category)
  set('status', state.status === '' ? null : state.status)
  set('color', state.color)
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
  'category',
  'status',
  'color',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export interface UseGarmentsState {
  state: GarmentsState
  dialog: GarmentDialogState
  listQuery: ClothesGarmentListQuery
  setFilters: (patch: GarmentsFilterPatch) => void
  setSort: (sort: ClothesGarmentSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: ClothesPageSize) => void
  clearFilters: () => void
  openCreateDialog: () => void
  openEditDialog: (garmentId: number) => void
  closeDialog: () => void
}

export function useGarmentsState(currentUserId: number | null): UseGarmentsState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseGarmentsState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(
    () => parseGarmentDialogState(searchParams),
    [searchParams],
  )

  const commit = useCallback(
    (next: GarmentsState, options?: { replace?: boolean }) => {
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
    (patch: GarmentsFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: ClothesGarmentSortField) => {
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
    (pageSize: ClothesPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        category: null,
        status: '',
        color: null,
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

  const setDialogParams = useCallback(
    (patch: { newGarment?: string | null; garmentId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newGarment == null) next.delete('newGarment')
        else next.set('newGarment', patch.newGarment)
        if (patch.garmentId == null) next.delete('garmentId')
        else next.set('garmentId', patch.garmentId)
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
    openCreateDialog: () => setDialogParams({ newGarment: 'true', garmentId: null }),
    openEditDialog: (garmentId: number) =>
      setDialogParams({ newGarment: null, garmentId: String(garmentId) }),
    closeDialog: () => setDialogParams({ newGarment: null, garmentId: null }),
  }
}

export function activeGarmentFilterCount(state: GarmentsState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.category != null) count += 1
  if (state.status !== '') count += 1
  if (state.color != null) count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
