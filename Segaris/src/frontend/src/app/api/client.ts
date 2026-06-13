import { ApiError, classifyStatus, type ProblemDetails } from './errors'

export const SESSION_EXPIRED_EVENT = 'segaris:session-expired'

let csrfToken: string | null = null
const requestTimeoutMs = 8_000

async function fetchWithTimeout(
  input: RequestInfo | URL,
  init: RequestInit,
): Promise<Response> {
  const controller = new AbortController()
  const signal =
    init.signal == null
      ? controller.signal
      : AbortSignal.any([init.signal, controller.signal])
  let timeoutId: ReturnType<typeof setTimeout> | undefined

  try {
    return await Promise.race([
      fetch(input, { ...init, signal }),
      new Promise<Response>((_, reject) => {
        timeoutId = setTimeout(() => {
          controller.abort()
          reject(new DOMException('The request timed out.', 'TimeoutError'))
        }, requestTimeoutMs)
      }),
    ])
  } finally {
    if (timeoutId !== undefined) clearTimeout(timeoutId)
  }
}

function isMutation(method: string): boolean {
  return !['GET', 'HEAD', 'OPTIONS'].includes(method.toUpperCase())
}

export interface ApiRequestOptions extends RequestInit {
  /**
   * When true, a `401` response does not dispatch the global session-expired
   * event. The sign-in request uses this because a `401` there means invalid
   * credentials, not an expired session, and must surface as a form error.
   */
  suppressSessionExpired?: boolean
}

async function parseProblem(response: Response): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get('content-type') ?? ''
  if (!contentType.includes('json')) return undefined

  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return undefined
  }
}

async function getCsrfToken(signal?: AbortSignal): Promise<string> {
  if (csrfToken !== null) return csrfToken

  let response: Response
  try {
    response = await fetchWithTimeout('/api/session/antiforgery', {
      credentials: 'same-origin',
      signal,
    })
  } catch (error) {
    throw new ApiError('unavailable', null, {
      detail: error instanceof Error ? error.message : undefined,
    })
  }

  if (!response.ok) {
    throw new ApiError(
      classifyStatus(response.status),
      response.status,
      await parseProblem(response),
    )
  }

  const payload = (await response.json()) as { csrfToken: string }
  csrfToken = payload.csrfToken
  return csrfToken
}

export async function apiRequest<T>(
  path: string,
  init: ApiRequestOptions = {},
): Promise<T> {
  const { suppressSessionExpired = false, ...requestInit } = init
  const method = requestInit.method ?? 'GET'
  const headers = new Headers(requestInit.headers)

  if (
    requestInit.body != null &&
    !(requestInit.body instanceof FormData) &&
    !headers.has('Content-Type')
  ) {
    headers.set('Content-Type', 'application/json')
  }

  if (isMutation(method)) {
    headers.set('X-CSRF-TOKEN', await getCsrfToken(requestInit.signal ?? undefined))
  }

  let response: Response
  try {
    response = await fetchWithTimeout(path, {
      ...requestInit,
      method,
      headers,
      credentials: 'same-origin',
    })
  } catch (error) {
    throw new ApiError('unavailable', null, {
      detail: error instanceof Error ? error.message : undefined,
    })
  }

  if (!response.ok) {
    const apiError = new ApiError(
      classifyStatus(response.status),
      response.status,
      await parseProblem(response),
    )
    if (apiError.kind === 'authentication-expired' && !suppressSessionExpired) {
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
    }
    throw apiError
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export function resetCsrfToken(): void {
  csrfToken = null
}
