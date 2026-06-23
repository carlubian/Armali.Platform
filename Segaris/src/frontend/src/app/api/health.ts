import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type HealthVisibility = 'Public' | 'Private'
export type HealthSortDirection = 'asc' | 'desc'
export type DiseaseSortField = 'name' | 'category'
export type MedicineSortField = 'name' | 'category'
export type HealthTab = 'diseases' | 'medicines'

export const healthRoutePath = '/health' as const
export const healthPageSizes = [10, 25, 50, 100] as const
export type HealthPageSize = (typeof healthPageSizes)[number]

export interface DiseaseCategory {
  id: number
  name: string
  sortOrder: number
}

export interface MedicineCategory {
  id: number
  name: string
  sortOrder: number
}

export interface HealthCategoryRequest {
  name: string
}

export interface DiseaseSummary {
  id: number
  name: string
  categoryId: number
  categoryName: string
  visibility: HealthVisibility
  associatedMedicineCount: number
  creatorId: number
  creatorName: string
}

export interface Disease extends DiseaseSummary {
  symptoms: string | null
  averageDurationDays: number | null
  notes: string | null
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface MedicineAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
  isPrimary: boolean
}

export interface MedicineThumbnail {
  attachmentId: string | null
  url: string | null
  source: 'primary' | 'firstImage' | 'placeholder'
}

export interface MedicineSummary {
  id: number
  name: string
  categoryId: number
  categoryName: string
  requiresPrescription: boolean
  inventoryItemId: number | null
  inventoryItemName: string | null
  visibility: HealthVisibility
  thumbnail: MedicineThumbnail
  creatorId: number
  creatorName: string
}

export interface Medicine extends MedicineSummary {
  posology: string | null
  notes: string | null
  attachments: MedicineAttachment[]
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface DiseaseListQuery {
  search?: string | null
  category?: number | null
  visibility?: HealthVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: DiseaseSortField
  sortDirection?: HealthSortDirection
}

export interface MedicineListQuery {
  search?: string | null
  category?: number | null
  requiresPrescription?: boolean | null
  visibility?: HealthVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: MedicineSortField
  sortDirection?: HealthSortDirection
}

export interface DiseaseRequest {
  name: string
  categoryId: number
  symptoms: string | null
  averageDurationDays: number | null
  notes: string | null
  visibility: HealthVisibility
}

export interface MedicineRequest {
  name: string
  categoryId: number
  posology: string | null
  requiresPrescription: boolean
  inventoryItemId: number | null
  notes: string | null
  visibility: HealthVisibility
}

function buildQuery<T extends object>(query: T): string {
  const parameters = new URLSearchParams()
  Object.entries(query as Record<string, unknown>).forEach(([key, value]) => {
    if (value == null) return
    const text =
      typeof value === 'string'
        ? value.trim()
        : typeof value === 'number' || typeof value === 'boolean'
          ? String(value)
          : ''
    if (text.length > 0) parameters.set(key, text)
  })
  const search = parameters.toString()
  return search ? `?${search}` : ''
}

export const healthApi = {
  diseaseCategories: (signal?: AbortSignal) =>
    apiRequest<DiseaseCategory[]>('/api/health/disease-categories', { signal }),
  medicineCategories: (signal?: AbortSignal) =>
    apiRequest<MedicineCategory[]>('/api/health/medicine-categories', { signal }),
  listDiseases: (query: DiseaseListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<DiseaseSummary>>(
      `/api/health/diseases${buildQuery(query)}`,
      { signal },
    ),
  getDisease: (diseaseId: number, signal?: AbortSignal) =>
    apiRequest<Disease>(`/api/health/diseases/${diseaseId}`, { signal }),
  createDisease: (request: DiseaseRequest, signal?: AbortSignal) =>
    apiRequest<Disease>('/api/health/diseases', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateDisease: (diseaseId: number, request: DiseaseRequest, signal?: AbortSignal) =>
    apiRequest<Disease>(`/api/health/diseases/${diseaseId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteDisease: (diseaseId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/health/diseases/${diseaseId}`, {
      method: 'DELETE',
      signal,
    }),
  listDiseaseMedicines: (diseaseId: number, signal?: AbortSignal) =>
    apiRequest<MedicineSummary[]>(`/api/health/diseases/${diseaseId}/medicines`, {
      signal,
    }),
  addDiseaseMedicine: (diseaseId: number, medicineId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/health/diseases/${diseaseId}/medicines/${medicineId}`, {
      method: 'POST',
      signal,
    }),
  removeDiseaseMedicine: (
    diseaseId: number,
    medicineId: number,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/health/diseases/${diseaseId}/medicines/${medicineId}`, {
      method: 'DELETE',
      signal,
    }),
  listMedicines: (query: MedicineListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<MedicineSummary>>(
      `/api/health/medicines${buildQuery(query)}`,
      { signal },
    ),
  getMedicine: (medicineId: number, signal?: AbortSignal) =>
    apiRequest<Medicine>(`/api/health/medicines/${medicineId}`, { signal }),
  createMedicine: (request: MedicineRequest, signal?: AbortSignal) =>
    apiRequest<Medicine>('/api/health/medicines', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateMedicine: (
    medicineId: number,
    request: MedicineRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Medicine>(`/api/health/medicines/${medicineId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteMedicine: (medicineId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/health/medicines/${medicineId}`, {
      method: 'DELETE',
      signal,
    }),
  listMedicineDiseases: (medicineId: number, signal?: AbortSignal) =>
    apiRequest<DiseaseSummary[]>(`/api/health/medicines/${medicineId}/diseases`, {
      signal,
    }),
  addMedicineDisease: (medicineId: number, diseaseId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/health/medicines/${medicineId}/diseases/${diseaseId}`, {
      method: 'POST',
      signal,
    }),
  removeMedicineDisease: (
    medicineId: number,
    diseaseId: number,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/health/medicines/${medicineId}/diseases/${diseaseId}`, {
      method: 'DELETE',
      signal,
    }),
  listMedicineAttachments: (medicineId: number, signal?: AbortSignal) =>
    apiRequest<MedicineAttachment[]>(
      `/api/health/medicines/${medicineId}/attachments`,
      { signal },
    ),
  uploadMedicineAttachment: (
    medicineId: number,
    file: File,
    signal?: AbortSignal,
  ) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<MedicineAttachment>(
      `/api/health/medicines/${medicineId}/attachments`,
      { method: 'POST', body, signal, timeoutMs: 60_000 },
    )
  },
  medicineAttachmentDownloadUrl: (medicineId: number, attachmentId: string) =>
    `/api/health/medicines/${medicineId}/attachments/${attachmentId}`,
  deleteMedicineAttachment: (
    medicineId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/health/medicines/${medicineId}/attachments/${attachmentId}`, {
      method: 'DELETE',
      signal,
    }),
  setPrimaryMedicineAttachment: (
    medicineId: number,
    attachmentId: string,
    signal?: AbortSignal,
  ) =>
    apiRequest<MedicineAttachment>(
      `/api/health/medicines/${medicineId}/attachments/${attachmentId}/primary`,
      { method: 'PUT', signal },
    ),
}

export const diseaseCategoriesManagementApi: CatalogManagementClient<
  DiseaseCategory,
  HealthCategoryRequest
> = catalogManagementClient<DiseaseCategory, HealthCategoryRequest>(
  '/api/health/disease-categories',
)

export const medicineCategoriesManagementApi: CatalogManagementClient<
  MedicineCategory,
  HealthCategoryRequest
> = catalogManagementClient<MedicineCategory, HealthCategoryRequest>(
  '/api/health/medicine-categories',
)
