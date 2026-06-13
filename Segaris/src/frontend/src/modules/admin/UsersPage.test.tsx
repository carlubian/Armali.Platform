import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'

interface MockUser {
  id: number
  userName: string
  displayName: string
  roles: string[]
  isActive: boolean
  createdAt: string
}

const adminSession = {
  userId: 1,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['Admin'],
  avatarUrl: null as string | null,
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

function makeUsers(count: number): MockUser[] {
  return Array.from({ length: count }, (_, index) => {
    const id = index + 1
    return {
      id,
      userName: `member${id.toString().padStart(2, '0')}`,
      displayName: `Member ${id.toString().padStart(2, '0')}`,
      roles: id === 1 ? ['Admin'] : ['User'],
      isActive: id !== 2,
      createdAt: '2026-01-15T09:00:00Z',
    }
  })
}

function mockBackend(options: { roles?: string[]; userCount?: number } = {}) {
  const session = { ...adminSession, roles: options.roles ?? ['Admin'] }
  const users = makeUsers(options.userCount ?? 3)
  const requests: Array<{ method: string; url: string; body?: unknown }> = []

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
      await Promise.resolve()
      const url = urlOf(input)
      const method = init?.method ?? 'GET'

      if (url === '/api/session/antiforgery') return json({ csrfToken: 'admin-token' })
      if (url === '/api/session' && method === 'GET') return json(session)
      if (url === '/api/session/profile' && method === 'GET') {
        return json({
          displayName: session.displayName,
          language: session.language,
          avatarUrl: session.avatarUrl,
        })
      }

      if (url.startsWith('/api/admin/users') && method === 'GET') {
        requests.push({ method, url })
        const parsed = new URL(url, 'http://localhost')
        const page = Number(parsed.searchParams.get('page') ?? '1')
        const pageSize = Number(parsed.searchParams.get('pageSize') ?? '12')
        const start = (page - 1) * pageSize
        const items = users.slice(start, start + pageSize).map((user) => ({
          ...user,
          avatarUrl: null,
        }))
        return json({ items, page, pageSize, totalCount: users.length })
      }

      const createMatch = url === '/api/admin/users' && method === 'POST'
      if (createMatch) {
        const body = JSON.parse(init?.body as string) as {
          userName: string
          role: string
          password: string
        }
        requests.push({ method, url, body })
        if (body.userName === 'taken') {
          return json(
            { code: 'request.invalid', errors: { userName: ['Already in use.'] } },
            400,
          )
        }
        const created: MockUser = {
          id: users.length + 1,
          userName: body.userName,
          displayName: body.userName,
          roles: [body.role],
          isActive: true,
          createdAt: '2026-06-13T09:00:00Z',
        }
        users.push(created)
        return json({ ...created, avatarUrl: null }, 201)
      }

      const updateMatch = url.match(/^\/api\/admin\/users\/(\d+)$/)
      if (updateMatch && method === 'PUT') {
        const id = Number(updateMatch[1])
        const body = JSON.parse(init?.body as string) as {
          displayName: string
          role: string
        }
        requests.push({ method, url, body })
        const target = users.find((user) => user.id === id)
        if (target == null) return json({ code: 'not.found' }, 404)
        if (body.displayName === 'taken') {
          return json(
            { code: 'request.invalid', errors: { displayName: ['Invalid.'] } },
            400,
          )
        }
        target.displayName = body.displayName
        target.roles = [body.role]
        return json({ ...target, avatarUrl: null })
      }

      const actionMatch = url.match(
        /^\/api\/admin\/users\/(\d+)\/(activate|deactivate|password)$/,
      )
      if (actionMatch && method === 'POST') {
        const id = Number(actionMatch[1])
        const action = actionMatch[2]
        const body: unknown =
          init?.body != null ? JSON.parse(init.body as string) : undefined
        requests.push({ method, url, body })
        const target = users.find((user) => user.id === id)
        if (action === 'activate' && target) target.isActive = true
        if (action === 'deactivate' && target) target.isActive = false
        return new Response(null, { status: 204 })
      }

      throw new Error(`Unexpected request: ${method} ${url}`)
    })

  return { fetchMock, requests, users }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/users')
})

afterEach(() => vi.restoreAllMocks())

