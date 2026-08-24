import { render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import { platform } from '@/app/i18n/resources'
import { mockBackend, userSession } from '@/test/backend'

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

afterEach(() => vi.restoreAllMocks())

describe('application shell and routing', () => {
  it('renders the boot screen inside the shell at the root route', async () => {
    mockBackend({ session: userSession })
    render(<App />)

    expect(
      await screen.findByRole('navigation', {
        name: platform.shell.primaryNavigation,
      }),
    ).toBeVisible()
    expect(
      screen.getByRole('heading', { level: 1, name: platform.shell.nav.gallery.title }),
    ).toBeVisible()
    expect(screen.getByText(platform.startup.cardTitle)).toBeVisible()
  })

  it('answers an unknown path with the not-found screen', async () => {
    window.history.replaceState({}, '', '/no-existe')
    mockBackend({ session: userSession })
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: platform.session.notFoundTitle }),
    ).toBeVisible()
  })

  it('provides a single query client to the tree', () => {
    expect(appQueryClient.getDefaultOptions().queries?.refetchOnWindowFocus).toBe(false)
    expect(appQueryClient.getDefaultOptions().mutations?.retry).toBe(false)
  })
})
