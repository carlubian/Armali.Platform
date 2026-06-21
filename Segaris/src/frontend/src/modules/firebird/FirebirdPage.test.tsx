import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { Person, PersonSummary } from '@/app/api/firebird'

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['User'],
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

function makePerson(id: number, overrides: Partial<PersonSummary> = {}): PersonSummary {
  return {
    id,
    name: `Person ${id.toString().padStart(2, '0')}`,
    categoryId: 1,
    categoryName: 'Family',
    status: 'Active',
    birthdayMonth: 6,
    birthdayDay: 24,
    visibility: 'Public',
    avatar: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

interface BackendOptions {
  people?: PersonSummary[]
}

function makeDetail(summary: PersonSummary): Person {
  return {
    ...summary,
    notes: null,
    usernames: [],
    interactions: [],
    createdById: summary.creatorId,
    createdByName: summary.creatorName,
    createdAt: '2026-06-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
  }
}

function mockBackend(options: BackendOptions = {}) {
  const people = options.people ?? [makePerson(1)]
  const requests: Array<{ method: string; url: string; body: string | null }> = []

  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    await Promise.resolve()
    const url = urlOf(input)
    const method = init?.method ?? 'GET'
    const body = typeof init?.body === 'string' ? init.body : null

    if (url === '/api/session/antiforgery') return json({ csrfToken: 'token' })
    if (url === '/api/session' && method === 'GET') return json(session)
    if (url === '/api/session/profile' && method === 'GET') {
      return json({
        displayName: session.displayName,
        language: session.language,
        avatarUrl: session.avatarUrl,
      })
    }
    if (url.startsWith('/api/launcher/attention')) return json({ modules: [] })
    if (url.startsWith('/api/people/categories')) {
      return json([{ id: 1, name: 'Family', sortOrder: 1 }])
    }
    if (url.startsWith('/api/people/platforms')) {
      return json([{ id: 1, name: 'Email', sortOrder: 1 }])
    }
    if (url === '/api/people' && method === 'POST') {
      requests.push({ method, url, body })
      const request = JSON.parse(body ?? '{}') as { name: string }
      return json(makeDetail(makePerson(99, { name: request.name })), 201)
    }
    if (url.startsWith('/api/people') && method === 'GET') {
      requests.push({ method, url, body })
      const parsed = new URL(url, 'http://localhost')
      const search = parsed.searchParams.get('search')?.toLowerCase() ?? ''
      const filtered =
        search.length === 0
          ? people
          : people.filter((person) => person.name.toLowerCase().includes(search))
      return json({
        items: filtered,
        page: 1,
        pageSize: 25,
        totalCount: filtered.length,
      })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/people')
})

afterEach(() => vi.restoreAllMocks())

describe('Firebird gallery', () => {
  it('renders people with placeholder avatars and birthdays', async () => {
    mockBackend({
      people: [
        makePerson(1),
        makePerson(2, {
          name: 'Greta Lindqvist',
          birthdayMonth: null,
          birthdayDay: null,
        }),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Person 01')).toBeInTheDocument()
    expect(screen.getByText('Greta Lindqvist')).toBeInTheDocument()
    expect(screen.getByText('24 Jun')).toBeInTheDocument()
    expect(screen.getByText('No birthday')).toBeInTheDocument()
  })

  it('exposes username and interaction popup buttons on each card', async () => {
    mockBackend({ people: [makePerson(1, { name: 'Lucía Romero' })] })
    render(<App />)

    await screen.findByText('Lucía Romero')
    expect(
      screen.getByRole('button', { name: 'Open usernames for Lucía Romero' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Open interactions for Lucía Romero' }),
    ).toBeInTheDocument()
  })

  it('serializes the search term into the people request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Person 01')
    await user.type(screen.getByLabelText('Search'), 'Person')

    await waitFor(() =>
      expect(requests.some((request) => request.url.includes('search=Person'))).toBe(
        true,
      ),
    )
  })

  it('opens the create dialog from the new person action', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Person 01')
    await user.click(screen.getByRole('button', { name: 'New person' }))

    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByLabelText('Name')).toBeInTheDocument()
    expect(within(dialog).getByText('Create person')).toBeInTheDocument()
  })

  it('reveals the month and day controls when a birthday is enabled', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Person 01')
    await user.click(screen.getByRole('button', { name: 'New person' }))
    const dialog = await screen.findByRole('dialog')

    expect(within(dialog).queryByLabelText('Month')).not.toBeInTheDocument()
    await user.click(within(dialog).getByLabelText('Has a birthday'))
    expect(within(dialog).getByLabelText('Month')).toBeInTheDocument()
    expect(within(dialog).getByLabelText('Day')).toBeInTheDocument()
  })

  it('blocks creation and shows a field error when the name is empty', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Person 01')
    await user.click(screen.getByRole('button', { name: 'New person' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByText('Create person'))

    expect(await within(dialog).findByText('Enter a name.')).toBeInTheDocument()
    expect(requests.some((request) => request.method === 'POST')).toBe(false)
  })

  it('creates a person and posts the trimmed request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Person 01')
    await user.click(screen.getByRole('button', { name: 'New person' }))
    const dialog = await screen.findByRole('dialog')
    await user.type(within(dialog).getByLabelText('Name'), '  Ada Lovelace  ')
    await user.click(within(dialog).getByText('Create person'))

    await waitFor(() => {
      const post = requests.find((request) => request.method === 'POST')
      expect(post).toBeDefined()
      expect(JSON.parse(post?.body ?? '{}')).toMatchObject({
        name: 'Ada Lovelace',
        status: 'Unknown',
        visibility: 'Public',
        birthdayMonth: null,
        birthdayDay: null,
      })
    })
    expect(await screen.findByText('Person created')).toBeInTheDocument()
  })
})
