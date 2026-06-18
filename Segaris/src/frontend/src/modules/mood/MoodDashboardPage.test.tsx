import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { MoodDashboard } from '@/app/api/mood'

import { moodDashboard } from './testing/moodFixtures'

const TODAY = '2026-06-17'
vi.mock('./entryForm', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./entryForm')>()
  return { ...actual, householdToday: () => TODAY }
})

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

function dashboardFor(scale: string, period: string): MoodDashboard {
  return moodDashboard({
    scale: scale as MoodDashboard['scale'],
    period,
    periodStart:
      scale === 'month' && period === '2026-06' ? '2026-06-01' : '2026-01-01',
    periodEnd: scale === 'month' && period === '2026-06' ? '2026-06-30' : '2026-12-31',
    previousPeriod: period === '2026' ? '2025' : '2026-05',
    nextPeriod: period === '2026' ? '2027' : '2026-07',
    scoreByDayOfWeek: [
      { dayOfWeek: 1, min: 3, average: 3.5, max: 4 },
      { dayOfWeek: 3, min: 2, average: 2.8, max: 4 },
      { dayOfWeek: 5, min: 4, average: 4.2, max: 5 },
    ],
    scoreByInterval: [
      { interval: '2026-01', min: 3, average: 3.2, max: 4 },
      { interval: '2026-02', min: 2, average: 3.6, max: 5 },
      { interval: '2026-03', min: 4, average: 4.1, max: 5 },
    ],
    distribution: {
      energy: [
        { value: 'Low', count: 1 },
        { value: 'Medium', count: 4 },
        { value: 'High', count: 3 },
      ],
      alignment: [
        { value: 'Negative', count: 1 },
        { value: 'Medium', count: 2 },
        { value: 'Positive', count: 5 },
      ],
      direction: [
        { value: 'Harmony', count: 3 },
        { value: 'Defensive', count: 1 },
        { value: 'Offensive', count: 2 },
        { value: 'Stability', count: 2 },
      ],
      source: [
        { value: 'Internal', count: 6 },
        { value: 'External', count: 2 },
      ],
    },
    evolution: [
      {
        interval: '2026-01',
        energy: { Low: 0, Medium: 2, High: 1 },
        alignment: { Negative: 0, Medium: 1, Positive: 2 },
        direction: { Harmony: 1, Defensive: 0, Offensive: 1, Stability: 1 },
        source: { Internal: 2, External: 1 },
      },
      {
        interval: '2026-02',
        energy: { Low: 1, Medium: 1, High: 2 },
        alignment: { Negative: 1, Medium: 1, Positive: 2 },
        direction: { Harmony: 2, Defensive: 1, Offensive: 0, Stability: 1 },
        source: { Internal: 4, External: 0 },
      },
    ],
  })
}

function emptyDashboard(scale: string, period: string): MoodDashboard {
  return moodDashboard({
    scale: scale as MoodDashboard['scale'],
    period,
    periodStart: '2026-01-01',
    periodEnd: '2026-12-31',
    distribution: { energy: [], alignment: [], direction: [], source: [] },
  })
}

function mockBackend(options: { empty?: boolean } = {}) {
  const requests: string[] = []
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
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
    if (url.startsWith('/api/mood/dashboard')) {
      requests.push(url)
      const parsed = new URL(url, 'http://localhost')
      const scale = parsed.searchParams.get('scale') ?? 'year'
      const period = parsed.searchParams.get('period') ?? '2026'
      return json(
        options.empty ? emptyDashboard(scale, period) : dashboardFor(scale, period),
      )
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/mood/dashboard')
})

afterEach(() => vi.restoreAllMocks())

describe('Mood dashboard view', () => {
  it('defaults to the current year and renders dashboard charts', async () => {
    const { requests } = mockBackend()
    render(<App />)

    expect(await screen.findByText('Interval average')).toBeInTheDocument()
    expect(
      screen.getAllByRole('img', {
        name: 'Mood score minimum, average, and maximum by day of week',
      }).length,
    ).toBeGreaterThan(0)
    expect(screen.getByRole('img', { name: 'Energy distribution' })).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: 'Energy criteria evolution by period interval' }),
    ).toBeInTheDocument()
    expect(
      requests.some((url) => url.includes('scale=year') && url.includes('period=2026')),
    ).toBe(true)
  })

  it('changes scale by resetting to the current period and updating the URL query', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Interval average')
    await user.click(screen.getByRole('button', { name: 'Month' }))

    await waitFor(() => expect(window.location.search).toContain('scale=month'))
    expect(window.location.search).toContain('period=2026-06')
    expect(
      requests.some(
        (url) => url.includes('scale=month') && url.includes('period=2026-06'),
      ),
    ).toBe(true)
  })

  it('navigates to adjacent strict periods', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Interval average')
    await user.click(screen.getByRole('button', { name: 'Previous period' }))

    await waitFor(() => expect(window.location.search).toContain('period=2025'))
  })

  it('renders a no-data state for an empty period', async () => {
    mockBackend({ empty: true })
    render(<App />)

    expect(await screen.findByText(/No entries in this period/)).toBeInTheDocument()
  })
})
