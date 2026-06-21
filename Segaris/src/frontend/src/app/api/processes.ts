import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type ProcessStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Cancelled'
export type StepExecutionState = 'Pending' | 'Completed' | 'Skipped'
export type ProcessVisibility = 'Public' | 'Private'
export type ProcessSortField = 'name' | 'category' | 'status' | 'dueDate' | 'visibility'
export type ProcessSortDirection = 'asc' | 'desc'

export const processStatuses = [
  'NotStarted',
  'InProgress',
  'Completed',
  'Cancelled',
] as const satisfies readonly ProcessStatus[]
export const stepExecutionStates = [
  'Pending',
  'Completed',
  'Skipped',
] as const satisfies readonly StepExecutionState[]
export const processVisibilities = [
  'Public',
  'Private',
] as const satisfies readonly ProcessVisibility[]
export const processPageSizes = [10, 25, 50, 100] as const
export type ProcessPageSize = (typeof processPageSizes)[number]
export const processesRoutePath = '/processes' as const

export interface ProcessCategory {
  id: number
  name: string
  sortOrder: number
}

export interface ProcessCategoryRequest {
  name: string
}

export interface ProcessAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
}

export interface ProcessStep {
  id: number
  description: string
  dueDate: string | null
  notes: string | null
  isOptional: boolean
  state: StepExecutionState
  sortOrder: number
}

export interface ProcessSummary {
  id: number
  name: string
  categoryId: number
  categoryName: string
  status: ProcessStatus
  isCancelled: boolean
  resolvedStepCount: number
  totalStepCount: number
  effectiveDueDate: string | null
  visibility: ProcessVisibility
  creatorId: number
  creatorName: string
}

export interface Process {
  id: number
  name: string
  categoryId: number
  categoryName: string
  status: ProcessStatus
  isCancelled: boolean
  dueDate: string | null
  effectiveDueDate: string | null
  notes: string | null
  resolvedStepCount: number
  totalStepCount: number
  nextPendingStepId: number | null
  visibility: ProcessVisibility
  steps: ProcessStep[]
  attachments: ProcessAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface ProcessListQuery {
  search?: string | null
  category?: number | null
  status?: ProcessStatus | null
  creator?: number | null
  visibility?: ProcessVisibility | null
  page?: number
  pageSize?: number
  sort?: ProcessSortField
  sortDirection?: ProcessSortDirection
}

export interface CreateProcessRequest {
  name: string
  categoryId: number
  dueDate: string | null
  notes: string | null
  visibility: ProcessVisibility
}

export type UpdateProcessRequest = CreateProcessRequest

export interface StepListItemRequest {
  id: number | null
  description: string
  dueDate: string | null
  notes: string | null
  isOptional: boolean
}

export interface UpdateStepListRequest {
  steps: StepListItemRequest[]
}

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

export const processesApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<ProcessCategory[]>('/api/processes/categories', { signal }),
  listProcesses: (query: ProcessListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<ProcessSummary>>(
      `/api/processes${buildQuery(query)}`,
      { signal },
    ),
  getProcess: (processId: number, signal?: AbortSignal) =>
    apiRequest<Process>(`/api/processes/${processId}`, { signal }),
  createProcess: (request: CreateProcessRequest, signal?: AbortSignal) =>
    apiRequest<Process>('/api/processes', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateProcess: (
    processId: number,
    request: UpdateProcessRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Process>(`/api/processes/${processId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteProcess: (processId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/processes/${processId}`, {
      method: 'DELETE',
      signal,
    }),
  cancelProcess: (processId: number, signal?: AbortSignal) =>
    apiRequest<Process>(`/api/processes/${processId}/cancel`, {
      method: 'POST',
      signal,
    }),
  reopenProcess: (processId: number, signal?: AbortSignal) =>
    apiRequest<Process>(`/api/processes/${processId}/reopen`, {
      method: 'POST',
      signal,
    }),
  listSteps: (processId: number, signal?: AbortSignal) =>
    apiRequest<ProcessStep[]>(`/api/processes/${processId}/steps`, { signal }),
  updateSteps: (
    processId: number,
    request: UpdateStepListRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Process>(`/api/processes/${processId}/steps`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  completeStep: (processId: number, stepId: number, signal?: AbortSignal) =>
    apiRequest<Process>(`/api/processes/${processId}/steps/${stepId}/complete`, {
      method: 'POST',
      signal,
    }),
  skipStep: (processId: number, stepId: number, signal?: AbortSignal) =>
    apiRequest<Process>(`/api/processes/${processId}/steps/${stepId}/skip`, {
      method: 'POST',
      signal,
    }),
  undoStep: (processId: number, stepId: number, signal?: AbortSignal) =>
    apiRequest<Process>(`/api/processes/${processId}/steps/${stepId}/undo`, {
      method: 'POST',
      signal,
    }),
  listAttachments: (processId: number, signal?: AbortSignal) =>
    apiRequest<ProcessAttachment[]>(`/api/processes/${processId}/attachments`, {
      signal,
    }),
  uploadAttachment: (processId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<ProcessAttachment>(`/api/processes/${processId}/attachments`, {
      method: 'POST',
      body,
      signal,
      timeoutMs: 60_000,
    })
  },
  attachmentDownloadUrl: (processId: number, attachmentId: string) =>
    `/api/processes/${processId}/attachments/${attachmentId}`,
  deleteAttachment: (processId: number, attachmentId: string, signal?: AbortSignal) =>
    apiRequest<void>(`/api/processes/${processId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
}

export const processCategoriesManagementApi: CatalogManagementClient<
  ProcessCategory,
  ProcessCategoryRequest
> = catalogManagementClient<ProcessCategory, ProcessCategoryRequest>(
  '/api/processes/categories',
)
