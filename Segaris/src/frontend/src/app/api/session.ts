import { apiRequest } from './client'

export interface Session {
  userId: number
  userName: string
  displayName: string
  language: string
  roles: string[]
  avatarUrl: string | null
}

export interface Profile {
  displayName: string
  language: string
  avatarUrl: string | null
}

export const sessionApi = {
  getSession: (signal?: AbortSignal) => apiRequest<Session>('/api/session', { signal }),
  getProfile: (signal?: AbortSignal) =>
    apiRequest<Profile>('/api/session/profile', { signal }),
  signOut: (signal?: AbortSignal) =>
    apiRequest<void>('/api/session', { method: 'DELETE', signal }),
}
