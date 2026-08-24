import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import { platform } from '@/app/i18n/resources'
import { adminSession, mockBackend, userSession } from '@/test/backend'

const copy = platform.admin.users

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/admin/users')
})

afterEach(() => vi.restoreAllMocks())

describe('account administration', () => {
  it('lists every account with its role and its state', async () => {
    mockBackend({ session: adminSession })
    render(<App />)

    const rows = await screen.findAllByRole('listitem')
    expect(rows).toHaveLength(3)
    expect(within(rows[0]).getByText('Marina Velasco')).toBeVisible()
    expect(within(rows[0]).getByText(copy.roleAdmin)).toBeVisible()
    expect(within(rows[1]).getByText(copy.roleUser)).toBeVisible()
    expect(within(rows[1]).getByText(copy.statusActive)).toBeVisible()
    expect(within(rows[2]).getByText(copy.statusInactive)).toBeVisible()
  })

  it('creates an account and reflects it in the list', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ session: adminSession })
    render(<App />)

    await screen.findAllByRole('listitem')
    await user.click(screen.getByRole('button', { name: copy.newUser }))

    const dialog = await screen.findByRole('dialog', { name: copy.create.title })
    await user.type(within(dialog).getByLabelText(copy.create.username), 'candela')
    await user.selectOptions(within(dialog).getByRole('combobox'), 'Admin')
    await user.type(
      within(dialog).getByLabelText(copy.create.password),
      'una-contrasena-larga',
    )
    await user.click(within(dialog).getByRole('button', { name: copy.create.submit }))

    // The list reflects the new account, and the confirmation is announced.
    expect(await screen.findByText('candela')).toBeVisible()
    expect(await screen.findAllByRole('listitem')).toHaveLength(4)
    expect(screen.getByText(copy.create.successTitle)).toBeVisible()

    const created = requests.find(
      (request) => request.method === 'POST' && request.url === '/api/admin/users',
    )
    expect(created?.body).toEqual({
      userName: 'candela',
      role: 'Admin',
      password: 'una-contrasena-larga',
    })
    // Every mutation carries the antiforgery header the backend demands.
    expect(created?.headers.get('X-CSRF-TOKEN')).toBe('csrf-token')
  })

  it('reports a rejected username on its own field', async () => {
    const user = userEvent.setup()
    mockBackend({ session: adminSession })
    render(<App />)

    await screen.findAllByRole('listitem')
    await user.click(screen.getByRole('button', { name: copy.newUser }))

    const dialog = await screen.findByRole('dialog', { name: copy.create.title })
    // `tomas` already exists, so the backend answers with a field-level problem.
    await user.type(within(dialog).getByLabelText(copy.create.username), 'tomas')
    await user.type(
      within(dialog).getByLabelText(copy.create.password),
      'una-contrasena-larga',
    )
    await user.click(within(dialog).getByRole('button', { name: copy.create.submit }))

    const username = within(dialog).getByLabelText(copy.create.username)
    await waitFor(() =>
      expect(username).toHaveAccessibleDescription(copy.create.usernameInvalid),
    )
  })

  it('asks for confirmation before deactivating an account', async () => {
    const user = userEvent.setup()
    mockBackend({ session: adminSession })
    render(<App />)

    const rows = await screen.findAllByRole('listitem')
    await user.click(within(rows[1]).getByRole('button', { name: copy.deactivate }))

    const dialog = await screen.findByRole('dialog', {
      name: copy.deactivateConfirm.title,
    })
    await user.click(
      within(dialog).getByRole('button', { name: copy.deactivateConfirm.confirm }),
    )

    await waitFor(() =>
      expect(
        within(screen.getAllByRole('listitem')[1]).getByText(copy.statusInactive),
      ).toBeVisible(),
    )
  })

  it('refuses to let an administrator deactivate their own account', async () => {
    mockBackend({ session: adminSession })
    render(<App />)

    const rows = await screen.findAllByRole('listitem')
    expect(
      within(rows[0]).getByRole('button', { name: copy.deactivate }),
    ).toBeDisabled()
  })

  it('answers a non-administrator with the not-found screen', async () => {
    mockBackend({ session: userSession })
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: platform.session.notFoundTitle }),
    ).toBeVisible()
    // Never an access-denied screen: that would confirm the area exists.
    expect(screen.queryByRole('button', { name: copy.newUser })).toBeNull()
  })
})
