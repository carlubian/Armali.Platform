import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  firebirdPageSizes,
  firebirdPersonStatuses,
  firebirdVisibilities,
  type FirebirdPageSize,
  type FirebirdPersonSortField,
  type FirebirdPersonStatus,
  type FirebirdSortDirection,
  type FirebirdVisibility,
  type PersonListQuery,
} from '@/app/api/firebird'

const sortFields: readonly FirebirdPersonSortField[] = [
  'name',
  'category',
  'status',
  'birthday',
  'visibility',
]

export const defaultSort: FirebirdPersonSortField = 'name'
export const defaultSortDirection: FirebirdSortDirection = 'asc'
export const defaultPageSize: FirebirdPageSize = 25

export interface PeopleState {
  search: string
  category: number | null
  status: FirebirdPersonStatus | ''
  visibility: FirebirdVisibility | ''
  mine: boolean
  sort: FirebirdPersonSortField
  sortDirection: FirebirdSortDirection
  page: number
  pageSize: FirebirdPageSize
}

export type PeopleFilterPatch = Partial<
  Omit<PeopleState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

export type PersonDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; personId: number }
  | { mode: 'usernames'; personId: number; returnToEdit: boolean }
  | { mode: 'interactions'; personId: number; returnToEdit: boolean }

export interface OpenSubEntityDialogOptions {
  returnToEdit?: boolean
}

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

export function parsePeopleState(
  params: URLSearchParams,
  currentUserId: number | null,
): PeopleState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    category: intOrNull(params.get('category')),
    status: oneOf(params.get('status'), firebirdPersonStatuses),
    visibility: oneOf(params.get('visibility'), firebirdVisibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (firebirdPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as FirebirdPageSize)
      : defaultPageSize,
  }
}

export function parsePersonDialogState(params: URLSearchParams): PersonDialogState {
  if (params.get('newPerson') === 'true') return { mode: 'create' }

  const personId = intOrNull(params.get('personId'))
  if (personId == null) return { mode: 'closed' }
  const returnToEdit = params.get('returnTo') === 'edit'
  if (params.get('usernames') === 'true')
    return { mode: 'usernames', personId, returnToEdit }
  if (params.get('interactions') === 'true')
    return { mode: 'interactions', personId, returnToEdit }
  return { mode: 'edit', personId }
}

export function toListQuery(
  state: PeopleState,
  currentUserId: number | null,
): PersonListQuery {
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

function writeState(state: PeopleState, currentUserId: number | null): URLSearchParams {
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
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export interface UsePeopleState {
  state: PeopleState
  dialog: PersonDialogState
  listQuery: PersonListQuery
  setFilters: (patch: PeopleFilterPatch) => void
  setSort: (sort: FirebirdPersonSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: FirebirdPageSize) => void
  clearFilters: () => void
  openCreateDialog: () => void
  openEditDialog: (personId: number) => void
  openUsernamesDialog: (personId: number, options?: OpenSubEntityDialogOptions) => void
  openInteractionsDialog: (
    personId: number,
    options?: OpenSubEntityDialogOptions,
  ) => void
  closeDialog: () => void
}

export function usePeopleState(currentUserId: number | null): UsePeopleState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parsePeopleState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(() => parsePersonDialogState(searchParams), [searchParams])

  const commit = useCallback(
    (next: PeopleState, options?: { replace?: boolean }) => {
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
    (patch: PeopleFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: FirebirdPersonSortField) => {
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
    (pageSize: FirebirdPageSize) => commit({ ...state, pageSize, page: 1 }),
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

  const listQuery = useMemo(
    () => toListQuery(state, currentUserId),
    [state, currentUserId],
  )

  const setDialogParams = useCallback(
    (patch: {
      newPerson?: string | null
      personId?: string | null
      usernames?: string | null
      interactions?: string | null
      returnTo?: string | null
    }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newPerson == null) next.delete('newPerson')
        else next.set('newPerson', patch.newPerson)
        if (patch.personId == null) next.delete('personId')
        else next.set('personId', patch.personId)
        if (patch.usernames == null) next.delete('usernames')
        else next.set('usernames', patch.usernames)
        if (patch.interactions == null) next.delete('interactions')
        else next.set('interactions', patch.interactions)
        if (patch.returnTo == null) next.delete('returnTo')
        else next.set('returnTo', patch.returnTo)
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
      setDialogParams({
        newPerson: 'true',
        personId: null,
        usernames: null,
        interactions: null,
        returnTo: null,
      }),
    openEditDialog: (personId: number) =>
      setDialogParams({
        newPerson: null,
        personId: String(personId),
        usernames: null,
        interactions: null,
        returnTo: null,
      }),
    openUsernamesDialog: (personId: number, options?: OpenSubEntityDialogOptions) =>
      setDialogParams({
        newPerson: null,
        personId: String(personId),
        usernames: 'true',
        interactions: null,
        returnTo: options?.returnToEdit === true ? 'edit' : null,
      }),
    openInteractionsDialog: (personId: number, options?: OpenSubEntityDialogOptions) =>
      setDialogParams({
        newPerson: null,
        personId: String(personId),
        usernames: null,
        interactions: 'true',
        returnTo: options?.returnToEdit === true ? 'edit' : null,
      }),
    closeDialog: () =>
      setDialogParams({
        newPerson: null,
        personId: null,
        usernames: null,
        interactions: null,
        returnTo: null,
      }),
  }
}

export function activePeopleFilterCount(state: PeopleState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.category != null) count += 1
  if (state.status !== '') count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
