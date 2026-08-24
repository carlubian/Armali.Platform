import { vi } from 'vitest'

/**
 * A stateful fake of the Blackwing API, shared by the screen tests.
 *
 * It answers the session and account-administration endpoints of milestone 1
 * over the real `fetch` seam, so the tests exercise the actual HTTP client,
 * antiforgery handling and query cache rather than a stubbed module. Test-only
 * helper: production modules must not import it.
 */

export interface MockSession {
  userId: number
  userName: string
  displayName: string
  roles: string[]
}

export interface MockAdminUser {
  id: number
  userName: string
  displayName: string
  roles: string[]
  isActive: boolean
  createdAt: string
}

export interface MockRequest {
  method: string
  url: string
  body?: unknown
  headers: Headers
}

export interface MockBackendOptions {
  /** The session the visitor arrives with, or `null` for an anonymous one. */
  session?: MockSession | null
  /** The account a successful sign-in produces. Defaults to `session`. */
  account?: MockSession
  /** Status returned by `POST /api/session`; anything below 400 signs in. */
  loginStatus?: number
  /** Delays the login response, to observe the pending state. */
  loginDelayMs?: number
  users?: MockAdminUser[]
}

export const adminSession: MockSession = {
  userId: 1,
  userName: 'marina',
  displayName: 'Marina Velasco',
  roles: ['Admin'],
}

export const userSession: MockSession = {
  userId: 2,
  userName: 'tomas',
  displayName: 'Tomás Ferrer',
  roles: ['User'],
}

export function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function urlOf(input: RequestInfo | URL): string {
  return typeof input === 'string'
    ? input
    : input instanceof URL
      ? input.href
      : input.url
}

export function defaultUsers(): MockAdminUser[] {
  return [
    {
      id: 1,
      userName: 'marina',
      displayName: 'Marina Velasco',
      roles: ['Admin'],
      isActive: true,
      createdAt: '2026-01-15T09:00:00Z',
    },
    {
      id: 2,
      userName: 'tomas',
      displayName: 'Tomás Ferrer',
      roles: ['User'],
      isActive: true,
      createdAt: '2026-02-03T09:00:00Z',
    },
    {
      id: 3,
      userName: 'lucia',
      displayName: 'Lucía Prats',
      roles: ['User'],
      isActive: false,
      createdAt: '2026-03-21T09:00:00Z',
    },
  ]
}

export function mockBackend(options: MockBackendOptions = {}) {
  const {
    session = adminSession,
    account = session ?? adminSession,
    loginStatus = 204,
    loginDelayMs = 0,
    users = defaultUsers(),
  } = options

  let current: MockSession | null = session
  const requests: MockRequest[] = []

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
      await Promise.resolve()
      const url = urlOf(input)
      const method = (init?.method ?? 'GET').toUpperCase()
      const headers = new Headers(init?.headers)
      const body =
        typeof init?.body === 'string' ? (JSON.parse(init.body) as unknown) : undefined
      requests.push({ method, url, body, headers })

      if (url === '/api/session/antiforgery') return json({ csrfToken: 'csrf-token' })

      if (url === '/api/session' && method === 'POST') {
        if (loginStatus < 400) current = account
        const respond = () =>
          loginStatus === 204
            ? new Response(null, { status: 204 })
            : json(
                { title: 'Authentication failed.', status: loginStatus },
                loginStatus,
              )
        if (loginDelayMs > 0) {
          return new Promise<Response>((resolve) =>
            setTimeout(() => resolve(respond()), loginDelayMs),
          )
        }
        return respond()
      }

      if (url === '/api/session' && method === 'GET') {
        return current === null ? json({ title: 'Unauthorized.' }, 401) : json(current)
      }

      if (url === '/api/session' && method === 'DELETE') {
        current = null
        return new Response(null, { status: 204 })
      }

      if (url === '/api/admin/users' && method === 'GET') {
        if (current === null) return json({ title: 'Unauthorized.' }, 401)
        if (!current.roles.includes('Admin')) return json({ title: 'Forbidden.' }, 403)
        return json(users)
      }

      if (url === '/api/admin/users' && method === 'POST') {
        const request = body as { userName: string; role: string; password: string }
        if (users.some((user) => user.userName === request.userName)) {
          return json(
            { code: 'request.invalid', errors: { userName: ['Already in use.'] } },
            400,
          )
        }
        const created: MockAdminUser = {
          id: users.length + 1,
          userName: request.userName,
          displayName: request.userName,
          roles: [request.role],
          isActive: true,
          createdAt: '2026-08-24T09:00:00Z',
        }
        users.push(created)
        return json(created, 201)
      }

      const activation = /^\/api\/admin\/users\/(\d+)\/(activate|deactivate)$/.exec(url)
      if (activation !== null && method === 'POST') {
        const target = users.find((user) => user.id === Number(activation[1]))
        if (target === undefined) return json({ title: 'Not found.' }, 404)
        target.isActive = activation[2] === 'activate'
        return new Response(null, { status: 204 })
      }

      const passwordReset = /^\/api\/admin\/users\/(\d+)\/password$/.exec(url)
      if (passwordReset !== null && method === 'POST') {
        return new Response(null, { status: 204 })
      }

      return Promise.reject(new Error(`Unexpected request: ${method} ${url}`))
    })

  return { fetchMock, requests, users }
}
