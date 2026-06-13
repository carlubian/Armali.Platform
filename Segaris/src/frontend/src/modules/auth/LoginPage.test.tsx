import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'

const session = {
  userId: 1,
  userName: 'marina',
  displayName: 'Marina',
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

function urlOf(input: RequestInfo | URL): string {
  return typeof input === 'string'
    ? input
    : input instanceof URL
      ? input.href
      : input.url
}

/**
 * A stateful backend: anonymous until a `POST /api/session` succeeds, then the
 * session and profile endpoints start answering, mirroring the real cookie flow.
 */
function mockBackend(options: { loginStatus?: number; loginDelayMs?: number } = {}) {
  const { loginStatus = 204, loginDelayMs = 0 } = options
  let signedIn = false

  return vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
    const url = urlOf(input)
    const method = init?.method ?? 'GET'

    if (url === '/api/session/antiforgery')
      return Promise.resolve(json({ csrfToken: 'tok' }))

    if (url === '/api/session' && method === 'POST') {
      if (loginStatus < 400) signedIn = true
      const respond = () =>
        loginStatus === 204
          ? new Response(null, { status: 204 })
          : json({ title: 'Authentication failed.' }, loginStatus)
      if (loginDelayMs > 0) {
        return new Promise<Response>((resolve) =>
          setTimeout(() => resolve(respond()), loginDelayMs),
        )
      }
      return Promise.resolve(respond())
    }

    if (url === '/api/session/profile') {
      return Promise.resolve(signedIn ? json(profile) : json({}, 401))
    }
    if (url === '/api/session' && method === 'GET') {
      return Promise.resolve(signedIn ? json(session) : json({}, 401))
    }

    return Promise.reject(new Error(`Unexpected request: ${method} ${url}`))
  })
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

afterEach(() => vi.restoreAllMocks())

describe('login screen', () => {
  it('signs in with valid credentials and reaches the launcher', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByRole('heading', { name: 'Welcome home' })
    await user.type(screen.getByLabelText('Username'), 'marina')
    await user.type(screen.getByLabelText('Password'), 'passphrase')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
    // The session context is populated: the shell shows the signed-in identity.
    expect(screen.getByText('Marina')).toBeInTheDocument()
  })

  it('shows a generic error for invalid credentials without leaking account state', async () => {
    const user = userEvent.setup()
    mockBackend({ loginStatus: 401 })
    render(<App />)

    await screen.findByRole('heading', { name: 'Welcome home' })
    await user.type(screen.getByLabelText('Username'), 'marina')
    await user.type(screen.getByLabelText('Password'), 'wrong-secret')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent(
      'We could not sign you in. Check your username and password and try again.',
    )
    // The message must not reveal whether the username exists or the password matched.
    expect(alert.textContent).not.toMatch(
      /username does not exist|password is incorrect/i,
    )
    // A failed sign-in keeps the user on the login screen.
    expect(screen.getByRole('heading', { name: 'Welcome home' })).toBeInTheDocument()
    // Focus returns to the first field for an immediate retry.
    expect(screen.getByLabelText('Username')).toHaveFocus()
  })

  it('surfaces rate limiting through a distinct message', async () => {
    const user = userEvent.setup()
    mockBackend({ loginStatus: 429 })
    render(<App />)

    await screen.findByRole('heading', { name: 'Welcome home' })
    await user.type(screen.getByLabelText('Username'), 'marina')
    await user.type(screen.getByLabelText('Password'), 'passphrase')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Too many sign-in attempts. Wait a moment before trying again.',
    )
  })

  it('validates presence on the client and associates errors with their fields', async () => {
    const user = userEvent.setup()
    const fetchMock = mockBackendSpy()
    render(<App />)

    await screen.findByRole('heading', { name: 'Welcome home' })
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    const username = screen.getByLabelText('Username')
    expect(username).toHaveAccessibleDescription('Enter your username.')
    expect(username).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Password')).toHaveAccessibleDescription(
      'Enter your password.',
    )
    // No network request is made when the client-side check fails.
    expect(fetchMock).not.toHaveBeenCalledWith(
      '/api/session',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it('submits with the keyboard alone', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByRole('heading', { name: 'Welcome home' })
    await user.type(screen.getByLabelText('Username'), 'marina')
    await user.type(screen.getByLabelText('Password'), 'passphrase{Enter}')

    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
  })

  it('disables the submit action while the request is pending', async () => {
    const user = userEvent.setup()
    mockBackend({ loginDelayMs: 10_000 })
    render(<App />)

    await screen.findByRole('heading', { name: 'Welcome home' })
    await user.type(screen.getByLabelText('Username'), 'marina')
    await user.type(screen.getByLabelText('Password'), 'passphrase')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Signing in…' })).toBeDisabled(),
    )
  })
})

/** A simple anonymous backend used by the client-side validation test. */
function mockBackendSpy() {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
    const url = urlOf(input)
    const method = init?.method ?? 'GET'
    if (url === '/api/session' && method === 'GET')
      return Promise.resolve(json({}, 401))
    if (url === '/api/session/antiforgery')
      return Promise.resolve(json({ csrfToken: 'tok' }))
    return Promise.reject(new Error(`Unexpected request: ${method} ${url}`))
  })
}
