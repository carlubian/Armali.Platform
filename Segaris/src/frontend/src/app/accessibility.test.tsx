import { render, screen } from '@testing-library/react'
import axe from 'axe-core'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'

const session = {
  userId: 1,
  userName: 'admin',
  displayName: 'Household Admin',
  language: 'en-GB',
  roles: ['Admin'],
  avatarUrl: null,
}

const profile = {
  displayName: session.displayName,
  language: session.language,
  avatarUrl: null,
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function mockApi(authenticated = true) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url =
      typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
    if (url === '/api/session') {
      return Promise.resolve(authenticated ? json(session) : json({}, 401))
    }
    if (url === '/api/session/profile') return Promise.resolve(json(profile))
    if (url.startsWith('/api/admin/users?')) {
      return Promise.resolve(json({ items: [], page: 1, pageSize: 12, totalCount: 0 }))
    }
    return Promise.reject(new Error(`Unexpected request: ${url}`))
  })
}

async function expectNoAccessibilityViolations() {
  const result = await axe.run(document.body, {
    rules: { 'color-contrast': { enabled: false } },
  })
  expect(result.violations).toEqual([])
}

beforeEach(() => appQueryClient.clear())
afterEach(() => vi.restoreAllMocks())

describe('core screen accessibility', () => {
  it.each([
    ['/login', false, 'Welcome home'],
    ['/', true, 'Choose a module'],
    ['/profile', true, 'My profile'],
    ['/users', true, 'Household users'],
  ] as const)(
    'has no automated violations on %s',
    async (path, authenticated, heading) => {
      window.history.replaceState({}, '', path)
      mockApi(authenticated)
      render(<App />)
      await screen.findByRole('heading', { name: heading })
      await expectNoAccessibilityViolations()
    },
  )
})
