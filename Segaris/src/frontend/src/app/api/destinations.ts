import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type DestinationVisibility = 'Public' | 'Private'
export type DestinationSortField = 'name' | 'category'
export type DestinationSortDirection = 'asc' | 'desc'
export type PlaceSortField = 'name' | 'category' | 'rating'
export type PlaceSortDirection = 'asc' | 'desc'
export type PlaceRating = 1 | 2 | 3 | 4 | 5

export const destinationPageSizes = [10, 25, 50, 100] as const
export type DestinationPageSize = (typeof destinationPageSizes)[number]
export const placePageSizes = [10, 25, 50, 100] as const
export type PlacePageSize = (typeof placePageSizes)[number]

export const destinationsRoutePath = '/destinations' as const
export const destinationPlacesRoutePath = (destinationId: number) =>
  `/destinations/${destinationId}/places` as const

export interface DestinationCategory {
  id: number
  name: string
  sortOrder: number
}

export interface PlaceCategory {
  id: number
  name: string
  sortOrder: number
}

export interface DestinationCategoryRequest {
  name: string
}

export interface PlaceCategoryRequest {
  name: string
}

export interface DestinationAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
  isPrimary: boolean
}

export interface DestinationThumbnail {
  attachmentId: string | null
  url: string | null
  source: 'primary' | 'firstImage' | 'placeholder'
}

export interface DestinationSummary {
  id: number
  name: string
  categoryId: number
  categoryName: string
  country: string | null
  isSchengenArea: boolean
  averagePlaceRating: number | null
  ratedPlaceCount: number
  visibility: DestinationVisibility
  thumbnail: DestinationThumbnail
  creatorId: number
  creatorName: string
}

export interface Destination extends DestinationSummary {
  entryRequirements: string | null
  notes: string | null
  attachments: DestinationAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface PlaceSummary {
  id: number
  destinationId: number
  name: string
  categoryId: number
  categoryName: string
  rating: PlaceRating | null
  review: string | null
  address: string | null
  createdAt: string
  updatedAt: string | null
}

export type Place = PlaceSummary

export interface DestinationListQuery {
  search?: string | null
  category?: number | null
  isSchengenArea?: boolean | null
  page?: number
  pageSize?: number
  sort?: DestinationSortField
  sortDirection?: DestinationSortDirection
}

export interface PlaceListQuery {
  search?: string | null
  category?: number | null
  rating?: PlaceRating | null
  page?: number
  pageSize?: number
  sort?: PlaceSortField
  sortDirection?: PlaceSortDirection
}

export interface CreateDestinationRequest {
  name: string
  categoryId: number
  country: string | null
  entryRequirements: string | null
  isSchengenArea: boolean
  notes: string | null
  visibility: DestinationVisibility
}

export type UpdateDestinationRequest = CreateDestinationRequest

export interface CreatePlaceRequest {
  name: string
  categoryId: number
  rating: PlaceRating | null
  review: string | null
  address: string | null
}

export type UpdatePlaceRequest = CreatePlaceRequest

function buildQuery<T extends object>(query: T): string {
  const parameters = new URLSearchParams()
  Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
    if (value == null) return
    const text =
      typeof value === 'string'
        ? value.trim()
        : typeof value === 'number' || typeof value === 'boolean'
          ? value.toString()
          : ''
    if (text.length > 0) parameters.set(key, text)
  })
  const search = parameters.toString()
  return search ? `?${search}` : ''
}

export const destinationsApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<DestinationCategory[]>('/api/destinations/categories', { signal }),
  placeCategories: (signal?: AbortSignal) =>
    apiRequest<PlaceCategory[]>('/api/destinations/place-categories', { signal }),
  listDestinations: (query: DestinationListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<DestinationSummary>>(
      `/api/destinations${buildQuery(query)}`,
      { signal },
    ),
  getDestination: (destinationId: number, signal?: AbortSignal) =>
    apiRequest<Destination>(`/api/destinations/${destinationId}`, { signal }),
  createDestination: (request: CreateDestinationRequest, signal?: AbortSignal) =>
    apiRequest<Destination>('/api/destinations', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateDestination: (
    destinationId: number,
    request: UpdateDestinationRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Destination>(`/api/destinations/${destinationId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteDestination: (destinationId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/destinations/${destinationId}`, {
      method: 'DELETE',
      signal,
    }),
  listDestinationAttachments: (destinationId: number, signal?: AbortSignal) =>
    apiRequest<DestinationAttachment[]>(
      `/api/destinations/${destinationId}/attachments`,
      { signal },
    ),
  uploadDestinationAttachment: (
    destinationId: number,
    file: File,
    signal?: AbortSignal,
  ) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<DestinationAttachment>(
      `/api/destinations/${destinationId}/attachments`,
      {
        method: 'POST',
        body,
        signal,
        timeoutMs: 60_000,
      },
    )
  },
  destinationAttachmentDownloadUrl: (destinationId: number, attachmentId: string) =>
    `/api/destinations/${destinationId}/attachments/${attachmentId}`,
  deleteDestinationAttachment: (
    destinationId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/destinations/${destinationId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
  setPrimaryDestinationAttachment: (
    destinationId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<DestinationAttachment>(
      `/api/destinations/${destinationId}/attachments/${attachmentId}/primary`,
      { method: 'PUT', signal },
    ),
  listPlaces: (
    destinationId: number,
    query: PlaceListQuery = {},
    signal?: AbortSignal,
  ) =>
    apiRequest<PaginatedResponse<PlaceSummary>>(
      `/api/destinations/${destinationId}/places${buildQuery(query)}`,
      { signal },
    ),
  getPlace: (destinationId: number, placeId: number, signal?: AbortSignal) =>
    apiRequest<Place>(`/api/destinations/${destinationId}/places/${placeId}`, {
      signal,
    }),
  createPlace: (
    destinationId: number,
    request: CreatePlaceRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Place>(`/api/destinations/${destinationId}/places`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updatePlace: (
    destinationId: number,
    placeId: number,
    request: UpdatePlaceRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Place>(`/api/destinations/${destinationId}/places/${placeId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deletePlace: (destinationId: number, placeId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/destinations/${destinationId}/places/${placeId}`, {
      method: 'DELETE',
      signal,
    }),
}

export const destinationCategoriesManagementApi: CatalogManagementClient<
  DestinationCategory,
  DestinationCategoryRequest
> = catalogManagementClient<DestinationCategory, DestinationCategoryRequest>(
  '/api/destinations/categories',
)

export const placeCategoriesManagementApi: CatalogManagementClient<
  PlaceCategory,
  PlaceCategoryRequest
> = catalogManagementClient<PlaceCategory, PlaceCategoryRequest>(
  '/api/destinations/place-categories',
)
