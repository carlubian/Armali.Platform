import { apiRequest } from './client'

export interface AdminUser {
  id: number
  userName: string
  displayName: string
  roles: string[]
  isActive: boolean
  createdAt: string
  avatarUrl: string | null
}

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type AdminUserSortField = 'userName' | 'createdAt'
export type SortDirection = 'asc' | 'desc'

export interface ListUsersQuery {
  page?: number
  pageSize?: number
  sort?: AdminUserSortField
  sortDirection?: SortDirection
}

export interface CreateUserRequest {
  userName: string
  password: string
  role: string
}

export const adminUsersApi = {
  list: (query: ListUsersQuery = {}, signal?: AbortSignal) => {
    const params = new URLSearchParams()
    if (query.page != null) params.set('page', String(query.page))
    if (query.pageSize != null) params.set('pageSize', String(query.pageSize))
    if (query.sort != null) params.set('sort', query.sort)
    if (query.sortDirection != null) params.set('sortDirection', query.sortDirection)
    const search = params.toString()
    return apiRequest<PaginatedResponse<AdminUser>>(
      `/api/admin/users${search ? `?${search}` : ''}`,
      { signal },
    )
  },
  create: (request: CreateUserRequest, signal?: AbortSignal) =>
    apiRequest<AdminUser>('/api/admin/users', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  activate: (id: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/admin/users/${id}/activate`, { method: 'POST', signal }),
  deactivate: (id: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/admin/users/${id}/deactivate`, { method: 'POST', signal }),
  resetPassword: (id: number, newPassword: string, signal?: AbortSignal) =>
    apiRequest<void>(`/api/admin/users/${id}/password`, {
      method: 'POST',
      body: JSON.stringify({ newPassword }),
      signal,
    }),
}
