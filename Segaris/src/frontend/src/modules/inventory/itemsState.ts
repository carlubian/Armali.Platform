import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  inventoryPageSizes,
  type InventoryItemListQuery,
  type InventoryItemSortField,
  type InventoryItemStatus,
  type InventoryPageSize,
  type InventorySortDirection,
  type InventoryVisibility,
} from '@/app/api/inventory'

const sortFields: readonly InventoryItemSortField[] = [
  'name',
  'status',
  'category',
  'location',
  'currentStock',
  'minimumStock',
  'visibility',
]
const statuses: readonly InventoryItemStatus[] = ['Candidate', 'Active', 'Deprecated']
const visibilities: readonly InventoryVisibility[] = ['Public', 'Private']

export const defaultSort: InventoryItemSortField = 'name'
export const defaultSortDirection: InventorySortDirection = 'asc'
export const defaultPageSize: InventoryPageSize = 25

export interface ItemsState {
  search: string
  status: InventoryItemStatus | ''
  category: number | null
  location: number | null
  supplier: number | null
  visibility: InventoryVisibility | ''
  mine: boolean
  sort: InventoryItemSortField
  sortDirection: InventorySortDirection
  page: number
  pageSize: InventoryPageSize
}

export type ItemsFilterPatch = Partial<
  Omit<ItemsState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

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

function parseState(params: URLSearchParams, currentUserId: number | null): ItemsState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    status: oneOf(params.get('status'), statuses),
    category: intOrNull(params.get('category')),
    location: intOrNull(params.get('location')),
    supplier: intOrNull(params.get('supplier')),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (inventoryPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as InventoryPageSize)
      : defaultPageSize,
  }
}

export function toListQuery(
  state: ItemsState,
  currentUserId: number | null,
): InventoryItemListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    status: state.status === '' ? null : state.status,
    category: state.category,
    location: state.location,
    supplier: state.supplier,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(state: ItemsState, currentUserId: number | null): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('status', state.status === '' ? null : state.status)
  set('category', state.category)
  set('location', state.location)
  set('supplier', state.supplier)
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
  'status',
  'category',
  'location',
  'supplier',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export interface UseItemsState {
  state: ItemsState
  listQuery: InventoryItemListQuery
  setFilters: (patch: ItemsFilterPatch) => void
  setSort: (sort: InventoryItemSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: InventoryPageSize) => void
  clearFilters: () => void
}

export function useItemsState(currentUserId: number | null): UseItemsState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )

  const commit = useCallback(
    (next: ItemsState, options?: { replace?: boolean }) => {
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
    (patch: ItemsFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: InventoryItemSortField) => {
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
    (pageSize: InventoryPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        status: '',
        category: null,
        location: null,
        supplier: null,
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

  return { state, listQuery, setFilters, setSort, setPage, setPageSize, clearFilters }
}

export function activeItemFilterCount(state: ItemsState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.status !== '') count += 1
  if (state.category != null) count += 1
  if (state.location != null) count += 1
  if (state.supplier != null) count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
