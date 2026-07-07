import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import axe from 'axe-core'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { Game, Playthrough, PlaythroughSummary } from '@/app/api/games'

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['User'],
  avatarUrl: null as string | null,
}

const games: Game[] = [
  { id: 1, name: 'Elden Ring', platform: 'Console', sortOrder: 1 },
  { id: 2, name: "Baldur's Gate 3", platform: 'PC', sortOrder: 2 },
]

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

function makeSummary(
  id: number,
  overrides: Partial<PlaythroughSummary> = {},
): PlaythroughSummary {
  return {
    id,
    name: `Run ${id.toString().padStart(2, '0')}`,
    gameId: 1,
    gameName: 'Elden Ring',
    platform: 'Console',
    status: 'Active',
    startYear: 2026,
    startMonth: 3,
    tags: ['Blind'],
    progress: { completedGoals: 2, totalGoals: 5 },
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeDetail(id: number, overrides: Partial<Playthrough> = {}): Playthrough {
  return {
    ...makeSummary(id),
    createdAt: '2026-03-12T10:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
    ...overrides,
  }
}

interface BackendOptions {
  summaries?: PlaythroughSummary[]
}

function mockBackend(options: BackendOptions = {}) {
  const summaries =
    options.summaries ?? Array.from({ length: 3 }, (_, i) => makeSummary(i + 1))
  const requests: Array<{ method: string; url: string; body?: unknown }> = []
  let created: Playthrough | null = null

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
      await Promise.resolve()
      const url = urlOf(input)
      const method = init?.method ?? 'GET'
      const body: unknown =
        typeof init?.body === 'string' ? (JSON.parse(init.body) as unknown) : undefined

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
      if (url === '/api/games/games') return json(games)

      // Detail read for a single playthrough.
      const detailMatch = url.match(/^\/api\/games\/playthroughs\/(\d+)$/)
      if (detailMatch && method === 'GET') {
        const id = Number(detailMatch[1])
        const summary = summaries.find((s) => s.id === id)
        return summary == null
          ? json({ code: 'games.playthrough.not_found' }, 404)
          : json(makeDetail(id, summary))
      }
      if (detailMatch && method === 'PUT') {
        requests.push({ method, url, body })
        return json(makeDetail(Number(detailMatch[1]), body as Partial<Playthrough>))
      }
      if (detailMatch && method === 'DELETE') {
        requests.push({ method, url })
        return new Response(null, { status: 204 })
      }

      if (url.startsWith('/api/games/playthroughs') && method === 'POST') {
        requests.push({ method, url, body })
        created = makeDetail(99, {
          name: (body as { name: string }).name,
          gameId: (body as { gameId: number }).gameId,
          tags: (body as { tags: string[] }).tags,
        })
        return json(created, 201)
      }

      if (url.startsWith('/api/games/playthroughs') && method === 'GET') {
        requests.push({ method, url })
        const params = new URL(url, 'http://localhost').searchParams
        const page = Number(params.get('page') ?? '1')
        const pageSize = Number(params.get('pageSize') ?? '25')
        const search = params.get('search')?.toLowerCase() ?? ''
        const status = params.get('status')
        const platform = params.get('platform')

        let filtered = summaries
        if (search) {
          filtered = filtered.filter(
            (s) =>
              s.name.toLowerCase().includes(search) ||
              s.gameName.toLowerCase().includes(search),
          )
        }
        if (status) filtered = filtered.filter((s) => s.status === status)
        if (platform) filtered = filtered.filter((s) => s.platform === platform)

        const start = (page - 1) * pageSize
        const items = filtered.slice(start, start + pageSize)
        return json({ items, page, pageSize, totalCount: filtered.length })
      }

      throw new Error(`Unexpected request: ${method} ${url}`)
    })

  return { fetchMock, requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/games')
})

afterEach(() => vi.restoreAllMocks())

describe('Games collection', () => {
  it('renders playthrough cards with game, status, and derived progress', async () => {
    mockBackend({ summaries: [makeSummary(1, { name: 'First run' })] })
    render(<App />)

    const card = await screen.findByRole('button', { name: 'Open First run' })
    expect(within(card).getByText('Elden Ring')).toBeInTheDocument()
    expect(within(card).getByText('Active')).toBeInTheDocument()
    expect(within(card).getByText('2 of 5 goals')).toBeInTheDocument()
    expect(within(card).getByText('40%')).toBeInTheDocument()
  })

  it('serializes the search term into the playthroughs request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByRole('button', { name: 'Open Run 01' })
    await user.type(screen.getByLabelText('Search'), 'Run 02')

    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('search=Run+02'))).toBe(true),
    )
  })

  it('requests the selected sort field and toggles direction', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByRole('button', { name: 'Open Run 01' })
    await user.selectOptions(screen.getByLabelText('Sort'), 'game')
    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('sort=game'))).toBe(true),
    )

    await user.click(screen.getByRole('button', { name: 'Toggle sort direction' }))
    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('sortDirection=desc'))).toBe(true),
    )
  })

  it('shows an active-filter chip and removes it on click', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByRole('button', { name: 'Open Run 01' })
    await user.selectOptions(screen.getByLabelText('Status'), 'Completed')

    const chip = await screen.findByRole('button', {
      name: 'Remove Status: Completed filter',
    })
    expect(chip).toBeInTheDocument()

    await user.click(chip)
    expect(
      screen.queryByRole('button', { name: 'Remove Status: Completed filter' }),
    ).not.toBeInTheDocument()
  })

  it('paginates through the result set', async () => {
    const user = userEvent.setup()
    mockBackend({ summaries: Array.from({ length: 30 }, (_, i) => makeSummary(i + 1)) })
    render(<App />)

    await screen.findByRole('button', { name: 'Open Run 01' })
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Open Run 26' }),
    ).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(
      await screen.findByRole('button', { name: 'Open Run 26' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument()
  })

  it('shows an empty state when no playthroughs match', async () => {
    mockBackend({ summaries: [] })
    render(<App />)

    expect(
      await screen.findByText(
        'No playthroughs yet. Create the first to start tracking progress.',
      ),
    ).toBeInTheDocument()
  })

  it('navigates to the progress page when a card is activated', async () => {
    const user = userEvent.setup()
    mockBackend({ summaries: [makeSummary(1, { name: 'First run' })] })
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Open First run' }))

    expect(
      await screen.findByRole('button', { name: 'All playthroughs' }),
    ).toBeInTheDocument()
    expect(window.location.pathname).toBe('/games/playthroughs/1')
  })
})

