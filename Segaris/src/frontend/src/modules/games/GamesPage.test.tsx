import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import axe from 'axe-core'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  Game,
  Goal,
  Playthrough,
  PlaythroughSummary,
  Section,
} from '@/app/api/games'

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

function makeSection(id: number, overrides: Partial<Section> = {}): Section {
  return {
    id,
    name: id === 1 ? 'Main bosses' : 'Exploration',
    color: id === 1 ? 'Red' : 'Teal',
    sortOrder: id,
    progress: { completedGoals: 0, totalGoals: 0 },
    ...overrides,
  }
}

function makeGoal(id: number, overrides: Partial<Goal> = {}): Goal {
  return {
    id,
    text: id === 1 ? 'Margit, the Fell Omen' : 'Godrick the Grafted',
    completed: id === 1,
    position: id,
    ...overrides,
  }
}

interface BackendOptions {
  summaries?: PlaythroughSummary[]
  sections?: Record<number, Section[]>
  goals?: Record<number, Goal[]>
}

function mockBackend(options: BackendOptions = {}) {
  const summaries =
    options.summaries ?? Array.from({ length: 3 }, (_, i) => makeSummary(i + 1))
  const sectionsByPlaythrough: Record<number, Section[]> = {
    1: [makeSection(1), makeSection(2)],
    ...(options.sections ?? {}),
  }
  const goalsBySection: Record<number, Goal[]> = {
    1: [makeGoal(1), makeGoal(2)],
    2: [],
    ...(options.goals ?? {}),
  }
  const requests: Array<{ method: string; url: string; body?: unknown }> = []
  let created: Playthrough | null = null

  const sectionWithProgress = (section: Section): Section => {
    const goals = goalsBySection[section.id] ?? []
    return {
      ...section,
      progress: {
        completedGoals: goals.filter((goal) => goal.completed).length,
        totalGoals: goals.length,
      },
    }
  }

  const playthroughProgress = (playthroughId: number) => {
    const sectionIds = (sectionsByPlaythrough[playthroughId] ?? []).map((s) => s.id)
    const allGoals = sectionIds.flatMap((sectionId) => goalsBySection[sectionId] ?? [])
    return {
      completedGoals: allGoals.filter((goal) => goal.completed).length,
      totalGoals: allGoals.length,
    }
  }

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
          : json(makeDetail(id, { ...summary, progress: playthroughProgress(id) }))
      }
      if (detailMatch && method === 'PUT') {
        requests.push({ method, url, body })
        return json(makeDetail(Number(detailMatch[1]), body as Partial<Playthrough>))
      }
      if (detailMatch && method === 'DELETE') {
        requests.push({ method, url })
        return new Response(null, { status: 204 })
      }

      const sectionsMatch = url.match(/^\/api\/games\/playthroughs\/(\d+)\/sections$/)
      if (sectionsMatch && method === 'GET') {
        const playthroughId = Number(sectionsMatch[1])
        return json(
          (sectionsByPlaythrough[playthroughId] ?? []).map(sectionWithProgress),
        )
      }
      if (sectionsMatch && method === 'POST') {
        requests.push({ method, url, body })
        const playthroughId = Number(sectionsMatch[1])
        const nextId =
          Math.max(
            0,
            ...Object.values(sectionsByPlaythrough)
              .flat()
              .map((s) => s.id),
          ) + 1
        const section = makeSection(nextId, {
          ...(body as Partial<Section>),
          sortOrder: (sectionsByPlaythrough[playthroughId] ?? []).length + 1,
        })
        sectionsByPlaythrough[playthroughId] = [
          ...(sectionsByPlaythrough[playthroughId] ?? []),
          section,
        ]
        goalsBySection[section.id] = []
        return json(sectionWithProgress(section), 201)
      }

      const orderMatch = url.match(
        /^\/api\/games\/playthroughs\/(\d+)\/sections\/order$/,
      )
      if (orderMatch && method === 'PUT') {
        requests.push({ method, url, body })
        const playthroughId = Number(orderMatch[1])
        const ids = (body as { sectionIds: number[] }).sectionIds
        sectionsByPlaythrough[playthroughId] = ids.map((id, index) => ({
          ...(sectionsByPlaythrough[playthroughId] ?? []).find((s) => s.id === id)!,
          sortOrder: index + 1,
        }))
        return new Response(null, { status: 204 })
      }

      const sectionMatch = url.match(
        /^\/api\/games\/playthroughs\/(\d+)\/sections\/(\d+)$/,
      )
      if (sectionMatch && method === 'PUT') {
        requests.push({ method, url, body })
        const playthroughId = Number(sectionMatch[1])
        const sectionId = Number(sectionMatch[2])
        const next = (sectionsByPlaythrough[playthroughId] ?? []).map((section) =>
          section.id === sectionId
            ? { ...section, ...(body as Partial<Section>) }
            : section,
        )
        sectionsByPlaythrough[playthroughId] = next
        return json(sectionWithProgress(next.find((s) => s.id === sectionId)!))
      }
      if (sectionMatch && method === 'DELETE') {
        requests.push({ method, url })
        const playthroughId = Number(sectionMatch[1])
        const sectionId = Number(sectionMatch[2])
        sectionsByPlaythrough[playthroughId] = (
          sectionsByPlaythrough[playthroughId] ?? []
        ).filter((section) => section.id !== sectionId)
        delete goalsBySection[sectionId]
        return new Response(null, { status: 204 })
      }

      const goalsMatch = url.match(
        /^\/api\/games\/playthroughs\/(\d+)\/sections\/(\d+)\/goals$/,
      )
      if (goalsMatch && method === 'GET') {
        return json(goalsBySection[Number(goalsMatch[2])] ?? [])
      }
      if (goalsMatch && method === 'POST') {
        requests.push({ method, url, body })
        const sectionId = Number(goalsMatch[2])
        const nextId =
          Math.max(
            0,
            ...Object.values(goalsBySection)
              .flat()
              .map((g) => g.id),
          ) + 1
        const goal = makeGoal(nextId, {
          text: (body as { text: string }).text,
          completed: false,
          position: (goalsBySection[sectionId] ?? []).length + 1,
        })
        goalsBySection[sectionId] = [...(goalsBySection[sectionId] ?? []), goal]
        return json(goal, 201)
      }

      const goalCompletionMatch = url.match(
        /^\/api\/games\/playthroughs\/(\d+)\/sections\/(\d+)\/goals\/(\d+)\/completion$/,
      )
      if (goalCompletionMatch && method === 'PUT') {
        requests.push({ method, url, body })
        const sectionId = Number(goalCompletionMatch[2])
        const goalId = Number(goalCompletionMatch[3])
        const next = (goalsBySection[sectionId] ?? []).map((goal) =>
          goal.id === goalId
            ? { ...goal, completed: (body as { completed: boolean }).completed }
            : goal,
        )
        goalsBySection[sectionId] = next
        return json(next.find((goal) => goal.id === goalId))
      }

      const goalMatch = url.match(
        /^\/api\/games\/playthroughs\/(\d+)\/sections\/(\d+)\/goals\/(\d+)$/,
      )
      if (goalMatch && method === 'PUT') {
        requests.push({ method, url, body })
        const sectionId = Number(goalMatch[2])
        const goalId = Number(goalMatch[3])
        const next = (goalsBySection[sectionId] ?? []).map((goal) =>
          goal.id === goalId
            ? { ...goal, text: (body as { text: string }).text }
            : goal,
        )
        goalsBySection[sectionId] = next
        return json(next.find((goal) => goal.id === goalId))
      }
      if (goalMatch && method === 'DELETE') {
        requests.push({ method, url })
        const sectionId = Number(goalMatch[2])
        const goalId = Number(goalMatch[3])
        goalsBySection[sectionId] = (goalsBySection[sectionId] ?? []).filter(
          (goal) => goal.id !== goalId,
        )
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

describe('Games progress page', () => {
  it('preserves selected-section route state and renders that section goals', async () => {
    mockBackend({
      summaries: [makeSummary(1, { name: 'First run' })],
      goals: { 2: [] },
    })
    window.history.replaceState({}, '', '/games/playthroughs/1?sectionId=2')
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Exploration' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('No goals in this section yet.')).toBeInTheDocument()
    expect(window.location.search).toContain('sectionId=2')
  })

  it('handles an empty playthrough without defaulting a section', async () => {
    mockBackend({
      summaries: [
        makeSummary(1, {
          name: 'Empty run',
          progress: { completedGoals: 0, totalGoals: 0 },
        }),
      ],
      sections: { 1: [] },
      goals: {},
    })
    window.history.replaceState({}, '', '/games/playthroughs/1?sectionId=99')
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'No sections yet' }),
    ).toBeInTheDocument()
    await waitFor(() => expect(window.location.search).not.toContain('sectionId=99'))
  })

  it('adds a goal to the selected section and keeps creation order', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      summaries: [makeSummary(1, { name: 'First run' })],
      goals: { 2: [] },
    })
    window.history.replaceState({}, '', '/games/playthroughs/1?sectionId=2')
    render(<App />)

    const input = await screen.findByRole('textbox', { name: 'Add goal' })
    await user.type(input, 'Reach Altus Plateau')
    await user.keyboard('{Enter}')

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url.endsWith('/sections/2/goals') &&
            (request.body as { text?: string }).text === 'Reach Altus Plateau',
        ),
      ).toBe(true),
    )
    expect(await screen.findByText('Reach Altus Plateau')).toBeInTheDocument()
  })

  it('toggles goal completion and refreshes derived progress', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      summaries: [makeSummary(1, { name: 'First run' })],
    })
    window.history.replaceState({}, '', '/games/playthroughs/1?sectionId=1')
    render(<App />)

    await user.click(await screen.findByLabelText('Toggle Godrick the Grafted'))

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'PUT' &&
            request.url.endsWith('/goals/2/completion') &&
            (request.body as { completed?: boolean }).completed === true,
        ),
      ).toBe(true),
    )
    expect(await screen.findAllByText('2 of 2 goals')).not.toHaveLength(0)
  })

  it('manages section ordering through the section popup', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      summaries: [makeSummary(1, { name: 'First run' })],
    })
    window.history.replaceState({}, '', '/games/playthroughs/1?sectionId=1')
    render(<App />)

    await screen.findByRole('heading', { name: 'Main bosses' })
    await user.click(screen.getByRole('button', { name: 'Manage sections' }))
    const dialog = await screen.findByRole('dialog', { name: 'Manage sections' })

    await user.click(
      within(dialog).getAllByRole('button', { name: 'Move section down' })[0],
    )

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'PUT' &&
            request.url.endsWith('/sections/order') &&
            JSON.stringify(request.body) === JSON.stringify({ sectionIds: [2, 1] }),
        ),
      ).toBe(true),
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

  it('has no automated violations on the progress page', async () => {
    mockBackend({ summaries: [makeSummary(1, { name: 'First run' })] })
    window.history.replaceState({}, '', '/games/playthroughs/1?sectionId=1')
    render(<App />)

    await screen.findByRole('heading', { name: 'Main bosses' })
    await expectNoAccessibilityViolations()
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
