import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type OpexMovementType = 'Income' | 'Expense'
export type OpexContractStatus = 'Planning' | 'Active' | 'OnHold' | 'Closed'
export type OpexExpectedFrequency =
  | 'None'
  | 'Weekly'
  | 'Monthly'
  | 'Quarterly'
  | 'SemiAnnual'
  | 'Annual'
  | 'Irregular'
export type OpexVisibility = 'Public' | 'Private'
export type OpexContractSortField =
  | 'name'
  | 'type'
  | 'status'
  | 'category'
  | 'supplier'
  | 'frequency'
  | 'estimatedAnnualAmount'
  | 'realizedCurrentYearAmount'
  | 'currency'
export type OpexSortDirection = 'asc' | 'desc'

export const opexPageSizes = [10, 25, 50, 100] as const
export const opexRoutePath = '/opex' as const

export interface OpexCategory {
  id: number
  name: string
  sortOrder: number
}

export interface OpexCategoryRequest {
  name: string
}

export interface OpexAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
}

export interface OpexContractSummary {
  id: number
  name: string
  movementType: OpexMovementType
  status: OpexContractStatus
  categoryId: number
  categoryName: string
  supplierId: number | null
  supplierName: string | null
  costCenterId: number | null
  costCenterName: string | null
  currencyId: number
  currencyCode: string
  expectedFrequency: OpexExpectedFrequency
  estimatedAnnualAmount: number | null
  realizedCurrentYearAmount: number
  visibility: OpexVisibility
  creatorId: number
  creatorName: string
}

export interface OpexContract {
  id: number
  name: string
  movementType: OpexMovementType
  status: OpexContractStatus
  startDate: string | null
  closedDate: string | null
  estimatedAnnualAmount: number | null
  expectedFrequency: OpexExpectedFrequency
  categoryId: number
  categoryName: string
  supplierId: number | null
  supplierName: string | null
  costCenterId: number | null
  costCenterName: string | null
  currencyId: number
  currencyCode: string
  notes: string | null
  visibility: OpexVisibility
  attachments: OpexAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string
}

export interface OpexOccurrenceSummary {
  id: number
  effectiveDate: string
  actualAmount: number
  description: string | null
}

export interface OpexOccurrence extends OpexOccurrenceSummary {
  contractId: number
  notes: string | null
  attachments: OpexAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string
}

export interface OpexContractListQuery {
  search?: string | null
  type?: OpexMovementType | null
  status?: OpexContractStatus | null
  category?: number | null
  supplier?: number | null
  costCenter?: number | null
  currency?: number | null
  frequency?: OpexExpectedFrequency | null
  visibility?: OpexVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: OpexContractSortField
  sortDirection?: OpexSortDirection
}

export interface OpexOccurrenceListQuery {
  page?: number
  pageSize?: number
}

export interface CreateOpexContractRequest {
  name: string
  movementType: OpexMovementType
  status: OpexContractStatus
  startDate: string | null
  closedDate: string | null
  estimatedAnnualAmount: number | null
  expectedFrequency: OpexExpectedFrequency
  categoryId: number
  supplierId: number | null
  costCenterId: number | null
  currencyId: number
  notes: string | null
  visibility: OpexVisibility
}

export type UpdateOpexContractRequest = CreateOpexContractRequest

export interface CreateOpexOccurrenceRequest {
  effectiveDate: string
  actualAmount: number
  description: string | null
  notes: string | null
}

export type UpdateOpexOccurrenceRequest = CreateOpexOccurrenceRequest

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

export const opexApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<OpexCategory[]>('/api/opex/categories', { signal }),
  listContracts: (query: OpexContractListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<OpexContractSummary>>(
      `/api/opex/contracts${buildQuery(query)}`,
      { signal },
    ),
  getContract: (contractId: number, signal?: AbortSignal) =>
    apiRequest<OpexContract>(`/api/opex/contracts/${contractId}`, { signal }),
  createContract: (request: CreateOpexContractRequest, signal?: AbortSignal) =>
    apiRequest<OpexContract>('/api/opex/contracts', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateContract: (
    contractId: number,
    request: UpdateOpexContractRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<OpexContract>(`/api/opex/contracts/${contractId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteContract: (contractId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/opex/contracts/${contractId}`, {
      method: 'DELETE',
      signal,
    }),
  listOccurrences: (
    contractId: number,
    query: OpexOccurrenceListQuery = {},
    signal?: AbortSignal,
  ) =>
    apiRequest<PaginatedResponse<OpexOccurrenceSummary>>(
      `/api/opex/contracts/${contractId}/occurrences${buildQuery(query)}`,
      { signal },
    ),
  getOccurrence: (contractId: number, occurrenceId: number, signal?: AbortSignal) =>
    apiRequest<OpexOccurrence>(
      `/api/opex/contracts/${contractId}/occurrences/${occurrenceId}`,
      { signal },
    ),
  createOccurrence: (
    contractId: number,
    request: CreateOpexOccurrenceRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<OpexOccurrence>(`/api/opex/contracts/${contractId}/occurrences`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateOccurrence: (
    contractId: number,
    occurrenceId: number,
    request: UpdateOpexOccurrenceRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<OpexOccurrence>(
      `/api/opex/contracts/${contractId}/occurrences/${occurrenceId}`,
      { method: 'PUT', body: JSON.stringify(request), signal },
    ),
  deleteOccurrence: (contractId: number, occurrenceId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/opex/contracts/${contractId}/occurrences/${occurrenceId}`, {
      method: 'DELETE',
      signal,
    }),
  listContractAttachments: (contractId: number, signal?: AbortSignal) =>
    apiRequest<OpexAttachment[]>(`/api/opex/contracts/${contractId}/attachments`, {
      signal,
    }),
  uploadContractAttachment: (contractId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<OpexAttachment>(`/api/opex/contracts/${contractId}/attachments`, {
      method: 'POST',
      body,
      signal,
      timeoutMs: 60_000,
    })
  },
  contractAttachmentDownloadUrl: (contractId: number, attachmentId: string) =>
    `/api/opex/contracts/${contractId}/attachments/${attachmentId}`,
  deleteContractAttachment: (
    contractId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/opex/contracts/${contractId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
  listOccurrenceAttachments: (
    contractId: number,
    occurrenceId: number,
    signal?: AbortSignal,
  ) =>
    apiRequest<OpexAttachment[]>(
      `/api/opex/contracts/${contractId}/occurrences/${occurrenceId}/attachments`,
      { signal },
    ),
  uploadOccurrenceAttachment: (
    contractId: number,
    occurrenceId: number,
    file: File,
    signal?: AbortSignal,
  ) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<OpexAttachment>(
      `/api/opex/contracts/${contractId}/occurrences/${occurrenceId}/attachments`,
      { method: 'POST', body, signal, timeoutMs: 60_000 },
    )
  },
  occurrenceAttachmentDownloadUrl: (
    contractId: number,
    occurrenceId: number,
    attachmentId: string,
  ) =>
    `/api/opex/contracts/${contractId}/occurrences/${occurrenceId}/attachments/${attachmentId}`,
  deleteOccurrenceAttachment: (
    contractId: number,
    occurrenceId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(
      `/api/opex/contracts/${contractId}/occurrences/${occurrenceId}/attachments/${attachmentId}`,
      { method: 'DELETE', signal },
    ),
}

export const opexCategoriesManagementApi: CatalogManagementClient<
  OpexCategory,
  OpexCategoryRequest
> = catalogManagementClient<OpexCategory, OpexCategoryRequest>('/api/opex/categories')
