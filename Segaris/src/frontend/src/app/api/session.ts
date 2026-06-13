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

export interface SignInCredentials {
  userName: string
  password: string
}

export const sessionApi = {
  getSession: (signal?: AbortSignal) => apiRequest<Session>('/api/session', { signal }),
  getProfile: (signal?: AbortSignal) =>
    apiRequest<Profile>('/api/session/profile', { signal }),
  signIn: (credentials: SignInCredentials, signal?: AbortSignal) =>
    apiRequest<void>('/api/session', {
      method: 'POST',
      body: JSON.stringify(credentials),
      // A 401 here is an invalid credential, not an expired session: keep it
      // local to the login form instead of triggering the global redirect.
      suppressSessionExpired: true,
      signal,
    }),
  signOut: (signal?: AbortSignal) =>
    apiRequest<void>('/api/session', { method: 'DELETE', signal }),
}
