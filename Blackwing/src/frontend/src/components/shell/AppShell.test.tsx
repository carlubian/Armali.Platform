import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'

import '@/app/i18n/i18n'
import { platform } from '@/app/i18n/resources'
import { createQueryClient } from '@/app/query/queryClient'
import { SessionProvider } from '@/app/session/SessionContext'
import { AppShell } from '@/components/shell/AppShell'
import {
  adminSession,
  mockBackend,
  userSession,
  type MockSession,
} from '@/test/backend'

const nav = platform.shell.nav

async function renderShell(session: MockSession) {
  mockBackend({ session })
  const result = render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={['/']}>
        <SessionProvider>
          <Routes>
            <Route element={<AppShell />}>
              <Route index element={<p>contenido</p>} />
              <Route path="admin/users" element={<p>cuentas</p>} />
            </Route>
          </Routes>
        </SessionProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  // Wait for the identity, not just the chrome: the navigation renders while the
  // session query is still in flight, and the role-gated entries only appear
  // once it has answered.
  await screen.findByText(session.displayName)
  return result
}

afterEach(() => vi.restoreAllMocks())

describe('AppShell', () => {
  it('renders the brand and the five prototype navigation entries in order', async () => {
    const { container } = await renderShell(userSession)
    expect(screen.getByText(platform.app.name)).toBeInTheDocument()

    const entries = [...container.querySelectorAll('.bw-nav__item')].map(
      (element) => element.textContent,
    )
    expect(entries).toEqual([
      nav.gallery.label,
      nav.upload.label,
      nav.review.label,
      nav.tags.label,
      nav.settings.label,
    ])
  })

  it('links the gallery and disables the sections that belong to later phases', async () => {
    await renderShell(userSession)
    const navigation = screen.getByRole('navigation', {
      name: platform.shell.primaryNavigation,
    })

    expect(
      within(navigation).getByRole('link', { name: nav.gallery.label }),
    ).toHaveAttribute('href', '/')
    for (const label of [
      nav.upload.label,
      nav.review.label,
      nav.tags.label,
      nav.settings.label,
    ]) {
      expect(within(navigation).getByRole('button', { name: label })).toBeDisabled()
    }
  })

  it('marks the active entry and titles the topbar from it', async () => {
    await renderShell(userSession)
    expect(screen.getByRole('link', { name: nav.gallery.label })).toHaveClass('active')
    expect(
      screen.getByRole('heading', { level: 1, name: nav.gallery.title }),
    ).toBeVisible()
    expect(screen.getByText(nav.gallery.eyebrow)).toBeVisible()
  })

  it('renders the routed screen inside the aurora content area', async () => {
    const { container } = await renderShell(userSession)
    const body = container.querySelector('.bw-body')
    expect(body).not.toBeNull()
    expect(body).toHaveClass('armali-aurora')
    expect(within(body as HTMLElement).getByText('contenido')).toBeInTheDocument()
  })

  it('shows the signed-in account and an action to sign out', async () => {
    await renderShell(userSession)
    expect(screen.getByText(userSession.displayName)).toBeVisible()
    expect(
      screen.getByRole('button', { name: platform.auth.signOut }),
    ).toBeInTheDocument()
  })

  it('offers the administration entry to an administrator', async () => {
    await renderShell(adminSession)
    const navigation = screen.getByRole('navigation', {
      name: platform.shell.primaryNavigation,
    })
    expect(
      within(navigation).getByRole('link', { name: nav.admin.label }),
    ).toHaveAttribute('href', '/admin/users')
  })

  it('does not render the administration entry for a plain user', async () => {
    await renderShell(userSession)
    const navigation = screen.getByRole('navigation', {
      name: platform.shell.primaryNavigation,
    })
    // Hidden outright rather than disabled: a disabled entry would still
    // advertise a surface this account cannot reach.
    expect(within(navigation).queryByText(nav.admin.label)).toBeNull()
  })

  it('signs the account out through the session context', async () => {
    const user = userEvent.setup()
    await renderShell(adminSession)

    await user.click(screen.getByRole('button', { name: platform.auth.signOut }))
    // The guard navigates to `/login`, which this harness does not route, so the
    // shell unmounts entirely: the identity is gone from the document.
    await waitFor(() => expect(screen.queryByText(adminSession.displayName)).toBeNull())
  })
})
