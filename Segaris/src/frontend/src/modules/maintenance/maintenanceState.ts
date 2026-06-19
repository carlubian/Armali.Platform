import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  maintenancePageSizes,
  type MaintenancePageSize,
  type MaintenancePriority,
  type MaintenanceSortDirection,
  type MaintenanceSortField,
  type MaintenanceStatus,
  type MaintenanceTaskListQuery,
  type MaintenanceVisibility,
} from '@/app/api/maintenance'

const sortFields: readonly MaintenanceSortField[] = [
  'title',
  'type',
  'status',
  'priority',
  'dueDate',
  'visibility',
]
const statuses: readonly MaintenanceStatus[] = [
  'Pending',
  'InProgress',
  'Completed',
  'Cancelled',
]
const priorities: readonly MaintenancePriority[] = ['Low', 'Medium', 'High']
const visibilities: readonly MaintenanceVisibility[] = ['Public', 'Private']

export const defaultSort: MaintenanceSortField = 'dueDate'
export const defaultSortDirection: MaintenanceSortDirection = 'asc'
export const defaultPageSize: MaintenancePageSize = 25

export interface MaintenanceState {
  search: string
  type: number | null
  status: MaintenanceStatus | ''
  priority: MaintenancePriority | ''
  asset: number | null
  visibility: MaintenanceVisibility | ''
  mine: boolean
  sort: MaintenanceSortField
  sortDirection: MaintenanceSortDirection
  page: number
  pageSize: MaintenancePageSize
}

export type MaintenanceFilterPatch = Partial<
  Omit<MaintenanceState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

export type MaintenanceDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; taskId: number }

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

export function parseMaintenanceState(
  params: URLSearchParams,
  currentUserId: number | null,
): MaintenanceState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    type: intOrNull(params.get('type')),
    status: oneOf(params.get('status'), statuses),
    priority: oneOf(params.get('priority'), priorities),
    asset: intOrNull(params.get('asset')),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (maintenancePageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as MaintenancePageSize)
      : defaultPageSize,
  }
}

export function parseMaintenanceDialogState(
  params: URLSearchParams,
): MaintenanceDialogState {
  if (params.get('newTask') === 'true') return { mode: 'create' }
  const taskId = intOrNull(params.get('taskId'))
  return taskId == null ? { mode: 'closed' } : { mode: 'edit', taskId }
}

export function toListQuery(
  state: MaintenanceState,
  currentUserId: number | null,
): MaintenanceTaskListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    type: state.type,
    status: state.status === '' ? null : state.status,
    priority: state.priority === '' ? null : state.priority,
    asset: state.asset,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(
  state: MaintenanceState,
  currentUserId: number | null,
): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('type', state.type)
  set('status', state.status === '' ? null : state.status)
  set('priority', state.priority === '' ? null : state.priority)
  set('asset', state.asset)
  set('visibility', state.visibility === '' ? null : state.visibility)
  set('creator', state.mine && currentUserId != null ? currentUserId : null)
  if (state.sort !== defaultSort) set('sort', state.sort)
  if (state.sortDirection !== defaultSortDirection) {
    set('sortDirection', state.sortDirection)
  }
  if (state.page !== 1) set('page', state.page)
  if (state.pageSize !== defaultPageSize) set('pageSize', state.pageSize)

  return params
}

const LIST_PARAMS = new Set([
  'search',
  'type',
  'status',
  'priority',
  'asset',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export function useMaintenanceState(currentUserId: number | null) {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseMaintenanceState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(
    () => parseMaintenanceDialogState(searchParams),
    [searchParams],
  )

  const setDialogParams = useCallback(
    (patch: { newTask?: string | null; taskId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newTask == null) next.delete('newTask')
        else next.set('newTask', patch.newTask)
        if (patch.taskId == null) next.delete('taskId')
        else next.set('taskId', patch.taskId)
        return next
      })
    },
    [setSearchParams],
  )

  const commit = useCallback(
    (next: MaintenanceState, options?: { replace?: boolean }) => {
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
    (patch: MaintenanceFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: MaintenanceSortField) => {
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
    (pageSize: MaintenancePageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        type: null,
        status: '',
        priority: '',
        asset: null,
        visibility: '',
        mine: false,
        page: 1,
      }),
    [commit, state],
  )

  return {
    state,
    dialog,
    listQuery: toListQuery(state, currentUserId),
    setFilters,
    setSort,
    setPage,
    setPageSize,
    clearFilters,
    openCreateDialog: () => setDialogParams({ newTask: 'true', taskId: null }),
    openEditDialog: (taskId: number) =>
      setDialogParams({ newTask: null, taskId: String(taskId) }),
    closeDialog: () => setDialogParams({ newTask: null, taskId: null }),
  }
}

export function activeMaintenanceFilterCount(state: MaintenanceState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.type != null) count += 1
  if (state.status !== '') count += 1
  if (state.priority !== '') count += 1
  if (state.asset != null) count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
