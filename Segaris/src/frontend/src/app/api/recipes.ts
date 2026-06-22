import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type RecipeDifficulty = 'Easy' | 'Medium' | 'Hard'
export type RecipeVisibility = 'Public' | 'Private'
export type MealSlot = 'Breakfast' | 'Lunch' | 'Snack' | 'Dinner'
export type MenuDay =
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday'
  | 'Sunday'
export type RecipeSortField = 'name' | 'category'
export type RecipeSortDirection = 'asc' | 'desc'

export const recipePageSizes = [10, 25, 50, 100] as const
export type RecipePageSize = (typeof recipePageSizes)[number]
export const recipesRoutePath = '/recipes' as const
export const recipeMenusRoutePath = '/recipes/menus' as const

export const mealSlots: readonly MealSlot[] = ['Breakfast', 'Lunch', 'Snack', 'Dinner']
export const menuDays: readonly MenuDay[] = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
]

export interface RecipeCategory {
  id: number
  name: string
  sortOrder: number
}

export interface RecipeCategoryRequest {
  name: string
}

export interface RecipeAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
  isPrimary: boolean
}

export interface RecipeThumbnail {
  attachmentId: string | null
  url: string | null
  source: 'primary' | 'firstImage' | 'placeholder'
}

export interface RecipeIngredient {
  id: number
  name: string
  quantity: string | null
  itemId: number | null
  itemName: string | null
  position: number
}

export interface RecipeStep {
  id: number
  instruction: string
  position: number
}

export interface RecipeSummary {
  id: number
  name: string
  categoryId: number
  categoryName: string
  difficulty: RecipeDifficulty | null
  visibility: RecipeVisibility
  thumbnail: RecipeThumbnail
  creatorId: number
  creatorName: string
}

export interface Recipe extends RecipeSummary {
  servings: number | null
  preparationMinutes: number | null
  cookMinutes: number | null
  ingredients: RecipeIngredient[]
  steps: RecipeStep[]
  notes: string | null
  attachments: RecipeAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface RecipeIngredientInput {
  name: string
  quantity: string | null
  itemId: number | null
}

export interface RecipeStepInput {
  instruction: string
}

export interface RecipeListQuery {
  search?: string | null
  category?: number | null
  difficulty?: RecipeDifficulty | null
  visibility?: RecipeVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: RecipeSortField
  sortDirection?: RecipeSortDirection
}

export interface CreateRecipeRequest {
  name: string
  categoryId: number
  difficulty: RecipeDifficulty | null
  servings: number | null
  preparationMinutes: number | null
  cookMinutes: number | null
  ingredients: RecipeIngredientInput[]
  steps: RecipeStepInput[]
  notes: string | null
  visibility: RecipeVisibility
}

export type UpdateRecipeRequest = CreateRecipeRequest

export interface WeeklyMenuSlotRecipe {
  recipeId: number
  recipeName: string | null
  thumbnail: RecipeThumbnail
}

export interface WeeklyMenuSlot {
  day: MenuDay
  slot: MealSlot
  recipes: WeeklyMenuSlotRecipe[]
}

export interface WeeklyMenuSummary {
  id: number
  week: string
  name: string | null
  visibility: RecipeVisibility
  creatorId: number
  creatorName: string
}

export interface WeeklyMenu {
  id: number
  week: string
  name: string | null
  visibility: RecipeVisibility
  slots: WeeklyMenuSlot[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface WeeklyMenuSlotInput {
  day: MenuDay
  slot: MealSlot
  recipeIds: number[]
}

export interface CreateWeeklyMenuRequest {
  week: string
  name: string | null
  visibility: RecipeVisibility
  slots: WeeklyMenuSlotInput[]
}

export type UpdateWeeklyMenuRequest = CreateWeeklyMenuRequest

function buildQuery<T extends object>(query: T): string {
  const parameters = new URLSearchParams()
  Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
    if (value == null) return
    const text =
      typeof value === 'string'
        ? value.trim()
        : typeof value === 'number'
          ? value.toString()
          : ''
    if (text.length > 0) parameters.set(key, text)
  })
  const search = parameters.toString()
  return search ? `?${search}` : ''
}

export const recipesApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<RecipeCategory[]>('/api/recipes/categories', { signal }),
  listRecipes: (query: RecipeListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<RecipeSummary>>(`/api/recipes${buildQuery(query)}`, {
      signal,
    }),
  getRecipe: (recipeId: number, signal?: AbortSignal) =>
    apiRequest<Recipe>(`/api/recipes/${recipeId}`, { signal }),
  createRecipe: (request: CreateRecipeRequest, signal?: AbortSignal) =>
    apiRequest<Recipe>('/api/recipes', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateRecipe: (
    recipeId: number,
    request: UpdateRecipeRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Recipe>(`/api/recipes/${recipeId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteRecipe: (recipeId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/recipes/${recipeId}`, { method: 'DELETE', signal }),
  listRecipeAttachments: (recipeId: number, signal?: AbortSignal) =>
    apiRequest<RecipeAttachment[]>(`/api/recipes/${recipeId}/attachments`, { signal }),
  uploadRecipeAttachment: (recipeId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<RecipeAttachment>(`/api/recipes/${recipeId}/attachments`, {
      method: 'POST',
      body,
      signal,
      timeoutMs: 60_000,
    })
  },
  recipeAttachmentDownloadUrl: (recipeId: number, attachmentId: string) =>
    `/api/recipes/${recipeId}/attachments/${attachmentId}`,
  deleteRecipeAttachment: (
    recipeId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/recipes/${recipeId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
  setPrimaryRecipeAttachment: (
    recipeId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<RecipeAttachment>(
      `/api/recipes/${recipeId}/attachments/${attachmentId}/primary`,
      { method: 'PUT', signal },
    ),
  listMenus: (week: string, signal?: AbortSignal) =>
    apiRequest<WeeklyMenuSummary[]>(`/api/recipes/menus${buildQuery({ week })}`, {
      signal,
    }),
  getMenu: (menuId: number, signal?: AbortSignal) =>
    apiRequest<WeeklyMenu>(`/api/recipes/menus/${menuId}`, { signal }),
  createMenu: (request: CreateWeeklyMenuRequest, signal?: AbortSignal) =>
    apiRequest<WeeklyMenu>('/api/recipes/menus', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateMenu: (
    menuId: number,
    request: UpdateWeeklyMenuRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<WeeklyMenu>(`/api/recipes/menus/${menuId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteMenu: (menuId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/recipes/menus/${menuId}`, { method: 'DELETE', signal }),
}

export const recipeCategoriesManagementApi: CatalogManagementClient<
  RecipeCategory,
  RecipeCategoryRequest
> = catalogManagementClient<RecipeCategory, RecipeCategoryRequest>(
  '/api/recipes/categories',
)
