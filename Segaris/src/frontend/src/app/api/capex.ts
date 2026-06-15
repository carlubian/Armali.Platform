import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type { PaginatedResponse } from './adminUsers'

/** Fixed Capex movement-type vocabulary exchanged on the wire. */
export type CapexMovementType = 'Income' | 'Expense'

/** Fixed Capex entry-status vocabulary exchanged on the wire. */
export type CapexEntryStatus = 'Planning' | 'Completed' | 'Canceled'

/** Platform visibility values used by Capex entries. */
export type CapexVisibility = 'Public' | 'Private'

/** Allow-listed sort fields for the paginated Entries list. */
export type CapexEntrySortField =
  | 'title'
  | 'type'
  | 'status'
  | 'dueDate'
  | 'category'
  | 'supplier'
  | 'costCenter'
  | 'total'
  | 'currency'

export type SortDirection = 'asc' | 'desc'

/** User-selectable page sizes; the default matches the platform default of 25. */
export const capexPageSizes = [10, 25, 50, 100] as const
export type CapexPageSize = (typeof capexPageSizes)[number]

/** Frozen Capex category catalog row from `GET /api/capex/categories`. */
export interface CapexCategory {
  id: number
  name: string
  sortOrder: number
}

/** Create/update body for a Capex category. Categories are required and
 * replace-only: clearing references and exchange rates are never valid. */
export interface CapexCategoryRequest {
  name: string
}

/** A single row of the paginated Entries list. Amounts and currency stay separate. */
export interface CapexEntrySummary {
  id: number
  title: string
  movementType: CapexMovementType
  status: CapexEntryStatus
  dueDate: string
  categoryId: number
  categoryName: string
  supplierId: number | null
  supplierName: string | null
  costCenterId: number | null
  costCenterName: string | null
  currencyId: number
  currencyCode: string
  totalAmount: number
  visibility: CapexVisibility
  creatorId: number
  creatorName: string
}

/** An ordered item line returned in entry detail. */
export interface CapexEntryItem {
  id: number
  position: number
  description: string
  quantity: number
  unitAmount: number
  lineAmount: number
}

/** An attachment descriptor for an entry's attachments. */
export interface CapexAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
}

/** Full detail returned by `GET /api/capex/entries/{id}` and create/update. */
export interface CapexEntry {
  id: number
  title: string
  movementType: CapexMovementType
  status: CapexEntryStatus
  dueDate: string
  categoryId: number
  categoryName: string
  supplierId: number | null
  supplierName: string | null
  costCenterId: number | null
  costCenterName: string | null
  currencyId: number
  currencyCode: string
  notes: string | null
  visibility: CapexVisibility
  totalAmount: number
  items: CapexEntryItem[]
  attachments: CapexAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string
}

/** Allow-listed filter and pagination inputs for the Entries list query. */
export interface CapexEntryListQuery {
  search?: string | null
  from?: string | null
  to?: string | null
  type?: CapexMovementType | null
  status?: CapexEntryStatus | null
  category?: number | null
  supplier?: number | null
  costCenter?: number | null
  currency?: number | null
  visibility?: CapexVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: CapexEntrySortField
  sortDirection?: SortDirection
}

/** Item line submitted on create/update. The server is authoritative for totals. */
export interface CapexItemInput {
  description: string
  quantity: number
  unitAmount: number
}

/** Body for `POST /api/capex/entries`. */
export interface CreateCapexEntryRequest {
  title: string
  movementType: CapexMovementType
  status: CapexEntryStatus
  dueDate: string
  categoryId: number
  supplierId: number | null
  costCenterId: number | null
  currencyId: number
  notes: string | null
  visibility: CapexVisibility
  items: CapexItemInput[]
}

/** Body for `PUT /api/capex/entries/{id}`; fully replaces the ordered items. */
export type UpdateCapexEntryRequest = CreateCapexEntryRequest

function buildListQuery(query: CapexEntryListQuery): string {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | null | undefined) => {
    if (value == null) return
    const text = typeof value === 'string' ? value.trim() : String(value)
    if (text.length > 0) params.set(key, text)
  }

  set('search', query.search)
  set('from', query.from)
  set('to', query.to)
  set('type', query.type)
  set('status', query.status)
  set('category', query.category)
  set('supplier', query.supplier)
  set('costCenter', query.costCenter)
  set('currency', query.currency)
  set('visibility', query.visibility)
  set('creator', query.creator)
  set('page', query.page)
  set('pageSize', query.pageSize)
  set('sort', query.sort)
  set('sortDirection', query.sortDirection)

  const search = params.toString()
  return search ? `?${search}` : ''
}

export const capexApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<CapexCategory[]>('/api/capex/categories', { signal }),
  listEntries: (query: CapexEntryListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<CapexEntrySummary>>(
      `/api/capex/entries${buildListQuery(query)}`,
      { signal },
    ),
  getEntry: (entryId: number, signal?: AbortSignal) =>
    apiRequest<CapexEntry>(`/api/capex/entries/${entryId}`, { signal }),
  createEntry: (request: CreateCapexEntryRequest, signal?: AbortSignal) =>
    apiRequest<CapexEntry>('/api/capex/entries', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateEntry: (
    entryId: number,
    request: UpdateCapexEntryRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<CapexEntry>(`/api/capex/entries/${entryId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteEntry: (entryId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/capex/entries/${entryId}`, { method: 'DELETE', signal }),
  listAttachments: (entryId: number, signal?: AbortSignal) =>
    apiRequest<CapexAttachment[]>(`/api/capex/entries/${entryId}/attachments`, {
      signal,
    }),
  uploadAttachment: (entryId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<CapexAttachment>(`/api/capex/entries/${entryId}/attachments`, {
      method: 'POST',
      body,
      signal,
      // Uploads carry up to 25 MB, so they need a longer window than the
      // default short request timeout.
      timeoutMs: 60_000,
    })
  },
  attachmentDownloadUrl: (entryId: number, attachmentId: string) =>
    `/api/capex/entries/${entryId}/attachments/${attachmentId}`,
  deleteAttachment: (entryId: number, attachmentId: string, signal?: AbortSignal) =>
    apiRequest<void>(`/api/capex/entries/${entryId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
}

/**
 * Administrator-only management client for the Capex category catalog. Reads stay
 * on {@link capexApi.categories} because the entry editor consumes them; every
 * method here requires `Admin` and is antiforgery-protected on the server.
 */
export const capexCategoriesManagementApi: CatalogManagementClient<
  CapexCategory,
  CapexCategoryRequest
> = catalogManagementClient<CapexCategory, CapexCategoryRequest>(
  '/api/capex/categories',
)
