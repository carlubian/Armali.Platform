import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type InventoryItemStatus = 'Candidate' | 'Active' | 'Deprecated'
export type InventoryOrderStatus = 'Planning' | 'Active' | 'Received' | 'Cancelled'
export type InventoryVisibility = 'Public' | 'Private'
export type InventoryStockAdjustmentDirection = 'Increase' | 'Decrease'
export type InventoryItemSortField =
  | 'name'
  | 'status'
  | 'category'
  | 'location'
  | 'currentStock'
  | 'minimumStock'
  | 'visibility'
export type InventoryOrderSortField =
  | 'supplier'
  | 'status'
  | 'orderDate'
  | 'expectedReceiptDate'
  | 'currency'
  | 'visibility'
export type InventorySortDirection = 'asc' | 'desc'

export const inventoryPageSizes = [10, 25, 50, 100] as const
export type InventoryPageSize = (typeof inventoryPageSizes)[number]
export const inventoryRoutePath = '/inventory' as const

export interface InventoryCategory {
  id: number
  name: string
  sortOrder: number
}

export interface InventoryCategoryRequest {
  name: string
}

export interface InventoryLocation {
  id: number
  name: string
  sortOrder: number
}

export interface InventoryLocationRequest {
  name: string
}

export interface InventoryAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
}

export interface InventoryItemSupplier {
  supplierId: number
  supplierName: string
}

export interface InventoryItemSummary {
  id: number
  name: string
  status: InventoryItemStatus
  categoryId: number
  categoryName: string
  locationId: number
  locationName: string
  currentStock: number
  minimumStock: number
  visibility: InventoryVisibility
  creatorId: number
  creatorName: string
}

export interface InventoryItem {
  id: number
  name: string
  status: InventoryItemStatus
  notes: string | null
  categoryId: number
  categoryName: string
  locationId: number
  locationName: string
  currentStock: number
  minimumStock: number
  visibility: InventoryVisibility
  suppliers: InventoryItemSupplier[]
  attachments: InventoryAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string
}

export interface InventoryItemDeletionImpact {
  isReferenced: boolean
  referenceCount: number
}

export interface InventoryOrderLine {
  id: number
  itemId: number
  itemName: string
  itemStatus: InventoryItemStatus
  quantity: number
  lineTotal: number
}

export interface InventoryOrderSummary {
  id: number
  supplierId: number
  supplierName: string
  status: InventoryOrderStatus
  orderDate: string | null
  expectedReceiptDate: string | null
  currencyId: number
  currencyCode: string
  visibility: InventoryVisibility
  creatorId: number
  creatorName: string
}

export interface InventoryOrder {
  id: number
  supplierId: number
  supplierName: string
  status: InventoryOrderStatus
  orderDate: string | null
  expectedReceiptDate: string | null
  currencyId: number
  currencyCode: string
  notes: string | null
  visibility: InventoryVisibility
  lines: InventoryOrderLine[]
  attachments: InventoryAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string
}

export interface InventoryItemListQuery {
  search?: string | null
  status?: InventoryItemStatus | null
  category?: number | null
  location?: number | null
  supplier?: number | null
  visibility?: InventoryVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: InventoryItemSortField
  sortDirection?: InventorySortDirection
}

export interface InventoryOrderListQuery {
  search?: string | null
  supplier?: number | null
  status?: InventoryOrderStatus | null
  currency?: number | null
  visibility?: InventoryVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: InventoryOrderSortField
  sortDirection?: InventorySortDirection
}

export interface CreateInventoryItemRequest {
  name: string
  status: InventoryItemStatus
  notes: string | null
  categoryId: number
  locationId: number
  currentStock: number
  minimumStock: number
  supplierIds: number[]
  visibility: InventoryVisibility
}

export type UpdateInventoryItemRequest = CreateInventoryItemRequest

export interface InventoryStockAdjustmentRequest {
  direction: InventoryStockAdjustmentDirection
  quantity: number
}

export interface InventoryOrderLineRequest {
  itemId: number
  quantity: number
  lineTotal: number
}

export interface CreateInventoryOrderRequest {
  supplierId: number
  status: InventoryOrderStatus
  currencyId: number
  orderDate: string | null
  expectedReceiptDate: string | null
  notes: string | null
  visibility: InventoryVisibility
  lines: InventoryOrderLineRequest[]
}

