import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  opexPageSizes,
  type OpexContractListQuery,
  type OpexContractSortField,
  type OpexContractStatus,
  type OpexExpectedFrequency,
  type OpexMovementType,
  type OpexPageSize,
  type OpexSortDirection,
  type OpexVisibility,
} from '@/app/api/opex'

const sortFields: readonly OpexContractSortField[] = [
  'name',
  'type',
  'status',
  'category',
  'supplier',
  'frequency',
  'estimatedAnnualAmount',
  'realizedCurrentYearAmount',
  'currency',
]
const movementTypes: readonly OpexMovementType[] = ['Income', 'Expense']
const statuses: readonly OpexContractStatus[] = ['Planning', 'Active', 'OnHold', 'Closed']
const frequencies: readonly OpexExpectedFrequency[] = [
  'None',
  'Weekly',
  'Monthly',
  'Quarterly',
  'SemiAnnual',
  'Annual',
  'Irregular',
]
const visibilities: readonly OpexVisibility[] = ['Public', 'Private']

export const defaultSort: OpexContractSortField = 'name'
export const defaultSortDirection: OpexSortDirection = 'asc'
export const defaultPageSize: OpexPageSize = 25

export interface ContractsState {
  search: string
  type: OpexMovementType | ''
  status: OpexContractStatus | ''
  frequency: OpexExpectedFrequency | ''
  category: number | null
  supplier: number | null
  costCenter: number | null
  currency: number | null
  visibility: OpexVisibility | ''
  mine: boolean
  sort: OpexContractSortField
  sortDirection: OpexSortDirection
  page: number
  pageSize: OpexPageSize
}

export type ContractsFilterPatch = Partial<
  Omit<ContractsState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
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
): ContractsState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    type: oneOf(params.get('type'), movementTypes),
    status: oneOf(params.get('status'), statuses),
    frequency: oneOf(params.get('frequency'), frequencies),
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
    pageSize: (opexPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as OpexPageSize)
      : defaultPageSize,
  }
}

export function toListQuery(
  state: ContractsState,
  currentUserId: number | null,
): OpexContractListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    type: state.type === '' ? null : state.type,
    status: state.status === '' ? null : state.status,
    frequency: state.frequency === '' ? null : state.frequency,
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
  state: ContractsState,
  currentUserId: number | null,
): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', state.search.trim() === '' ? null : state.search.trim())
  set('type', state.type === '' ? null : state.type)
  set('status', state.status === '' ? null : state.status)
  set('frequency', state.frequency === '' ? null : state.frequency)
  set('category', state.category)
  set('supplier', state.supplier)
  set('costCenter', state.costCenter)
  set('currency', state.currency)
  set('visibility', state.visibility === '' ? null : state.visibility)
  set('creator', state.mine && currentUserId != null ? currentUserId : null)
  if (state.sort !== defaultSort) set('sort', state.sort)
  if (state.sortDirection !== defaultSortDirection) set('sortDirection', state.sortDirection)
  if (state.page !== 1) set('page', state.page)
  if (state.pageSize !== defaultPageSize) set('pageSize', state.pageSize)

  return params
}

export interface UseContractsState {
  state: ContractsState
  listQuery: OpexContractListQuery
  setFilters: (patch: ContractsFilterPatch) => void
  setSort: (sort: OpexContractSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: OpexPageSize) => void
  clearFilters: () => void
}

export function useContractsState(currentUserId: number | null): UseContractsState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )

  const commit = useCallback(
    (next: ContractsState, options?: { replace?: boolean }) => {
      setSearchParams(
        (current) => {
          const listParams = writeState(next, currentUserId)
          const merged = new URLSearchParams(listParams)
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
    (patch: ContractsFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: OpexContractSortField) => {
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
    (pageSize: OpexPageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        type: '',
        status: '',
        frequency: '',
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
  'type',
  'status',
  'frequency',
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

export type ContractDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; contractId: number }

export interface UseContractDialog {
  dialog: ContractDialogState
  openCreate: () => void
  openContract: (contractId: number) => void
  close: () => void
}

export function useContractDialog(): UseContractDialog {
  const [searchParams, setSearchParams] = useSearchParams()

  const dialog = useMemo<ContractDialogState>(() => {
    if (searchParams.get('new') === 'true') return { mode: 'create' }
    const contractId = Number.parseInt(searchParams.get('contractId') ?? '', 10)
    if (Number.isFinite(contractId) && contractId > 0) return { mode: 'edit', contractId }
    return { mode: 'closed' }
  }, [searchParams])

  const openCreate = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.delete('contractId')
      next.set('new', 'true')
      return next
    })
  }, [setSearchParams])

  const openContract = useCallback(
    (contractId: number) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        next.delete('new')
        next.set('contractId', String(contractId))
        return next
      })
    },
    [setSearchParams],
  )

  const close = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.delete('new')
      next.delete('contractId')
      return next
    })
  }, [setSearchParams])

  return { dialog, openCreate, openContract, close }
}

export function activeFilterCount(state: ContractsState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.type !== '') count += 1
  if (state.status !== '') count += 1
  if (state.frequency !== '') count += 1
  if (state.category != null) count += 1
  if (state.supplier != null) count += 1
  if (state.costCenter != null) count += 1
  if (state.currency != null) count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
