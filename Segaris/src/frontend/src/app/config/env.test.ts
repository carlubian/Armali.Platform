import { afterEach, describe, expect, it, vi } from 'vitest'

afterEach(() => {
  vi.unstubAllEnvs()
  vi.resetModules()
})

async function loadConfig() {
  vi.resetModules()
  return import('@/app/config/env')
}

describe('appConfig', () => {
  it('exposes the configured relative API base URL and version', async () => {
    vi.stubEnv('VITE_API_BASE_URL', '/api')
    vi.stubEnv('VITE_APP_VERSION', 'v1.2.3')

    const { appConfig } = await loadConfig()

    expect(appConfig.apiBaseUrl).toBe('/api')
    expect(appConfig.appVersion).toBe('v1.2.3')
  })

  it('strips a trailing slash from the API base URL', async () => {
    vi.stubEnv('VITE_API_BASE_URL', '/api/')

    const { appConfig } = await loadConfig()

    expect(appConfig.apiBaseUrl).toBe('/api')
  })

  it('defaults the version to "development" when unset', async () => {
    vi.stubEnv('VITE_API_BASE_URL', '/api')
    vi.stubEnv('VITE_APP_VERSION', '')

    const { appConfig } = await loadConfig()

    expect(appConfig.appVersion).toBe('development')
  })

  it('rejects an absolute cross-origin API base URL', async () => {
    vi.stubEnv('VITE_API_BASE_URL', 'https://example.com/api')

    await expect(loadConfig()).rejects.toThrow(/same-origin relative path/)
  })

  it('rejects a missing API base URL', async () => {
    vi.stubEnv('VITE_API_BASE_URL', '')

    await expect(loadConfig()).rejects.toThrow(/required/)
  })
})
