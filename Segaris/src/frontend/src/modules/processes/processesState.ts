import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  processPageSizes,
  type ProcessListQuery,
  type ProcessPageSize,
  type ProcessSortDirection,
  type ProcessSortField,
  type ProcessStatus,
  type ProcessVisibility,
} from '@/app/api/processes'

const sortFields: readonly ProcessSortField[] = [
  'name',
  'category',
  'status',
  'dueDate',
  'visibility',
]
const statuses: readonly ProcessStatus[] = [
  'NotStarted',
  'InProgress',
  'Completed',
  'Cancelled',
]
const visibilities: readonly ProcessVisibility[] = ['Public', 'Private']

export const defaultSort: ProcessSortField = 'dueDate'
export const defaultSortDirection: ProcessSortDirection = 'asc'
export const defaultPageSize: ProcessPageSize = 25

export interface ProcessesState {
  search: string
  category: number | null
  status: ProcessStatus | ''
  visibility: ProcessVisibility | ''
  mine: boolean
  sort: ProcessSortField
  sortDirection: ProcessSortDirection
  page: number
  pageSize: ProcessPageSize
}

export type ProcessesFilterPatch = Partial<
  Omit<ProcessesState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

/**
 * Dialog state is layered on the table state via temporary URL flags so the table
 * survives dialog open and close without a reload. The step-timeline popup
 * (`steps=true`) opens over a selected process.
 */
export type ProcessesDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; processId: number }
  | { mode: 'steps'; processId: number }
  | { mode: 'restructure'; processId: number }

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

export function parseProcessesState(
  params: URLSearchParams,
  currentUserId: number | null,
): ProcessesState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    category: intOrNull(params.get('category')),
    status: oneOf(params.get('status'), statuses),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (processPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as ProcessPageSize)
      : defaultPageSize,
  }
}

export function parseProcessesDialogState(
  params: URLSearchParams,
): ProcessesDialogState {
  if (params.get('newProcess') === 'true') return { mode: 'create' }
  const processId = intOrNull(params.get('processId'))
  if (processId == null) return { mode: 'closed' }
  if (params.get('steps') === 'true' && params.get('restructure') === 'true') {
    return { mode: 'restructure', processId }
  }
  if (params.get('steps') === 'true') return { mode: 'steps', processId }
  return { mode: 'edit', processId }
}

export function toListQuery(
  state: ProcessesState,
  currentUserId: number | null,
): ProcessListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    status: state.status === '' ? null : state.status,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(
  state: ProcessesState,
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
  'category',
  'status',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export function useProcessesState(currentUserId: number | null) {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseProcessesState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(() => parseProcessesDialogState(searchParams), [searchParams])

  const setDialogParams = useCallback(
    (patch: {
      newProcess?: string | null
      processId?: string | null
      steps?: string | null
      restructure?: string | null
    }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        for (const key of [
          'newProcess',
          'processId',
          'steps',
          'restructure',
        ] as const) {
          const value = patch[key]
          if (value == null) next.delete(key)
          else next.set(key, value)
        }
        return next
      })
    },
    [setSearchParams],
  )

  const commit = useCallback(
    (next: ProcessesState, options?: { replace?: boolean }) => {
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
    (patch: ProcessesFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: ProcessSortField) => {
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
    (pageSize: ProcessPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        category: null,
        status: '',
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
    openCreateDialog: () =>
      setDialogParams({
        newProcess: 'true',
        processId: null,
        steps: null,
        restructure: null,
      }),
    openEditDialog: (processId: number) =>
      setDialogParams({
        newProcess: null,
        processId: String(processId),
        steps: null,
        restructure: null,
      }),
    openStepsDialog: (processId: number) =>
      setDialogParams({
        newProcess: null,
        processId: String(processId),
        steps: 'true',
        restructure: null,
      }),
    openRestructureDialog: (processId: number) =>
      setDialogParams({
        newProcess: null,
        processId: String(processId),
        steps: 'true',
        restructure: 'true',
      }),
    closeDialog: () =>
      setDialogParams({
        newProcess: null,
        processId: null,
        steps: null,
        restructure: null,
      }),
  }
}

export function activeProcessesFilterCount(state: ProcessesState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.category != null) count += 1
  if (state.status !== '') count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
