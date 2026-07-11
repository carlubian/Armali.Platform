import { render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'

afterEach(() => vi.unstubAllGlobals())

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