describe('Games playthrough editor', () => {
  it('requires a game before creating a playthrough', async () => {
    const user = userEvent.setup()
    mockBackend({ summaries: [] })
    render(<App />)

    await screen.findByText(
      'No playthroughs yet. Create the first to start tracking progress.',
    )
    await user.click(screen.getByRole('button', { name: 'New playthrough' }))

    await user.type(await screen.findByLabelText('Name'), 'Blind run')
    await user.click(screen.getByRole('button', { name: 'Create playthrough' }))

    expect(await screen.findByText('A game is required.')).toBeInTheDocument()
  })

  it('creates a playthrough with a selected game and normalized tags', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({ summaries: [] })
    render(<App />)

    await screen.findByText(
      'No playthroughs yet. Create the first to start tracking progress.',
    )
    await user.click(screen.getByRole('button', { name: 'New playthrough' }))

    await user.type(await screen.findByLabelText('Name'), 'Blind run')

    // Open the floating game selector and pick a game.
    await user.click(
      screen.getByRole('button', { name: /Choose a game from the catalogue/ }),
    )
    const selectRow = await screen.findByRole('dialog', { name: /Choose a game/ })
    const eldenRow = within(selectRow).getByText('Elden Ring').closest('[role="row"]')
    await user.click(
      within(eldenRow as HTMLElement).getByRole('button', { name: 'Select' }),
    )

    // Duplicate tags are de-duplicated case-insensitively before submission.
    const tagInput = screen.getByPlaceholderText('Add a tag, press Enter')
    await user.type(tagInput, 'Melee{Enter}melee{Enter}')

    await user.click(screen.getByRole('button', { name: 'Create playthrough' }))

    await waitFor(() => {
      const post = requests.find((r) => r.method === 'POST')
      expect(post).toBeDefined()
      expect(post?.body).toMatchObject({
        name: 'Blind run',
        gameId: 1,
        tags: ['Melee'],
      })
    })
  })

  it('confirms before deleting a playthrough from the editor', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      summaries: [makeSummary(1, { name: 'First run' })],
    })
    // The edit dialog opens straight from URL state, restoring on reload.
    window.history.replaceState({}, '', '/games?playthroughId=1')
    render(<App />)

    const nameField = await screen.findByLabelText('Name')
    expect(nameField).toHaveValue('First run')

    await user.click(screen.getByRole('button', { name: 'Delete' }))
    await user.click(await screen.findByRole('button', { name: 'Delete playthrough' }))

    await waitFor(() =>
      expect(requests.some((r) => r.method === 'DELETE' && r.url.endsWith('/1'))).toBe(
        true,
      ),
    )
  })
})

async function expectNoAccessibilityViolations() {
  const result = await axe.run(document.body, {
    rules: { 'color-contrast': { enabled: false } },
  })
  expect(result.violations).toEqual([])
}

describe('Games accessibility', () => {
  it('has no automated violations on the collection', async () => {
    mockBackend({ summaries: [makeSummary(1, { name: 'First run' })] })
    render(<App />)

    await screen.findByRole('button', { name: 'Open First run' })
    await expectNoAccessibilityViolations()
  })

  it('opens the editor dialog with focus and no violations', async () => {
    const user = userEvent.setup()
    mockBackend({ summaries: [] })
    render(<App />)

    await screen.findByText(
      'No playthroughs yet. Create the first to start tracking progress.',
    )
    await user.click(screen.getByRole('button', { name: 'New playthrough' }))

    const dialog = await screen.findByRole('dialog', { name: 'New playthrough' })
    expect(dialog).toHaveFocus()
    await expectNoAccessibilityViolations()
  })

  it('activates a card with the keyboard', async () => {
    const user = userEvent.setup()
    mockBackend({ summaries: [makeSummary(1, { name: 'First run' })] })
    render(<App />)

    const card = await screen.findByRole('button', { name: 'Open First run' })
    card.focus()
    await user.keyboard('{Enter}')

    expect(
      await screen.findByRole('button', { name: 'All playthroughs' }),
    ).toBeInTheDocument()
  })
})

describe('Games navigation', () => {
  it('loads the Games module lazily from the launcher', async () => {
    const user = userEvent.setup()
    mockBackend()
    window.history.replaceState({}, '', '/')
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Games/ }))

    expect(await screen.findByRole('heading', { name: 'Games' })).toBeInTheDocument()
  })
})
