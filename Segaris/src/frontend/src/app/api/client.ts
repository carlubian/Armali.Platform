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

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = init.method ?? 'GET'
  const headers = new Headers(init.headers)

  if (
    init.body != null &&
    !(init.body instanceof FormData) &&
    !headers.has('Content-Type')
  ) {
    headers.set('Content-Type', 'application/json')
  }

  if (isMutation(method)) {
    headers.set('X-CSRF-TOKEN', await getCsrfToken(init.signal ?? undefined))
  }

  let response: Response
  try {
    response = await fetchWithTimeout(path, {
      ...init,
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
    if (apiError.kind === 'authentication-expired') {
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
