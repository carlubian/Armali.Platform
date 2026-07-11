import { render, screen } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'

afterEach(() => vi.unstubAllGlobals())
// BrowserRouter reads jsdom's shared history, so reset the URL between tests;
// otherwise the catch-all redirect from a previous test leaks into the next.
beforeEach(() => window.history.pushState({}, '', '/'))

test('renders the gallery foundation for a signed-in user', async () => {
  // The shell only mounts once the session check resolves, so stub the identity
  // endpoint and wait for the authenticated view to appear.
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      if (String(input).endsWith('/api/auth/me'))
        return new Response(JSON.stringify({ id: 'u1', username: 'ada', roles: [] }), { status: 200, headers: { 'content-type': 'application/json' } })
      return new Response('{}', { status: 200, headers: { 'content-type': 'application/json' } })
    }),
  )

  const { App } = await import('./App')
  render(<App />)
  expect(await screen.findByRole('heading', { name: 'Gallery' })).toBeInTheDocument()
})

test('an admin lands on the account management page and sees existing accounts', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input)
      if (url.endsWith('/api/auth/me'))
        return new Response(JSON.stringify({ id: 'admin-1', username: 'root', roles: ['Admin'] }), { status: 200, headers: { 'content-type': 'application/json' } })
      if (url.endsWith('/api/admin/accounts/'))
        return new Response(JSON.stringify({ accounts: [{ id: 'admin-1', username: 'root', role: 'Admin' }, { id: 'u2', username: 'ada', role: 'User' }] }), { status: 200, headers: { 'content-type': 'application/json' } })
      return new Response('{}', { status: 200, headers: { 'content-type': 'application/json' } })
    }),
  )

  const { App } = await import('./App')
  render(<App />)
  expect(await screen.findByRole('heading', { name: 'Manage accounts' })).toBeInTheDocument()
  expect(await screen.findByText('ada')).toBeInTheDocument()
})
