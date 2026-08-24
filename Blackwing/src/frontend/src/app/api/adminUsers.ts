import { apiRequest } from './client'

export interface AdminUser {
  id: number
  userName: string
  displayName: string
  roles: string[]
  isActive: boolean
  createdAt: string
}

export interface CreateUserRequest {
  userName: string
  password: string
  role: string
}

/**
 * Account administration: the entire administrative surface of Blackwing.
 *
 * There is deliberately no endpoint here that returns another account's
 * content. An administrator manages accounts and nothing else; the ownership
 * perimeter has no administrative exception.
 *
 * The listing is not paginated: the roster of a single household fits in one
 * response, so it comes back complete and ordered by identifier.
 */
export const adminUsersApi = {
  list: (signal?: AbortSignal) => apiRequest<AdminUser[]>('/admin/users', { signal }),
  create: (request: CreateUserRequest, signal?: AbortSignal) =>
    apiRequest<AdminUser>('/admin/users', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  resetPassword: (id: number, newPassword: string, signal?: AbortSignal) =>
    apiRequest<void>(`/admin/users/${id}/password`, {
      method: 'POST',
      body: JSON.stringify({ newPassword }),
      signal,
    }),
  activate: (id: number, signal?: AbortSignal) =>
    apiRequest<void>(`/admin/users/${id}/activate`, { method: 'POST', signal }),
  deactivate: (id: number, signal?: AbortSignal) =>
    apiRequest<void>(`/admin/users/${id}/deactivate`, { method: 'POST', signal }),
}
