import { apiRequest, resetCsrfToken } from './client'

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

export interface UpdateProfileRequest {
  displayName: string
  language: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface AvatarResponse {
  avatarUrl: string
  contentType: string
  size: number
}

export const sessionApi = {
  getSession: (signal?: AbortSignal) => apiRequest<Session>('/api/session', { signal }),
  getProfile: (signal?: AbortSignal) =>
    apiRequest<Profile>('/api/session/profile', { signal }),
  updateProfile: (profile: UpdateProfileRequest, signal?: AbortSignal) =>
    apiRequest<Profile>('/api/session/profile', {
      method: 'PUT',
      body: JSON.stringify(profile),
      signal,
    }),
  changePassword: (passwords: ChangePasswordRequest, signal?: AbortSignal) =>
    apiRequest<void>('/api/session/password', {
      method: 'POST',
      body: JSON.stringify(passwords),
      signal,
    }),
  uploadAvatar: (file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<AvatarResponse>('/api/session/profile/avatar', {
      method: 'PUT',
      body,
      signal,
    })
  },
  removeAvatar: (signal?: AbortSignal) =>
    apiRequest<void>('/api/session/profile/avatar', {
      method: 'DELETE',
      signal,
    }),
  signIn: async (credentials: SignInCredentials, signal?: AbortSignal) => {
    await apiRequest<void>('/api/session', {
      method: 'POST',
      body: JSON.stringify(credentials),
      // A 401 here is an invalid credential, not an expired session: keep it
      // local to the login form instead of triggering the global redirect.
      suppressSessionExpired: true,
      signal,
    })
    // The antiforgery token fetched to send this request was bound to the
    // anonymous identity. Antiforgery tokens are tied to the user, so discard
    // it now that a session exists; the next mutation fetches one bound to the
    // authenticated user instead of failing validation with a 400.
    resetCsrfToken()
  },
  signOut: (signal?: AbortSignal) =>
    apiRequest<void>('/api/session', { method: 'DELETE', signal }),
}