export type UpdateInventoryOrderRequest = CreateInventoryOrderRequest

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

export const inventoryApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<InventoryCategory[]>('/api/inventory/categories', { signal }),
  locations: (signal?: AbortSignal) =>
    apiRequest<InventoryLocation[]>('/api/inventory/locations', { signal }),
  listItems: (query: InventoryItemListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<InventoryItemSummary>>(
      `/api/inventory/items${buildQuery(query)}`,
      { signal },
    ),
  getItem: (itemId: number, signal?: AbortSignal) =>
    apiRequest<InventoryItem>(`/api/inventory/items/${itemId}`, { signal }),
  createItem: (request: CreateInventoryItemRequest, signal?: AbortSignal) =>
    apiRequest<InventoryItem>('/api/inventory/items', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateItem: (
    itemId: number,
    request: UpdateInventoryItemRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<InventoryItem>(`/api/inventory/items/${itemId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteItem: (itemId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/inventory/items/${itemId}`, {
      method: 'DELETE',
      signal,
    }),
  itemDeletionImpact: (itemId: number, signal?: AbortSignal) =>
    apiRequest<InventoryItemDeletionImpact>(
      `/api/inventory/items/${itemId}/deletion-impact`,
      { signal },
    ),
  adjustStock: (
    itemId: number,
    request: InventoryStockAdjustmentRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<InventoryItem>(`/api/inventory/items/${itemId}/stock-adjustments`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  listOrders: (query: InventoryOrderListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<InventoryOrderSummary>>(
      `/api/inventory/orders${buildQuery(query)}`,
      { signal },
    ),
  getOrder: (orderId: number, signal?: AbortSignal) =>
    apiRequest<InventoryOrder>(`/api/inventory/orders/${orderId}`, { signal }),
  createOrder: (request: CreateInventoryOrderRequest, signal?: AbortSignal) =>
    apiRequest<InventoryOrder>('/api/inventory/orders', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateOrder: (
    orderId: number,
    request: UpdateInventoryOrderRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<InventoryOrder>(`/api/inventory/orders/${orderId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteOrder: (orderId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/inventory/orders/${orderId}`, {
      method: 'DELETE',
      signal,
    }),
  receiveOrder: (orderId: number, signal?: AbortSignal) =>
    apiRequest<InventoryOrder>(`/api/inventory/orders/${orderId}/receive`, {
      method: 'POST',
      signal,
    }),
  listItemAttachments: (itemId: number, signal?: AbortSignal) =>
    apiRequest<InventoryAttachment[]>(`/api/inventory/items/${itemId}/attachments`, {
      signal,
    }),
  uploadItemAttachment: (itemId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<InventoryAttachment>(
      `/api/inventory/items/${itemId}/attachments`,
      { method: 'POST', body, signal, timeoutMs: 60_000 },
    )
  },
  itemAttachmentDownloadUrl: (itemId: number, attachmentId: string) =>
    `/api/inventory/items/${itemId}/attachments/${attachmentId}`,
  deleteItemAttachment: (itemId: number, attachmentId: string, signal?: AbortSignal) =>
    apiRequest<void>(`/api/inventory/items/${itemId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
  listOrderAttachments: (orderId: number, signal?: AbortSignal) =>
    apiRequest<InventoryAttachment[]>(`/api/inventory/orders/${orderId}/attachments`, {
      signal,
    }),
  uploadOrderAttachment: (orderId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<InventoryAttachment>(
      `/api/inventory/orders/${orderId}/attachments`,
      { method: 'POST', body, signal, timeoutMs: 60_000 },
    )
  },
  orderAttachmentDownloadUrl: (orderId: number, attachmentId: string) =>
    `/api/inventory/orders/${orderId}/attachments/${attachmentId}`,
  deleteOrderAttachment: (
    orderId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/inventory/orders/${orderId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
}

export const inventoryCategoriesManagementApi: CatalogManagementClient<
  InventoryCategory,
  InventoryCategoryRequest
> = catalogManagementClient<InventoryCategory, InventoryCategoryRequest>(
  '/api/inventory/categories',
)

export const inventoryLocationsManagementApi: CatalogManagementClient<
  InventoryLocation,
  InventoryLocationRequest
> = catalogManagementClient<InventoryLocation, InventoryLocationRequest>(
  '/api/inventory/locations',
)
