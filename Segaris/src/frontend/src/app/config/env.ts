/**
 * Validated, typed access to the public build-time configuration.
 *
 * This module is the single place feature and module code reads
 * `import.meta.env` through. Values originate from `VITE_*` variables embedded
 * by Vite at build time and are therefore public. See
 * docs/planning/FRONTEND_FOUNDATION_DECISIONS.md.
 */

export interface AppConfig {
  /** Relative, same-origin API path prefix (for example `/api`). */
  readonly apiBaseUrl: string
  /** Non-secret release identifier surfaced in safe diagnostics. */
  readonly appVersion: string
}

function readApiBaseUrl(raw: string | undefined): string {
  const value = raw?.trim()
  if (value === undefined || value === '') {
    throw new Error('VITE_API_BASE_URL is required.')
  }

  // A same-origin relative path keeps session and antiforgery cookies working
  // identically in local development and when deployed behind Caddy.
  if (!value.startsWith('/')) {
    throw new Error(
      `VITE_API_BASE_URL must be a same-origin relative path beginning with "/", received: ${value}`,
    )
  }

  // Normalise away a trailing slash so callers can compose `${apiBaseUrl}/path`.
  return value.length > 1 && value.endsWith('/') ? value.slice(0, -1) : value
}

function readAppVersion(raw: string | undefined): string {
  const value = raw?.trim()
  return value === undefined || value === '' ? 'development' : value
}

export const appConfig: AppConfig = {
  apiBaseUrl: readApiBaseUrl(import.meta.env.VITE_API_BASE_URL),
  appVersion: readAppVersion(import.meta.env.VITE_APP_VERSION),
}
