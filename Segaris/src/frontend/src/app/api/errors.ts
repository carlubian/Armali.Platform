export type ApiErrorKind =
  | 'authentication-expired'
  | 'authorization-denied'
  | 'validation'
  | 'not-found'
  | 'transient'
  | 'unavailable'
  | 'unexpected'

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
  errors?: Record<string, string[]>
  traceId?: string
}

export class ApiError extends Error {
  constructor(
    public readonly kind: ApiErrorKind,
    public readonly status: number | null,
    public readonly problem?: ProblemDetails,
  ) {
    super(problem?.detail ?? problem?.title ?? kind)
    this.name = 'ApiError'
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}

export function classifyStatus(status: number): ApiErrorKind {
  if (status === 401) return 'authentication-expired'
  if (status === 403) return 'authorization-denied'
  if (status === 400 || status === 409 || status === 422) return 'validation'
  if (status === 404) return 'not-found'
  if (status === 408 || status === 425 || status === 429 || status >= 500) {
    return 'transient'
  }
  return 'unexpected'
}
