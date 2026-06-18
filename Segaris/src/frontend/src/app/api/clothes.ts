import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type ClothesGarmentStatus = 'Active' | 'Unavailable' | 'Deprecated'
export type ClothesVisibility = 'Public' | 'Private'
export type ClothesWashingCare =
  | 'Any'
  | 'Wash30'
  | 'Wash30Delicate'
  | 'Wash40'
  | 'Wash40Delicate'
  | 'Wash50'
  | 'Wash50Delicate'
  | 'Wash60'
  | 'Wash60Delicate'
  | 'HandWash'
  | 'DoNotWash'
export type ClothesDryingCare = 'Any' | 'Delicate' | 'VeryDelicate'
export type ClothesIroningCare = 'Any' | 'Low' | 'Medium' | 'DoNotIron'
export type ClothesDryCleaningCare = 'Any' | 'DoNotDryClean'
export type ClothesGarmentSortField =
  | 'name'
  | 'category'
  | 'status'
  | 'visibility'
export type ClothesSortDirection = 'asc' | 'desc'

export const clothesPageSizes = [10, 25, 50, 100] as const
export type ClothesPageSize = (typeof clothesPageSizes)[number]
export const clothesRoutePath = '/clothes' as const

export interface ClothingCategory {
  id: number
  name: string
  sortOrder: number
}

export interface ClothingCategoryRequest {
  name: string
}

export interface ClothingColor {
  id: number
  name: string
  colorValue: string
  sortOrder: number
}

export interface ClothingColorRequest {
  name: string
  colorValue: string
}

export interface ClothesAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
  isPrimary: boolean
}

export interface ClothesThumbnail {
  attachmentId: string | null
  url: string | null
  source: 'primary' | 'firstImage' | 'placeholder'
}

export interface ClothesGarmentSummary {
  id: number
  name: string
  categoryId: number
  categoryName: string
  status: ClothesGarmentStatus
  size: string | null
  colors: ClothingColor[]
  washingCare: ClothesWashingCare | null
  dryingCare: ClothesDryingCare | null
  ironingCare: ClothesIroningCare | null
  dryCleaningCare: ClothesDryCleaningCare | null
  visibility: ClothesVisibility
  thumbnail: ClothesThumbnail
  creatorId: number
  creatorName: string
}

export interface ClothesGarment extends ClothesGarmentSummary {
  notes: string | null
  attachments: ClothesAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface ClothesGarmentListQuery {
  search?: string | null
  category?: number | null
  status?: ClothesGarmentStatus | null
  color?: number | null
  visibility?: ClothesVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: ClothesGarmentSortField
  sortDirection?: ClothesSortDirection
}

export interface CreateClothesGarmentRequest {
  name: string
  categoryId: number
  status: ClothesGarmentStatus
  size: string | null
  colorIds: number[]
  washingCare: ClothesWashingCare | null
  dryingCare: ClothesDryingCare | null
  ironingCare: ClothesIroningCare | null
  dryCleaningCare: ClothesDryCleaningCare | null
  notes: string | null
  visibility: ClothesVisibility
}

export type UpdateClothesGarmentRequest = CreateClothesGarmentRequest

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

export const clothesApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<ClothingCategory[]>('/api/clothes/categories', { signal }),
  colors: (signal?: AbortSignal) =>
    apiRequest<ClothingColor[]>('/api/clothes/colors', { signal }),
  listGarments: (query: ClothesGarmentListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<ClothesGarmentSummary>>(
      `/api/clothes/garments${buildQuery(query)}`,
      { signal },
    ),
  getGarment: (garmentId: number, signal?: AbortSignal) =>
    apiRequest<ClothesGarment>(`/api/clothes/garments/${garmentId}`, { signal }),
  createGarment: (request: CreateClothesGarmentRequest, signal?: AbortSignal) =>
    apiRequest<ClothesGarment>('/api/clothes/garments', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateGarment: (
    garmentId: number,
    request: UpdateClothesGarmentRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<ClothesGarment>(`/api/clothes/garments/${garmentId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteGarment: (garmentId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/clothes/garments/${garmentId}`, {
      method: 'DELETE',
      signal,
    }),
  listGarmentAttachments: (garmentId: number, signal?: AbortSignal) =>
    apiRequest<ClothesAttachment[]>(
      `/api/clothes/garments/${garmentId}/attachments`,
      { signal },
    ),
  uploadGarmentAttachment: (garmentId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<ClothesAttachment>(
      `/api/clothes/garments/${garmentId}/attachments`,
      { method: 'POST', body, signal, timeoutMs: 60_000 },
    )
  },
  garmentAttachmentDownloadUrl: (garmentId: number, attachmentId: string) =>
    `/api/clothes/garments/${garmentId}/attachments/${attachmentId}`,
  deleteGarmentAttachment: (
    garmentId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(
      `/api/clothes/garments/${garmentId}/attachments/${attachmentId}`,
      { method: 'DELETE', signal },
    ),
  setPrimaryGarmentAttachment: (
    garmentId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<ClothesAttachment>(
      `/api/clothes/garments/${garmentId}/attachments/${attachmentId}/primary`,
      { method: 'PUT', signal },
    ),
}

export const clothingCategoriesManagementApi: CatalogManagementClient<
  ClothingCategory,
  ClothingCategoryRequest
> = catalogManagementClient<ClothingCategory, ClothingCategoryRequest>(
  '/api/clothes/categories',
)

export const clothingColorsManagementApi: CatalogManagementClient<
  ClothingColor,
  ClothingColorRequest
> = catalogManagementClient<ClothingColor, ClothingColorRequest>(
  '/api/clothes/colors',
)