describe('administrative user management', () => {
  it('renders the user list with role and status badges', async () => {
    mockBackend({ userCount: 3 })
    render(<App />)

    expect(await screen.findByText('Member 01')).toBeInTheDocument()
    expect(screen.getByText('@member02')).toBeInTheDocument()
    // Member 02 is inactive in the fixture.
    const inactiveCard = screen.getByText('Member 02').closest('.seg-ucard')
    expect(inactiveCard).toHaveClass('is-inactive')
    expect(
      within(inactiveCard as HTMLElement).getByText('Inactive'),
    ).toBeInTheDocument()
  })

  it('requests a new sort order and resets to the first page', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    await user.selectOptions(screen.getByLabelText('Sort'), 'Name A–Z')

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('sort=userName') &&
            request.url.includes('sortDirection=asc'),
        ),
      ).toBe(true),
    )
  })

  it('paginates through multiple pages', async () => {
    const user = userEvent.setup()
    mockBackend({ userCount: 14 })
    render(<App />)

    await screen.findByText('Member 01')
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
    expect(screen.queryByText('Member 13')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(await screen.findByText('Member 13')).toBeInTheDocument()
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument()
  })

  it('maps a backend validation error to the create form without adding a user', async () => {
    const user = userEvent.setup()
    const { users } = mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    await user.click(screen.getByRole('button', { name: 'New user' }))

    const dialog = await screen.findByRole('dialog', { name: 'New user' })
    await user.type(within(dialog).getByLabelText('Username'), 'taken')
    await user.type(
      within(dialog).getByLabelText('Temporary password'),
      'Password1234!',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Create user' }))

    expect(
      await within(dialog).findByText(
        'Choose a different username. It may already be in use.',
      ),
    ).toBeInTheDocument()
    expect(users).toHaveLength(3)
  })

  it('creates a user and reflects it in the list', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    await user.click(screen.getByRole('button', { name: 'New user' }))

    const dialog = await screen.findByRole('dialog', { name: 'New user' })
    await user.type(within(dialog).getByLabelText('Username'), 'alex')
    await user.type(
      within(dialog).getByLabelText('Temporary password'),
      'Password1234!',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Create user' }))

    expect(await screen.findByText('User created')).toBeInTheDocument()
    expect(await screen.findByText('@alex')).toBeInTheDocument()
    expect(
      requests.some(
        (request) => request.method === 'POST' && request.url === '/api/admin/users',
      ),
    ).toBe(true)
  })

  it('edits a member display name and role and reflects it in the list', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    const card = screen.getByText('Member 03').closest('.seg-ucard') as HTMLElement
    await user.click(within(card).getByRole('button', { name: 'Edit' }))

    const dialog = await screen.findByRole('dialog', { name: 'Edit user' })
    const nameField = within(dialog).getByLabelText('Display name')
    await user.clear(nameField)
    await user.type(nameField, 'Renamed Member')
    await user.selectOptions(within(dialog).getByLabelText('Role'), 'Admin')
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('User updated')).toBeInTheDocument()
    expect(await screen.findByText('Renamed Member')).toBeInTheDocument()
    const update = requests.find(
      (request) => request.method === 'PUT' && request.url === '/api/admin/users/3',
    )
    expect(update?.body).toEqual({ displayName: 'Renamed Member', role: 'Admin' })
  })

  it('maps a backend validation error to the edit form', async () => {
    const user = userEvent.setup()
    mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    const card = screen.getByText('Member 03').closest('.seg-ucard') as HTMLElement
    await user.click(within(card).getByRole('button', { name: 'Edit' }))

    const dialog = await screen.findByRole('dialog', { name: 'Edit user' })
    const nameField = within(dialog).getByLabelText('Display name')
    await user.clear(nameField)
    await user.type(nameField, 'taken')
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    expect(
      await within(dialog).findByText('Choose a valid display name.'),
    ).toBeInTheDocument()
  })

  it('locks the role selector when an administrator edits themselves', async () => {
    const user = userEvent.setup()
    mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    // Member 01 is the signed-in administrator (session userId 1).
    const card = screen.getByText('Member 01').closest('.seg-ucard') as HTMLElement
    await user.click(within(card).getByRole('button', { name: 'Edit' }))

    const dialog = await screen.findByRole('dialog', { name: 'Edit user' })
    expect(within(dialog).getByLabelText('Role')).toBeDisabled()
    expect(
      within(dialog).getByText('You cannot change your own role. Ask another administrator.'),
    ).toBeInTheDocument()
  })

  it('resets a password for a member', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    const card = screen.getByText('Member 03').closest('.seg-ucard') as HTMLElement
    await user.click(within(card).getByRole('button', { name: 'Reset password' }))

    const dialog = await screen.findByRole('dialog', { name: 'Reset password' })
    await user.type(within(dialog).getByLabelText('New password'), 'BrandNewPass1!')
    await user.type(
      within(dialog).getByLabelText('Confirm new password'),
      'BrandNewPass1!',
    )
    await user.click(within(dialog).getByRole('button', { name: 'Reset password' }))

    expect(await screen.findByText('Password reset')).toBeInTheDocument()
    expect(
      requests.some((request) => request.url === '/api/admin/users/3/password'),
    ).toBe(true)
  })

  it('deactivates an account only after confirmation', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ userCount: 3 })
    render(<App />)

    await screen.findByText('Member 01')
    const card = screen.getByText('Member 03').closest('.seg-ucard') as HTMLElement
    await user.click(within(card).getByRole('button', { name: 'Deactivate' }))

    const dialog = await screen.findByRole('dialog', { name: 'Deactivate account?' })
    expect(requests.some((request) => request.url.includes('/deactivate'))).toBe(false)

    await user.click(within(dialog).getByRole('button', { name: 'Deactivate' }))

    await waitFor(() =>
      expect(
        requests.some((request) => request.url === '/api/admin/users/3/deactivate'),
      ).toBe(true),
    )
  })

  it('shows the access-denied state to non-admins without requesting the list', async () => {
    const { requests } = mockBackend({ roles: ['User'] })
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'This area is for administrators' }),
    ).toBeInTheDocument()
    expect(requests.some((request) => request.url.startsWith('/api/admin/users'))).toBe(
      false,
    )
  })
})
