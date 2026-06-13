import { defineConfig, devices } from '@playwright/test'

/**
 * Full-stack end-to-end configuration.
 *
 * Specs live under the repository's `tests/frontend/e2e/` directory because they
 * validate the deployed frontend boundary rather than one frontend module. The
 * sign-in journey runs against the full stack and is skipped without seeded
 * credentials. Profile uses the same seeded-account contract; the admin
 * journey is added with the administrative-user wave.
 */
const port = 4173
const host = '127.0.0.1'

export default defineConfig({
  testDir: '../../tests/frontend/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? 'github' : 'list',
  use: {
    baseURL: `http://${host}:${port}`,
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    // Invoke the local Vite binary directly: Playwright inherits the
    // node_modules/.bin PATH from the `test:e2e` package script, and the
    // frontend is launched the same way in CI and for contributors.
    command: `vite build && vite preview --host ${host} --port ${port} --strictPort`,
    url: `http://${host}:${port}`,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    // Supply the public build-time contract so the smoke test is self-contained
    // without a committed local `.env`. Vite exposes VITE_* values from the
    // process environment in addition to `.env` files.
    env: {
      VITE_API_BASE_URL: '/api',
      VITE_APP_VERSION: 'e2e',
    },
  },
})
