/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'

import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

/**
 * Resolve the backend origin the development proxy forwards `/api` to.
 *
 * The browser always requests relative `/api/...` URLs so session and
 * antiforgery cookies stay same-origin; Vite proxies them during local
 * development, and the production Caddy ingress routes the same path. The target
 * is a Node-side configuration input (never a `VITE_*` browser variable) and
 * defaults to the backend HTTP launch profile. See
 * docs/planning/FRONTEND_FOUNDATION_DECISIONS.md.
 */
function resolveProxyTarget(): string {
  const fallback = 'http://localhost:5004'
  const override = process.env.SEGARIS_FRONTEND_PROXY_TARGET
  if (override === undefined || override === '') {
    return fallback
  }

  let parsed: URL
  try {
    parsed = new URL(override)
  } catch {
    throw new Error(
      `SEGARIS_FRONTEND_PROXY_TARGET must be an absolute HTTP or HTTPS origin, received: ${override}`,
    )
  }

  if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
    throw new Error(
      `SEGARIS_FRONTEND_PROXY_TARGET must use http or https, received: ${override}`,
    )
  }

  return parsed.origin
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': {
        target: resolveProxyTarget(),
        changeOrigin: false,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    include: ['src/**/*.test.{ts,tsx}'],
    env: {
      VITE_API_BASE_URL: '/api',
      VITE_APP_VERSION: 'test',
    },
    coverage: {
      provider: 'v8',
      reportsDirectory: './coverage',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'src/vite-env.d.ts'],
    },
  },
})
