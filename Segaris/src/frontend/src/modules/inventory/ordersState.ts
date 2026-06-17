import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  inventoryPageSizes,
  type InventoryOrderListQuery,
  type InventoryOrderSortField,
  type InventoryOrderStatus,
  type InventoryPageSize,
  type InventorySortDirection,
  type InventoryVisibility,
} from '@/app/api/inventory'

const sortFields: readonly InventoryOrderSortField[] = [
  'supplier',
  'status',
  'orderDate',
  'expectedReceiptDate',
  'currency',
  'visibility',
]
const statuses: readonly InventoryOrderStatus[] = [
  'Planning',
  'Active',
  'Received',
  'Cancelled',
]
const visibilities: readonly InventoryVisibility[] = ['Public', 'Private']

export const defaultSort: InventoryOrderSortField = 'orderDate'
export const defaultSortDirection: InventorySortDirection = 'desc'
export const defaultPageSize: InventoryPageSize = 25

export interface OrdersState {
  search: string
  supplier: number | null
  status: InventoryOrderStatus | ''
  currency: number | null
  visibility: InventoryVisibility | ''
  mine: boolean
  sort: InventoryOrderSortField
  sortDirection: InventorySortDirection
  page: number
  pageSize: InventoryPageSize
}

export type OrdersFilterPatch = Partial<
  Omit<OrdersState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

// Order list parameters are namespaced with an `o` prefix so the items and
// orders tables keep independent, non-colliding URL state on the same route.
const KEYS = {
  search: 'oSearch',
  supplier: 'oSupplier',
  status: 'oStatus',
  currency: 'oCurrency',
  visibility: 'oVisibility',
  creator: 'oCreator',
  sort: 'oSort',
  sortDirection: 'oSortDirection',
  page: 'oPage',
  pageSize: 'oPageSize',
} as const

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

function parseState(
  params: URLSearchParams,
  currentUserId: number | null,
): OrdersState {
  const sort = oneOf(params.get(KEYS.sort), sortFields)
  const direction = params.get(KEYS.sortDirection)
  const pageSizeRaw = Number.parseInt(params.get(KEYS.pageSize) ?? '', 10)
  const pageRaw = Number.parseInt(params.get(KEYS.page) ?? '', 10)
  const creator = intOrNull(params.get(KEYS.creator))

  return {
    search: params.get(KEYS.search) ?? '',
    supplier: intOrNull(params.get(KEYS.supplier)),
    status: oneOf(params.get(KEYS.status), statuses),
    currency: intOrNull(params.get(KEYS.currency)),
    visibility: oneOf(params.get(KEYS.visibility), visibilities),
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
  state: OrdersState,
  currentUserId: number | null,
): InventoryOrderListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    supplier: state.supplier,
    status: state.status === '' ? null : state.status,
    currency: state.currency,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(state: OrdersState, currentUserId: number | null): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set(KEYS.search, state.search.trim() === '' ? null : state.search.trim())
  set(KEYS.supplier, state.supplier)
  set(KEYS.status, state.status === '' ? null : state.status)
  set(KEYS.currency, state.currency)
  set(KEYS.visibility, state.visibility === '' ? null : state.visibility)
  set(KEYS.creator, state.mine && currentUserId != null ? currentUserId : null)
  if (state.sort !== defaultSort) set(KEYS.sort, state.sort)
  if (state.sortDirection !== defaultSortDirection)
    set(KEYS.sortDirection, state.sortDirection)
  if (state.page !== 1) set(KEYS.page, state.page)
  if (state.pageSize !== defaultPageSize) set(KEYS.pageSize, state.pageSize)

  return params
}

const LIST_PARAMS = new Set<string>(Object.values(KEYS))

export interface UseOrdersState {
  state: OrdersState
  listQuery: InventoryOrderListQuery
  setFilters: (patch: OrdersFilterPatch) => void
  setSort: (sort: InventoryOrderSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: InventoryPageSize) => void
  clearFilters: () => void
}

export function useOrdersState(currentUserId: number | null): UseOrdersState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )

  const commit = useCallback(
    (next: OrdersState, options?: { replace?: boolean }) => {
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
    (patch: OrdersFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: InventoryOrderSortField) => {
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
        supplier: null,
        status: '',
        currency: null,
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

export function activeOrderFilterCount(state: OrdersState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.supplier != null) count += 1
  if (state.status !== '') count += 1
  if (state.currency != null) count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
