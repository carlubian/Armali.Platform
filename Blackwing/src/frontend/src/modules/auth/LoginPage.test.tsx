import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import { platform } from '@/app/i18n/resources'
import { mockBackend } from '@/test/backend'

const copy = platform.auth.login

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/login')
})

afterEach(() => vi.restoreAllMocks())

describe('login screen', () => {
  it('signs in with valid credentials and reaches the gallery', async () => {
    const user = userEvent.setup()
    mockBackend({ session: null })
    render(<App />)

    await screen.findByRole('heading', { name: copy.title })
    await user.type(screen.getByLabelText(copy.usernameLabel), 'marina')
    await user.type(screen.getByLabelText(copy.passwordLabel), 'una-frase-larga')
    await user.click(screen.getByRole('button', { name: copy.submit }))

    expect(
      await screen.findByRole('heading', {
        level: 1,
        name: platform.shell.nav.gallery.title,
      }),
    ).toBeVisible()
    // The session context is populated: the shell shows the signed-in identity.
    expect(screen.getByText('Marina Velasco')).toBeVisible()
  })

  it('shows a single generic error for invalid credentials', async () => {
    const user = userEvent.setup()
    mockBackend({ session: null, loginStatus: 401 })
    render(<App />)

    await screen.findByRole('heading', { name: copy.title })
    await user.type(screen.getByLabelText(copy.usernameLabel), 'marina')
    await user.type(screen.getByLabelText(copy.passwordLabel), 'contrasena-mala')
    await user.click(screen.getByRole('button', { name: copy.submit }))

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent(copy.errorInvalid)
    // The message must not reveal whether the account exists, whether the
    // password matched, or whether the account was deactivated.
    expect(alert.textContent).not.toMatch(/no existe|incorrecta|desactivada/i)
    expect(screen.getByRole('heading', { name: copy.title })).toBeInTheDocument()
    // Focus returns to the first field for an immediate retry.
    expect(screen.getByLabelText(copy.usernameLabel)).toHaveFocus()
  })

  it('surfaces rate limiting through a distinct message', async () => {
    const user = userEvent.setup()
    mockBackend({ session: null, loginStatus: 429 })
    render(<App />)

    await screen.findByRole('heading', { name: copy.title })
    await user.type(screen.getByLabelText(copy.usernameLabel), 'marina')
    await user.type(screen.getByLabelText(copy.passwordLabel), 'una-frase-larga')
    await user.click(screen.getByRole('button', { name: copy.submit }))

    expect(await screen.findByRole('alert')).toHaveTextContent(copy.errorRateLimited)
  })

  it('validates presence on the client without calling the backend', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ session: null })
    render(<App />)

    await screen.findByRole('heading', { name: copy.title })
    await user.click(screen.getByRole('button', { name: copy.submit }))

    const username = screen.getByLabelText(copy.usernameLabel)
    await waitFor(() => expect(username).toHaveAttribute('aria-invalid', 'true'))
    expect(username).toHaveAccessibleDescription(copy.usernameRequired)
    expect(screen.getByLabelText(copy.passwordLabel)).toHaveAccessibleDescription(
      copy.passwordRequired,
    )
    expect(
      requests.some(
        (request) => request.method === 'POST' && request.url === '/api/session',
      ),
    ).toBe(false)
  })

  it('blocks the submit action while the request is in flight', async () => {
    const user = userEvent.setup()
    mockBackend({ session: null, loginDelayMs: 10_000 })
    render(<App />)

    await screen.findByRole('heading', { name: copy.title })
    await user.type(screen.getByLabelText(copy.usernameLabel), 'marina')
    await user.type(screen.getByLabelText(copy.passwordLabel), 'una-frase-larga')
    await user.click(screen.getByRole('button', { name: copy.submit }))

    await waitFor(() =>
      expect(screen.getByRole('button', { name: copy.submitting })).toBeDisabled(),
    )
  })
})
