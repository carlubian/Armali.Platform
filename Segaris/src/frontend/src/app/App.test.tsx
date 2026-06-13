import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import { SESSION_EXPIRED_EVENT } from '@/app/api/client'

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

function mockAuthenticatedFetch() {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
    const url =
      typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
    if (url === '/api/session/profile') return Promise.resolve(json(profile))
    if (url === '/api/session/antiforgery') {
      return Promise.resolve(json({ csrfToken: 'token' }))
    }
    if (url === '/api/session' && init?.method === 'DELETE') {
      return Promise.resolve(new Response(null, { status: 204 }))
    }
    if (url === '/api/session') return Promise.resolve(json(session))
    return Promise.reject(new Error(`Unexpected request: ${url}`))
  })
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

afterEach(() => vi.restoreAllMocks())

describe('application routing and session', () => {
  it('redirects unauthenticated users to login', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(json({}, 401))
    render(<App />)
    expect(
      await screen.findByRole('heading', { name: 'Welcome home' }),
    ).toBeInTheDocument()
    expect(window.location.pathname).toBe('/login')
  })

  it('loads the authenticated shell and launcher', async () => {
    mockAuthenticatedFetch()
    render(<App />)
    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Household Admin')).toBeInTheDocument()
  })

  it('renders the explicit not-found route', async () => {
    mockAuthenticatedFetch()
    window.history.replaceState({}, '', '/missing')
    render(<App />)
    expect(
      await screen.findByRole('heading', { name: "We can't find that page" }),
    ).toBeInTheDocument()
    expect(screen.getByText('/missing')).toBeInTheDocument()
  })

  it('shows loading while the initial session request is pending', () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => new Promise(() => undefined))
    render(<App />)
    expect(screen.getByLabelText('Loading Segaris')).toBeInTheDocument()
  })

  it('redirects to login when an active session expires', async () => {
    mockAuthenticatedFetch()
    render(<App />)
    await screen.findByRole('heading', { name: 'Choose a module' })
    window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
    expect(
      await screen.findByRole('heading', { name: 'Welcome home' }),
    ).toBeInTheDocument()
  })

  it('signs out, clears the session and returns to login', async () => {
    const fetchMock = mockAuthenticatedFetch()
    render(<App />)
    const signOut = await screen.findByRole('button', { name: 'Sign out' })
    fireEvent.click(signOut)
    await screen.findByRole('heading', { name: 'Welcome home' })
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/session',
      expect.objectContaining({ method: 'DELETE' }),
    )
  })

  it('shows service unavailable and retries on demand', async () => {
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(json(session))
      .mockResolvedValueOnce(json(profile))
    render(<App />)
    const retry = await screen.findByRole('button', { name: 'Try again' })
    fireEvent.click(retry)
    await waitFor(() =>
      expect(
        screen.getByRole('heading', { name: 'Choose a module' }),
      ).toBeInTheDocument(),
    )
    expect(fetchMock).toHaveBeenCalledTimes(3)
  })

  it('treats exhausted startup server failures as unavailable', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(json({}, 503))
    render(<App />)
    expect(
      await screen.findByRole(
        'heading',
        {
          name: "Segaris can't reach the household server",
        },
        { timeout: 3_000 },
      ),
    ).toBeInTheDocument()
  })
})
