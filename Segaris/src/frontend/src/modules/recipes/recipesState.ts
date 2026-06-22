import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  recipePageSizes,
  type RecipeDifficulty,
  type RecipeListQuery,
  type RecipePageSize,
  type RecipeSortDirection,
  type RecipeSortField,
  type RecipeVisibility,
} from '@/app/api/recipes'

const sortFields: readonly RecipeSortField[] = ['name', 'category']
const difficulties: readonly RecipeDifficulty[] = ['Easy', 'Medium', 'Hard']
const visibilities: readonly RecipeVisibility[] = ['Public', 'Private']

export const defaultSort: RecipeSortField = 'name'
export const defaultSortDirection: RecipeSortDirection = 'asc'
export const defaultPageSize: RecipePageSize = 25

export interface RecipesState {
  search: string
  category: number | null
  difficulty: RecipeDifficulty | ''
  visibility: RecipeVisibility | ''
  mine: boolean
  sort: RecipeSortField
  sortDirection: RecipeSortDirection
  page: number
  pageSize: RecipePageSize
}

export type RecipesFilterPatch = Partial<
  Omit<RecipesState, 'page' | 'sort' | 'sortDirection' | 'pageSize'>
>

export type RecipeDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; recipeId: number }

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

export function parseRecipesState(
  params: URLSearchParams,
  currentUserId: number | null,
): RecipesState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    category: intOrNull(params.get('category')),
    difficulty: oneOf(params.get('difficulty'), difficulties),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (recipePageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as RecipePageSize)
      : defaultPageSize,
  }
}

export function parseRecipeDialogState(params: URLSearchParams): RecipeDialogState {
  if (params.get('newRecipe') === 'true') return { mode: 'create' }
  const recipeId = intOrNull(params.get('recipeId'))
  return recipeId == null ? { mode: 'closed' } : { mode: 'edit', recipeId }
}

export function toListQuery(
  state: RecipesState,
  currentUserId: number | null,
): RecipeListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    difficulty: state.difficulty === '' ? null : state.difficulty,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

function writeState(
  state: RecipesState,
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
  set('difficulty', state.difficulty === '' ? null : state.difficulty)
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
  'difficulty',
  'visibility',
  'creator',
  'sort',
  'sortDirection',
  'page',
  'pageSize',
])

export interface UseRecipesState {
  state: RecipesState
  dialog: RecipeDialogState
  listQuery: RecipeListQuery
  setFilters: (patch: RecipesFilterPatch) => void
  setSort: (sort: RecipeSortField) => void
  setPage: (page: number) => void
  setPageSize: (pageSize: RecipePageSize) => void
  clearFilters: () => void
  openCreateDialog: () => void
  openEditDialog: (recipeId: number) => void
  closeDialog: () => void
}

export function useRecipesState(currentUserId: number | null): UseRecipesState {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseRecipesState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(() => parseRecipeDialogState(searchParams), [searchParams])

  const commit = useCallback(
    (next: RecipesState, options?: { replace?: boolean }) => {
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
    (patch: RecipesFilterPatch) => {
      const replace = 'search' in patch && Object.keys(patch).length === 1
      commit({ ...state, ...patch, page: 1 }, { replace })
    },
    [commit, state],
  )

  const setSort = useCallback(
    (sort: RecipeSortField) => {
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
    (pageSize: RecipePageSize) => commit({ ...state, pageSize, page: 1 }),
    [commit, state],
  )

  const clearFilters = useCallback(
    () =>
      commit({
        ...state,
        search: '',
        category: null,
        difficulty: '',
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
    (patch: { newRecipe?: string | null; recipeId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newRecipe == null) next.delete('newRecipe')
        else next.set('newRecipe', patch.newRecipe)
        if (patch.recipeId == null) next.delete('recipeId')
        else next.set('recipeId', patch.recipeId)
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
    openCreateDialog: () => setDialogParams({ newRecipe: 'true', recipeId: null }),
    openEditDialog: (recipeId: number) =>
      setDialogParams({ newRecipe: null, recipeId: String(recipeId) }),
    closeDialog: () => setDialogParams({ newRecipe: null, recipeId: null }),
  }
}

export function activeRecipeFilterCount(state: RecipesState): number {
  let count = 0
  if (state.search.trim() !== '') count += 1
  if (state.category != null) count += 1
  if (state.difficulty !== '') count += 1
  if (state.visibility !== '') count += 1
  if (state.mine) count += 1
  return count
}
