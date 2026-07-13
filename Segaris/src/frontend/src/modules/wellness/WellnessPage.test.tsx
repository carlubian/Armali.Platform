import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import axe from 'axe-core'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { WellnessDayTask, WellnessToday } from '@/app/api/wellness'

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

function makeTask(overrides: Partial<WellnessDayTask> = {}): WellnessDayTask {
  return {
    id: 1,
    name: 'Drink water',
    category: 'HealthAndBody',
    completed: false,
    position: 1,
    ...overrides,
  }
}

function scoreOf(tasks: WellnessDayTask[]): number | null {
  if (tasks.length === 0) return null
  return Math.round(
    (tasks.filter((task) => task.completed).length / tasks.length) * 100,
  )
}

interface BackendOptions {
  tasks?: WellnessDayTask[]
  toggleStatus?: number
}

function mockBackend(options: BackendOptions = {}) {
  const tasks = options.tasks ?? [
    makeTask({ id: 1, name: 'Drink water', category: 'HealthAndBody' }),
    makeTask({ id: 2, name: 'Sleep 8 hours', category: 'MindAndSleep' }),
    makeTask({ id: 3, name: 'Call a friend', category: 'PeopleAndWork' }),
  ]
  const requests: Array<{ method: string; url: string }> = []

  const today = (): WellnessToday => ({
    date: '2026-07-13',
    score: scoreOf(tasks),
    tasks: tasks.map((task) => ({ ...task })),
  })

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
      await Promise.resolve()
      const url = urlOf(input)
      const method = init?.method ?? 'GET'

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

      if (url === '/api/wellness/today' && method === 'GET') {
        requests.push({ method, url })
        return json(today())
      }

      const toggleMatch = url.match(/^\/api\/wellness\/today\/tasks\/(\d+)\/toggle$/)
      if (toggleMatch && method === 'POST') {
        requests.push({ method, url })
        if (options.toggleStatus != null && options.toggleStatus >= 400) {
          return json({ code: 'wellness.day.error' }, options.toggleStatus)
        }
        const id = Number(toggleMatch[1])
        const target = tasks.find((task) => task.id === id)
        if (target) target.completed = !target.completed
        return json(today())
      }

      throw new Error(`Unexpected request: ${method} ${url}`)
    })

  return { fetchMock, requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/wellness')
})

afterEach(() => vi.restoreAllMocks())

describe('Wellness today surface', () => {
  it('renders the day tasks, their categories, and the daily score', async () => {
    mockBackend()
    render(<App />)

    expect(await screen.findByText('Drink water')).toBeInTheDocument()
    expect(screen.getByText('Sleep 8 hours')).toBeInTheDocument()
    expect(screen.getByText('Call a friend')).toBeInTheDocument()

    expect(screen.getByText('Health & Body')).toBeInTheDocument()
    expect(screen.getByText('Mind & Sleep')).toBeInTheDocument()
    expect(screen.getByText('People & Work')).toBeInTheDocument()

    // Nothing completed yet: the ring reads 0 and the caption is 0 of 3.
    expect(screen.getByRole('img', { name: /0 percent/ })).toBeInTheDocument()
    expect(screen.getByText('0 of 3 completed')).toBeInTheDocument()
  })

  it('toggles a task and reflects the recomputed score', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await user.click(
      await screen.findByRole('checkbox', { name: 'Toggle Drink water' }),
    )

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url.endsWith('/wellness/today/tasks/1/toggle'),
        ),
      ).toBe(true),
    )

    // One of three completed rounds to 33.
    expect(await screen.findByText('1 of 3 completed')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: /33 percent/ })).toBeInTheDocument()
  })

  it('shows the empty state when the catalogue is empty', async () => {
    mockBackend({ tasks: [] })
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'No tasks for today' }),
    ).toBeInTheDocument()
    expect(screen.queryByRole('img', { name: /percent/ })).not.toBeInTheDocument()
  })

  it('surfaces a privacy-safe toast when a toggle fails', async () => {
    const user = userEvent.setup()
    mockBackend({ toggleStatus: 500 })
    render(<App />)

    await user.click(
      await screen.findByRole('checkbox', { name: 'Toggle Drink water' }),
    )

    const alert = await screen.findByText('That change could not be saved')
    expect(alert).toBeInTheDocument()
    // The failure message never discloses server internals.
    expect(
      screen.getByText('Your task could not be updated. Please try again.'),
    ).toBeInTheDocument()
  })
})

describe('Wellness accessibility', () => {
  it('has no automated violations on the today surface', async () => {
    mockBackend()
    render(<App />)

    await screen.findByText('Drink water')
    const result = await axe.run(document.body, {
      rules: { 'color-contrast': { enabled: false } },
    })
    expect(result.violations).toEqual([])
  })

  it('completes a task with the keyboard', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    const checkbox = await screen.findByRole('checkbox', { name: 'Toggle Drink water' })
    checkbox.focus()
    await user.keyboard(' ')

    await waitFor(() =>
      expect(
        requests.some((request) =>
          request.url.endsWith('/wellness/today/tasks/1/toggle'),
        ),
      ).toBe(true),
    )
  })
})

describe('Wellness navigation', () => {
  it('loads the module lazily from the launcher', async () => {
    const user = userEvent.setup()
    mockBackend()
    window.history.replaceState({}, '', '/')
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Wellness/ }))

    expect(await screen.findByText("Today's tasks")).toBeInTheDocument()
  })
})
