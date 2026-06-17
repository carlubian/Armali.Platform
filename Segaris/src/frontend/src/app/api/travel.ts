import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type TravelTripStatus = 'Planned' | 'Ongoing' | 'Completed' | 'Cancelled'
export type TravelVisibility = 'Public' | 'Private'
export type TravelTripSortField =
  | 'name'
  | 'tripType'
  | 'destination'
  | 'startDate'
  | 'endDate'
  | 'status'
  | 'visibility'
export type TravelExpenseSortField =
  | 'date'
  | 'category'
  | 'description'
  | 'amount'
  | 'currency'
  | 'supplier'
  | 'costCenter'
export type TravelSortDirection = 'asc' | 'desc'

export const travelPageSizes = [10, 25, 50, 100] as const
export type TravelPageSize = (typeof travelPageSizes)[number]
export const travelRoutePath = '/travel' as const

export interface TravelTripType {
  id: number
  name: string
  sortOrder: number
}

export interface TravelTripTypeRequest {
  name: string
}

export interface TravelExpenseCategory {
  id: number
  name: string
  sortOrder: number
}

export interface TravelExpenseCategoryRequest {
  name: string
}

export interface TravelAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
}

export interface TravelTripSummary {
  id: number
  name: string
  tripTypeId: number
  tripTypeName: string
  destination: string | null
  startDate: string
  endDate: string
  status: TravelTripStatus
  visibility: TravelVisibility
  creatorId: number
  creatorName: string
}

export interface TravelItineraryEntry {
  id: number
  date: string
  time: string | null
  title: string
  place: string | null
  reservationLocator: string | null
  note: string | null
  sortOrder: number
}

export interface TravelExpenseTotal {
  currencyId: number
  currencyCode: string
  amount: number
}

export interface TravelTrip {
  id: number
  name: string
  tripTypeId: number
  tripTypeName: string
  destination: string | null
  startDate: string
  endDate: string
  status: TravelTripStatus
  notes: string | null
  visibility: TravelVisibility
  itinerary: TravelItineraryEntry[]
  expenseTotals: TravelExpenseTotal[]
  attachments: TravelAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface TravelExpenseSummary {
  id: number
  expenseCategoryId: number
  expenseCategoryName: string
  description: string
  date: string
  amount: number
  currencyId: number
  currencyCode: string
  supplierId: number | null
  supplierName: string | null
  costCenterId: number | null
  costCenterName: string | null
}

export interface TravelExpense extends TravelExpenseSummary {
  notes: string | null
  attachments: TravelAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface TravelTripListQuery {
  search?: string | null
  tripType?: number | null
  status?: TravelTripStatus | null
  visibility?: TravelVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: TravelTripSortField
  sortDirection?: TravelSortDirection
}

export interface TravelExpenseListQuery {
  search?: string | null
  category?: number | null
  currency?: number | null
  supplier?: number | null
  costCenter?: number | null
  page?: number
  pageSize?: number
  sort?: TravelExpenseSortField
  sortDirection?: TravelSortDirection
}

export interface TravelItineraryEntryRequest {
  date: string
  time: string | null
  title: string
  place: string | null
  reservationLocator: string | null
  note: string | null
}

export interface CreateTravelTripRequest {
  name: string
  tripTypeId: number
  destination: string | null
  startDate: string
  endDate: string
  status: TravelTripStatus
  notes: string | null
  visibility: TravelVisibility
  itinerary: TravelItineraryEntryRequest[]
}

export type UpdateTravelTripRequest = CreateTravelTripRequest

export interface CreateTravelExpenseRequest {
  expenseCategoryId: number
  description: string
  date: string
  amount: number
  currencyId: number
  supplierId: number | null
  costCenterId: number | null
  notes: string | null
}

export type UpdateTravelExpenseRequest = CreateTravelExpenseRequest

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

export const travelApi = {
  tripTypes: (signal?: AbortSignal) =>
    apiRequest<TravelTripType[]>('/api/travel/trip-types', { signal }),
  expenseCategories: (signal?: AbortSignal) =>
    apiRequest<TravelExpenseCategory[]>('/api/travel/expense-categories', { signal }),
  listTrips: (query: TravelTripListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<TravelTripSummary>>(
      `/api/travel/trips${buildQuery(query)}`,
      { signal },
    ),
  getTrip: (tripId: number, signal?: AbortSignal) =>
    apiRequest<TravelTrip>(`/api/travel/trips/${tripId}`, { signal }),
  createTrip: (request: CreateTravelTripRequest, signal?: AbortSignal) =>
    apiRequest<TravelTrip>('/api/travel/trips', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateTrip: (tripId: number, request: UpdateTravelTripRequest, signal?: AbortSignal) =>
    apiRequest<TravelTrip>(`/api/travel/trips/${tripId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteTrip: (tripId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/travel/trips/${tripId}`, { method: 'DELETE', signal }),
  listExpenses: (tripId: number, query: TravelExpenseListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<TravelExpenseSummary>>(
      `/api/travel/trips/${tripId}/expenses${buildQuery(query)}`,
      { signal },
    ),
  getExpense: (tripId: number, expenseId: number, signal?: AbortSignal) =>
    apiRequest<TravelExpense>(
      `/api/travel/trips/${tripId}/expenses/${expenseId}`,
      { signal },
    ),
  createExpense: (
    tripId: number,
    request: CreateTravelExpenseRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<TravelExpense>(`/api/travel/trips/${tripId}/expenses`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateExpense: (
    tripId: number,
    expenseId: number,
    request: UpdateTravelExpenseRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<TravelExpense>(`/api/travel/trips/${tripId}/expenses/${expenseId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteExpense: (tripId: number, expenseId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/travel/trips/${tripId}/expenses/${expenseId}`, {
      method: 'DELETE',
      signal,
    }),
}

export const travelTripTypesManagementApi: CatalogManagementClient<
  TravelTripType,
  TravelTripTypeRequest
> = catalogManagementClient<TravelTripType, TravelTripTypeRequest>(
  '/api/travel/trip-types',
)

export const travelExpenseCategoriesManagementApi: CatalogManagementClient<
  TravelExpenseCategory,
  TravelExpenseCategoryRequest
> = catalogManagementClient<TravelExpenseCategory, TravelExpenseCategoryRequest>(
  '/api/travel/expense-categories',
)
