import { apiRequest, resetCsrfToken } from './client'

/**
 * The signed-in account, as `/api/session` returns it.
 *
 * Blackwing has no profile surface: there is no language (the interface is only
 * Spanish) and no avatar. Anything beyond identity and roles belongs to the
 * account administration endpoints.
 */
export interface Session {
  userId: number
  userName: string
  displayName: string
  roles: string[]
}

export interface SignInCredentials {
  userName: string
  password: string
}

export const sessionApi = {
  getSession: (signal?: AbortSignal) => apiRequest<Session>('/session', { signal }),
  signIn: async (credentials: SignInCredentials, signal?: AbortSignal) => {
    await apiRequest<void>('/session', {
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
    apiRequest<void>('/session', { method: 'DELETE', signal }),
}
