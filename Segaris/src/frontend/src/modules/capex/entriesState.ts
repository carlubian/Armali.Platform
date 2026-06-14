import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  capexPageSizes,
  type CapexEntryListQuery,
  type CapexEntrySortField,
  type CapexEntryStatus,
  type CapexMovementType,
  type CapexPageSize,
  type CapexVisibility,
  type SortDirection,
} from '@/app/api/capex'

const sortFields: readonly CapexEntrySortField[] = [
  'title',
  'type',
  'status',
  'dueDate',
  'category',
  'supplier',
  'costCenter',
  'total',
  'currency',
]
const movementTypes: readonly CapexMovementType[] = ['Income', 'Expense']
const statuses: readonly CapexEntryStatus[] = ['Planning', 'Completed', 'Canceled']
const visibilities: readonly CapexVisibility[] = ['Public', 'Private']

export const defaultSort: CapexEntrySortField = 'dueDate'
export const defaultSortDirection: SortDirection = 'desc'
export const defaultPageSize: CapexPageSize = 25

/** Normalized, URL-backed state driving the Entries query. */
export interface EntriesState {
  search: string
  from: string
  to: string
  type: CapexMovementType | ''
  status: CapexEntryStatus | ''
  category: number | null
  supplier: number | null
  costCenter: number | null
  currency: number | null
  visibility: CapexVisibility | ''
  /** True when filtering to the current user's own entries. */
  mine: boolean
  sort: CapexEntrySortField
  sortDirection: SortDirection
  page: number
  pageSize: CapexPageSize
}

/** Filter fields that, when changed, reset pagination back to the first page. */
export type EntriesFilterPatch = Partial<
  Omit<EntriesState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
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

function parseState(
  params: URLSearchParams,
  currentUserId: number | null,
): EntriesState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    from: params.get('from') ?? '',
    to: params.get('to') ?? '',
    type: oneOf(params.get('type'), movementTypes),
    status: oneOf(params.get('status'), statuses),
    category: intOrNull(params.get('category')),
    supplier: intOrNull(params.get('supplier')),
    costCenter: intOrNull(params.get('costCenter')),
    currency: intOrNull(params.get('currency')),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (capexPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as CapexPageSize)
      : defaultPageSize,
  }
}

/** Build the request body the API client expects from the normalized state. */
export function toListQuery(
  state: EntriesState,
  currentUserId: number | null,
): CapexEntryListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    from: state.from === '' ? null : state.from,
    to: state.to === '' ? null : state.to,
    type: state.type === '' ? null : state.type,
    status: state.status === '' ? null : state.status,
    category: state.category,
    supplier: state.supplier,
    costCenter: state.costCenter,
    currency: state.currency,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(
  state: EntriesState,
  currentUserId: number | null,
): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('from', state.from === '' ? null : state.from)
  set('to', state.to === '' ? null : state.to)
  set('type', state.type === '' ? null : state.type)
  set('status', state.status === '' ? null : state.status)
  set('category', state.category)
  set('supplier', state.supplier)
  set('costCenter', state.costCenter)
  set('currency', state.currency)
  set('visibility', state.visibility === '' ? null : state.visibility)
  set('creator', state.mine && currentUserId != null ? currentUserId : null)
  // Persist sort/page only when they diverge from the defaults to keep links clean.
  if (state.sort !== defaultSort) set('sort', state.sort)
  if (state.sortDirection !== defaultSortDirection)
    set('sortDirection', state.sortDirection)
  if (state.page !== 1) set('page', state.page)
  if (state.pageSize !== defaultPageSize) set('pageSize', state.pageSize)

  return params
}

export interface UseEntriesState {
  state: EntriesState
  listQuery: CapexEntryListQuery
  setFilters: (patch: EntriesFilterPatch) => void
  setSort: (sort: CapexEntrySortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: CapexPageSize) => void
  clearFilters: () => void
}

/**
 * Reads and writes the Entries query state through the URL search parameters so
 * the table is linkable and survives the editor dialog opening and closing.
 * Parameters unrelated to the list (such as a future `entryId`) are preserved.
 */
export function useEntriesState(currentUserId: number | null): UseEntriesState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )

  const commit = useCallback(
    (next: EntriesState, options?: { replace?: boolean }) => {
      setSearchParams(
        (current) => {
          const listParams = writeState(next, currentUserId)
          const merged = new URLSearchParams(listParams)
          // Preserve non-list params (e.g. the editor dialog's entryId/new).
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
    (patch: EntriesFilterPatch) => {
      // Any filter change returns to the first page; searching replaces history
      // so rapid typing does not flood the back button.
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: CapexEntrySortField) => {
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
    (pageSize: CapexPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        from: '',
        to: '',
        type: '',
        status: '',
        category: null,
        supplier: null,
        costCenter: null,
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

const LIST_PARAMS = new Set([
  'search',
  'from',
  'to',
  'type',
  'status',
  'category',
  'supplier',
  'costCenter',
  'currency',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

/** The editor dialog's URL-driven mode, derived from `new` and `entryId`. */
export type EntryDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; entryId: number }

export interface UseEntryDialog {
  dialog: EntryDialogState
  openCreate: () => void
  openEntry: (entryId: number) => void
  close: () => void
}

/**
 * Reads and writes the editor dialog through the `new`/`entryId` search params,
 * always preserving the Entries list state so opening and closing the dialog
 * returns to the same table page, filters, sort, and search.
 */
export function useEntryDialog(): UseEntryDialog {
  const [searchParams, setSearchParams] = useSearchParams()

  const dialog = useMemo<EntryDialogState>(() => {
    if (searchParams.get('new') === 'true') return { mode: 'create' }
    const entryId = Number.parseInt(searchParams.get('entryId') ?? '', 10)
    if (Number.isFinite(entryId) && entryId > 0) return { mode: 'edit', entryId }
    return { mode: 'closed' }
  }, [searchParams])

  const openCreate = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.delete('entryId')
      next.set('new', 'true')
      return next
    })
  }, [setSearchParams])

  const openEntry = useCallback(
    (entryId: number) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        next.delete('new')
        next.set('entryId', String(entryId))
        return next
      })
    },
    [setSearchParams],
  )

  const close = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.delete('new')
      next.delete('entryId')
      return next
    })
  }, [setSearchParams])

  return { dialog, openCreate, openEntry, close }
}

/** Count of filters that are currently narrowing the result set. */
export function activeFilterCount(state: EntriesState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.from !== '') count += 1
  if (state.to !== '') count += 1
  if (state.type !== '') count += 1
  if (state.status !== '') count += 1
  if (state.category != null) count += 1
  if (state.supplier != null) count += 1
  if (state.costCenter != null) count += 1
  if (state.currency != null) count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
