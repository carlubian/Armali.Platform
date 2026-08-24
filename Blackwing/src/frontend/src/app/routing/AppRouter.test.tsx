import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import { platform } from '@/app/i18n/resources'
import { adminSession, mockBackend, userSession } from '@/test/backend'

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

afterEach(() => vi.restoreAllMocks())

describe('route guards', () => {
  it('sends a visitor without a session to the login screen', async () => {
    mockBackend({ session: null })
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: platform.auth.login.title }),
    ).toBeVisible()
    await waitFor(() => expect(window.location.pathname).toBe('/login'))
  })

  it('keeps a protected screen out of reach until the session query answers', async () => {
    mockBackend({ session: null })
    render(<App />)

    // Before the answer arrives the guard shows the loading screen, never the
    // shell: an unauthenticated visitor must not glimpse application chrome.
    expect(screen.getByRole('status', { name: platform.session.loading })).toBeVisible()
    expect(screen.queryByRole('navigation')).toBeNull()
    await screen.findByRole('heading', { name: platform.auth.login.title })
  })

  it('renders the gallery for an authenticated account', async () => {
    mockBackend({ session: userSession })
    render(<App />)

    expect(
      await screen.findByRole('heading', {
        level: 1,
        name: platform.shell.nav.gallery.title,
      }),
    ).toBeVisible()
    expect(window.location.pathname).toBe('/')
  })

  it('answers an unknown path with the not-found screen', async () => {
    window.history.replaceState({}, '', '/no-existe')
    mockBackend({ session: adminSession })
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: platform.session.notFoundTitle }),
    ).toBeVisible()
  })
})
