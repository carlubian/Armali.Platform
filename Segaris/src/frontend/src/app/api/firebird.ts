import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'
import type { PaginatedResponse } from './adminUsers'

export type FirebirdPersonStatus = 'Unknown' | 'Active' | 'Unavailable' | 'Blocked'
export type FirebirdVisibility = 'Public' | 'Private'
export type FirebirdPersonSortField =
  | 'name'
  | 'category'
  | 'status'
  | 'birthday'
  | 'visibility'
export type FirebirdSortDirection = 'asc' | 'desc'

export const firebirdPersonStatuses = [
  'Unknown',
  'Active',
  'Unavailable',
  'Blocked',
] as const
export const firebirdVisibilities = ['Public', 'Private'] as const
export const firebirdPageSizes = [10, 25, 50, 100] as const
export type FirebirdPageSize = (typeof firebirdPageSizes)[number]
export const firebirdRoutePath = '/people' as const

export interface PersonCategory {
  id: number
  name: string
  sortOrder: number
}

export interface PersonCategoryRequest {
  name: string
}

export interface UsernamePlatform {
  id: number
  name: string
  sortOrder: number
}

export interface UsernamePlatformRequest {
  name: string
}

export interface PersonAvatar {
  attachmentId: string | null
  url: string | null
  source: 'avatar' | 'placeholder'
}

export interface PersonSummary {
  id: number
  name: string
  categoryId: number
  categoryName: string
  status: FirebirdPersonStatus
  birthdayMonth: number | null
  birthdayDay: number | null
  visibility: FirebirdVisibility
  avatar: PersonAvatar
  creatorId: number
  creatorName: string
}

export interface Username {
  id: number
  platformId: number
  platformName: string
  handle: string
  notes: string | null
}

export interface Interaction {
  id: number
  date: string
  description: string
}

export interface Person extends PersonSummary {
  notes: string | null
  usernames: Username[]
  interactions: Interaction[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface PersonListQuery {
  search?: string | null
  category?: number | null
  status?: FirebirdPersonStatus | null
  visibility?: FirebirdVisibility | null
  creator?: number | null
  page?: number
  pageSize?: number
  sort?: FirebirdPersonSortField
  sortDirection?: FirebirdSortDirection
}

export interface CreatePersonRequest {
  name: string
  categoryId: number
  status: FirebirdPersonStatus
  birthdayMonth: number | null
  birthdayDay: number | null
  notes: string | null
  visibility: FirebirdVisibility
}

export type UpdatePersonRequest = CreatePersonRequest

export interface UsernameRequest {
  platformId: number
  handle: string
  notes: string | null
}

export interface InteractionRequest {
  date: string
  description: string
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

export const firebirdApi = {
  categories: (signal?: AbortSignal) =>
    apiRequest<PersonCategory[]>('/api/people/categories', { signal }),
  platforms: (signal?: AbortSignal) =>
    apiRequest<UsernamePlatform[]>('/api/people/platforms', { signal }),
  listPeople: (query: PersonListQuery = {}, signal?: AbortSignal) =>
    apiRequest<PaginatedResponse<PersonSummary>>(`/api/people${buildQuery(query)}`, {
      signal,
    }),
  getPerson: (personId: number, signal?: AbortSignal) =>
    apiRequest<Person>(`/api/people/${personId}`, { signal }),
  createPerson: (request: CreatePersonRequest, signal?: AbortSignal) =>
    apiRequest<Person>('/api/people', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updatePerson: (
    personId: number,
    request: UpdatePersonRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Person>(`/api/people/${personId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deletePerson: (personId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/people/${personId}`, {
      method: 'DELETE',
      signal,
    }),
  avatarDownloadUrl: (personId: number) => `/api/people/${personId}/avatar`,
  uploadAvatar: (personId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<PersonAvatar>(`/api/people/${personId}/avatar`, {
      method: 'PUT',
      body,
      signal,
      timeoutMs: 60_000,
    })
  },
  deleteAvatar: (personId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/people/${personId}/avatar`, {
      method: 'DELETE',
      signal,
    }),
  listUsernames: (personId: number, signal?: AbortSignal) =>
    apiRequest<Username[]>(`/api/people/${personId}/usernames`, { signal }),
  createUsername: (personId: number, request: UsernameRequest, signal?: AbortSignal) =>
    apiRequest<Username>(`/api/people/${personId}/usernames`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateUsername: (
    personId: number,
    usernameId: number,
    request: UsernameRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Username>(`/api/people/${personId}/usernames/${usernameId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteUsername: (personId: number, usernameId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/people/${personId}/usernames/${usernameId}`, {
      method: 'DELETE',
      signal,
    }),
  listInteractions: (personId: number, signal?: AbortSignal) =>
    apiRequest<Interaction[]>(`/api/people/${personId}/interactions`, { signal }),
  createInteraction: (
    personId: number,
    request: InteractionRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Interaction>(`/api/people/${personId}/interactions`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateInteraction: (
    personId: number,
    interactionId: number,
    request: InteractionRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Interaction>(`/api/people/${personId}/interactions/${interactionId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteInteraction: (personId: number, interactionId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/people/${personId}/interactions/${interactionId}`, {
      method: 'DELETE',
      signal,
    }),
}

export const personCategoriesManagementApi: CatalogManagementClient<
  PersonCategory,
  PersonCategoryRequest
> = catalogManagementClient<PersonCategory, PersonCategoryRequest>(
  '/api/people/categories',
)

export const usernamePlatformsManagementApi: CatalogManagementClient<
  UsernamePlatform,
  UsernamePlatformRequest
> = catalogManagementClient<UsernamePlatform, UsernamePlatformRequest>(
  '/api/people/platforms',
)
