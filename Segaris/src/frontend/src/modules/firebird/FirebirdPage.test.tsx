import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { Interaction, Person, PersonSummary, Username } from '@/app/api/firebird'

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
  usernames?: Username[]
  interactions?: Interaction[]
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
  const usernames = options.usernames ?? [
    {
      id: 10,
      platformId: 1,
      platformName: 'Email',
      handle: 'ada@example.test',
      notes: null,
    },
  ]
  const interactions = options.interactions ?? [
    { id: 20, date: '2026-06-18', description: 'Sent a birthday card' },
  ]
  const requests: Array<{ method: string; url: string; body: string | null }> = []
  let nextId = 1000

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
    const usernameMatch = url.match(/^\/api\/people\/(\d+)\/usernames(?:\/(\d+))?$/)
    if (usernameMatch) {
      const usernameId = usernameMatch[2] != null ? Number(usernameMatch[2]) : null
      if (usernameId == null && method === 'GET') return json(usernames)
      if (usernameId == null && method === 'POST') {
        requests.push({ method, url, body })
        const request = JSON.parse(body ?? '{}') as {
          platformId: number
          handle: string
          notes: string | null
        }
        const created: Username = {
          id: nextId++,
          platformId: request.platformId,
          platformName: 'Email',
          handle: request.handle,
          notes: request.notes,
        }
        usernames.push(created)
        return json(created, 201)
      }
      if (usernameId != null && method === 'PUT') {
        requests.push({ method, url, body })
        const request = JSON.parse(body ?? '{}') as {
          platformId: number
          handle: string
          notes: string | null
        }
        const updated: Username = {
          id: usernameId,
          platformId: request.platformId,
          platformName: 'Email',
          handle: request.handle,
          notes: request.notes,
        }
        return json(updated)
      }
      if (usernameId != null && method === 'DELETE') {
        requests.push({ method, url, body })
        return new Response(null, { status: 204 })
      }
    }
    const interactionMatch = url.match(
      /^\/api\/people\/(\d+)\/interactions(?:\/(\d+))?$/,
    )
    if (interactionMatch) {
      const interactionId =
        interactionMatch[2] != null ? Number(interactionMatch[2]) : null
      if (interactionId == null && method === 'GET') return json(interactions)
      if (interactionId == null && method === 'POST') {
        requests.push({ method, url, body })
        const request = JSON.parse(body ?? '{}') as Omit<Interaction, 'id'>
        const created: Interaction = { id: nextId++, ...request }
        interactions.unshift(created)
        return json(created, 201)
      }
      if (interactionId != null && method === 'PUT') {
        requests.push({ method, url, body })
        const request = JSON.parse(body ?? '{}') as Omit<Interaction, 'id'>
        return json({ id: interactionId, ...request })
      }
      if (interactionId != null && method === 'DELETE') {
        requests.push({ method, url, body })
        return new Response(null, { status: 204 })
      }
    }
    const detailMatch = url.match(/^\/api\/people\/(\d+)$/)
    if (detailMatch && method === 'GET') {
      const person = people.find((candidate) => candidate.id === Number(detailMatch[1]))
      return person == null ? json({}, 404) : json(makeDetail(person))
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

  it('creates usernames from the popup with platform selection and validation', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ people: [makePerson(1, { name: 'Ada' })] })
    render(<App />)

    await screen.findByText('Ada')
    await user.click(screen.getByRole('button', { name: 'Open usernames for Ada' }))
    const popup = await screen.findByRole('dialog', { name: 'Usernames' })
    expect(within(popup).getByText('ada@example.test')).toBeInTheDocument()

    await user.click(within(popup).getByRole('button', { name: 'Add username' }))
    const editor = await screen.findByRole('dialog', { name: 'New username' })
    await user.click(within(editor).getByRole('button', { name: 'Save changes' }))
    expect(await within(editor).findByText('Enter a value.')).toBeInTheDocument()

    await user.type(within(editor).getByLabelText('Value'), '  ada@armali.test  ')
    await user.click(within(editor).getByRole('button', { name: 'Save changes' }))

    await waitFor(() => {
      const post = requests.find(
        (request) => request.method === 'POST' && request.url.endsWith('/usernames'),
      )
      expect(post).toBeDefined()
      expect(JSON.parse(post?.body ?? '{}')).toMatchObject({
        platformId: 1,
        handle: 'ada@armali.test',
        notes: null,
      })
    })
    expect(await screen.findByText('Username saved')).toBeInTheDocument()
  })

  it('closes a gallery username popup without opening the person editor', async () => {
    const user = userEvent.setup()
    mockBackend({ people: [makePerson(1, { name: 'Ada' })] })
    render(<App />)

    await screen.findByText('Ada')
    await user.click(screen.getByRole('button', { name: 'Open usernames for Ada' }))
    const popup = await screen.findByRole('dialog', { name: 'Usernames' })
    await user.click(within(popup).getAllByRole('button', { name: 'Close' })[0])

    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Usernames' }),
      ).not.toBeInTheDocument(),
    )
    expect(
      screen.queryByRole('dialog', { name: 'Edit person' }),
    ).not.toBeInTheDocument()
  })

  it('shows interactions in server order and creates a dated interaction', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      people: [makePerson(1, { name: 'Ada' })],
      interactions: [
        { id: 22, date: '2026-06-20', description: 'Most recent note' },
        { id: 21, date: '2026-05-01', description: 'Older note' },
      ],
    })
    render(<App />)

    await screen.findByText('Ada')
    await user.click(screen.getByRole('button', { name: 'Open interactions for Ada' }))
    const popup = await screen.findByRole('dialog', { name: 'Interactions' })
    const recent = within(popup).getByText('Most recent note')
    const older = within(popup).getByText('Older note')
    expect(recent.compareDocumentPosition(older)).toBe(Node.DOCUMENT_POSITION_FOLLOWING)

    await user.click(within(popup).getByRole('button', { name: 'Add interaction' }))
    const editor = await screen.findByRole('dialog', { name: 'New interaction' })
    await user.clear(within(editor).getByLabelText('Description'))
    await user.type(within(editor).getByLabelText('Description'), '  Followed up  ')
    await user.click(within(editor).getByRole('button', { name: 'Save changes' }))

    await waitFor(() => {
      const post = requests.find(
        (request) => request.method === 'POST' && request.url.endsWith('/interactions'),
      )
      expect(post).toBeDefined()
      expect(JSON.parse(post?.body ?? '{}')).toMatchObject({
        description: 'Followed up',
      })
    })
    expect(await screen.findByText('Interaction saved')).toBeInTheDocument()
  })
})
