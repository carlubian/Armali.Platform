import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type MaintenanceStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled'
export type MaintenancePriority = 'Low' | 'Medium' | 'High'
export type MaintenanceVisibility = 'Public' | 'Private'
export type MaintenanceSortField =
  | 'title'
  | 'type'
  | 'status'
  | 'priority'
  | 'dueDate'
  | 'visibility'
export type MaintenanceSortDirection = 'asc' | 'desc'

export const maintenancePageSizes = [10, 25, 50, 100] as const
export type MaintenancePageSize = (typeof maintenancePageSizes)[number]
export const maintenanceRoutePath = '/maintenance' as const

export interface MaintenanceType {
  id: number
  name: string
  sortOrder: number
}

export interface MaintenanceTypeRequest {
  name: string
}

export interface MaintenanceTaskAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
}

export interface MaintenanceTaskSummary {
  id: number
  title: string
  maintenanceTypeId: number
  maintenanceTypeName: string
  status: MaintenanceStatus
  priority: MaintenancePriority
  assetId: number | null
  assetName: string | null
  dueDate: string | null
  visibility: MaintenanceVisibility
  creatorId: number
  creatorName: string
}

export interface MaintenanceTask extends MaintenanceTaskSummary {
  completedDate: string | null
  notes: string | null
  attachments: MaintenanceTaskAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface MaintenanceTaskListQuery {
  search?: string | null
  type?: number | null
  status?: MaintenanceStatus | null
  priority?: MaintenancePriority | null
  asset?: number | null
  visibility?: MaintenanceVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: MaintenanceSortField
  sortDirection?: MaintenanceSortDirection
}

export interface CreateMaintenanceTaskRequest {
  title: string
  maintenanceTypeId: number
  status: MaintenanceStatus
  priority: MaintenancePriority
  dueDate: string | null
  notes: string | null
  assetId: number | null
  visibility: MaintenanceVisibility
}

export type UpdateMaintenanceTaskRequest = CreateMaintenanceTaskRequest

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

export const maintenanceApi = {
  types: (signal?: AbortSignal) =>
    apiRequest<MaintenanceType[]>('/api/maintenance/types', { signal }),
  listTasks: (query: MaintenanceTaskListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<MaintenanceTaskSummary>>(
      `/api/maintenance/tasks${buildQuery(query)}`,
      { signal },
    ),
  getTask: (taskId: number, signal?: AbortSignal) =>
    apiRequest<MaintenanceTask>(`/api/maintenance/tasks/${taskId}`, { signal }),
  createTask: (request: CreateMaintenanceTaskRequest, signal?: AbortSignal) =>
    apiRequest<MaintenanceTask>('/api/maintenance/tasks', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateTask: (
    taskId: number,
    request: UpdateMaintenanceTaskRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<MaintenanceTask>(`/api/maintenance/tasks/${taskId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteTask: (taskId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/maintenance/tasks/${taskId}`, {
      method: 'DELETE',
      signal,
    }),
  listTaskAttachments: (taskId: number, signal?: AbortSignal) =>
    apiRequest<MaintenanceTaskAttachment[]>(
      `/api/maintenance/tasks/${taskId}/attachments`,
      { signal },
    ),
  uploadTaskAttachment: (taskId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<MaintenanceTaskAttachment>(
      `/api/maintenance/tasks/${taskId}/attachments`,
      {
        method: 'POST',
        body,
        signal,
        timeoutMs: 60_000,
      },
    )
  },
  taskAttachmentDownloadUrl: (taskId: number, attachmentId: string) =>
    `/api/maintenance/tasks/${taskId}/attachments/${attachmentId}`,
  deleteTaskAttachment: (taskId: number, attachmentId: string, signal?: AbortSignal) =>
    apiRequest<void>(`/api/maintenance/tasks/${taskId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
}

export const maintenanceTypesManagementApi: CatalogManagementClient<
  MaintenanceType,
  MaintenanceTypeRequest
> = catalogManagementClient<MaintenanceType, MaintenanceTypeRequest>(
  '/api/maintenance/types',
)
