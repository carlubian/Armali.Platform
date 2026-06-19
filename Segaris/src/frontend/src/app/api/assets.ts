import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type AssetStatus = 'Active' | 'Stored' | 'Retired'
export type AssetVisibility = 'Public' | 'Private'
export type AssetSortField =
  | 'name'
  | 'code'
  | 'category'
  | 'location'
  | 'status'
  | 'expectedEndOfLife'
  | 'visibility'
export type AssetSortDirection = 'asc' | 'desc'

export const assetPageSizes = [10, 25, 50, 100] as const
export type AssetPageSize = (typeof assetPageSizes)[number]
export const assetsRoutePath = '/assets' as const

export interface AssetCategory {
  id: number
  name: string
  sortOrder: number
}

export interface AssetCategoryRequest {
  name: string
}

export interface AssetLocation {
  id: number
  name: string
  sortOrder: number
}

export interface AssetLocationRequest {
  name: string
}

export interface AssetAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
  isPrimary: boolean
}

export interface AssetThumbnail {
  attachmentId: string | null
  url: string | null
  source: 'primary' | 'firstImage' | 'placeholder'
}

export interface AssetSummary {
  id: number
  name: string
  code: string | null
  categoryId: number
  categoryName: string
  locationId: number
  locationName: string
  status: AssetStatus
  expectedEndOfLifeDate: string | null
  visibility: AssetVisibility
  thumbnail: AssetThumbnail
  creatorId: number
  creatorName: string
}

export interface Asset extends AssetSummary {
  brandModel: string | null
  serialNumber: string | null
  acquisitionDate: string | null
  notes: string | null
  attachments: AssetAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface AssetListQuery {
  search?: string | null
  category?: number | null
  location?: number | null
  status?: AssetStatus | null
  visibility?: AssetVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: AssetSortField
  sortDirection?: AssetSortDirection
}

export interface CreateAssetRequest {
  name: string
  categoryId: number
  locationId: number
  status: AssetStatus
  code: string | null
  brandModel: string | null
  serialNumber: string | null
  acquisitionDate: string | null
  expectedEndOfLifeDate: string | null
  notes: string | null
  visibility: AssetVisibility
}

export type UpdateAssetRequest = CreateAssetRequest

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

export const assetsApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<AssetCategory[]>('/api/assets/categories', { signal }),
  locations: (signal?: AbortSignal) =>
    apiRequest<AssetLocation[]>('/api/assets/locations', { signal }),
  listAssets: (query: AssetListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<AssetSummary>>(
      `/api/assets/items${buildQuery(query)}`,
      { signal },
    ),
  getAsset: (assetId: number, signal?: AbortSignal) =>
    apiRequest<Asset>(`/api/assets/items/${assetId}`, { signal }),
  createAsset: (request: CreateAssetRequest, signal?: AbortSignal) =>
    apiRequest<Asset>('/api/assets/items', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateAsset: (assetId: number, request: UpdateAssetRequest, signal?: AbortSignal) =>
    apiRequest<Asset>(`/api/assets/items/${assetId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteAsset: (assetId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/assets/items/${assetId}`, {
      method: 'DELETE',
      signal,
    }),
  listAssetAttachments: (assetId: number, signal?: AbortSignal) =>
    apiRequest<AssetAttachment[]>(`/api/assets/items/${assetId}/attachments`, {
      signal,
    }),
  uploadAssetAttachment: (assetId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<AssetAttachment>(`/api/assets/items/${assetId}/attachments`, {
      method: 'POST',
      body,
      signal,
      timeoutMs: 60_000,
    })
  },
  assetAttachmentDownloadUrl: (assetId: number, attachmentId: string) =>
    `/api/assets/items/${assetId}/attachments/${attachmentId}`,
  deleteAssetAttachment: (
    assetId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/assets/items/${assetId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
  setPrimaryAssetAttachment: (
    assetId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<AssetAttachment>(
      `/api/assets/items/${assetId}/attachments/${attachmentId}/primary`,
      { method: 'PUT', signal },
    ),
}

export const assetCategoriesManagementApi: CatalogManagementClient<
  AssetCategory,
  AssetCategoryRequest
> = catalogManagementClient<AssetCategory, AssetCategoryRequest>(
  '/api/assets/categories',
)

export const assetLocationsManagementApi: CatalogManagementClient<
  AssetLocation,
  AssetLocationRequest
> = catalogManagementClient<AssetLocation, AssetLocationRequest>(
  '/api/assets/locations',
)
