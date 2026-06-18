import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { MoodDashboardScale } from '@/app/api/mood'

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

interface MoodDashboardResponseFixture {
  scale: string
  period: string
  from: string
  to: string
  previousPeriod: string
  nextPeriod: string
  bucketGranularity: string
  entryCount: number
  scoreByDayOfWeek: Array<{
    dayOfWeek: string
    minScore: number | null
    averageScore: number | null
    maxScore: number | null
  }>
  distribution: CriteriaDistributionFixture
  buckets: Array<{
    key: string
    start: string
    end: string
    minScore: number | null
    averageScore: number | null
    maxScore: number | null
    distribution: CriteriaDistributionFixture
  }>
}

interface CriteriaDistributionFixture {
  energy: Array<{ value: string; count: number }>
  alignment: Array<{ value: string; count: number }>
  direction: Array<{ value: string; count: number }>
  source: Array<{ value: string; count: number }>
}

function responseScale(scale: string): string {
  const normalized: Record<MoodDashboardScale, string> = {
    year: 'Year',
    semester: 'Semester',
    quarter: 'Quarter',
    month: 'Month',
  }
  return normalized[scale as MoodDashboardScale] ?? 'Year'
}

function dashboardFor(scale: string, period: string): MoodDashboardResponseFixture {
  const from = scale === 'month' && period === '2026-06' ? '2026-06-01' : '2026-01-01'
  const to = scale === 'month' && period === '2026-06' ? '2026-06-30' : '2026-12-31'
  return {
    scale: responseScale(scale),
    period,
    from,
    to,
    previousPeriod: period === '2026' ? '2025' : '2026-05',
    nextPeriod: period === '2026' ? '2027' : '2026-07',
    bucketGranularity: scale === 'month' ? 'Week' : 'Month',
    entryCount: 8,
    scoreByDayOfWeek: [
      { dayOfWeek: 'Monday', minScore: 3, averageScore: 3.5, maxScore: 4 },
      { dayOfWeek: 'Wednesday', minScore: 2, averageScore: 2.8, maxScore: 4 },
      { dayOfWeek: 'Friday', minScore: 4, averageScore: 4.2, maxScore: 5 },
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
    buckets: [
      {
        key: '2026-01',
        start: '2026-01-01',
        end: '2026-01-31',
        minScore: 3,
        averageScore: 3.2,
        maxScore: 4,
        distribution: {
          energy: [
            { value: 'Low', count: 0 },
            { value: 'Medium', count: 2 },
            { value: 'High', count: 1 },
          ],
          alignment: [
            { value: 'Negative', count: 0 },
            { value: 'Medium', count: 1 },
            { value: 'Positive', count: 2 },
          ],
          direction: [
            { value: 'Harmony', count: 1 },
            { value: 'Defensive', count: 0 },
            { value: 'Offensive', count: 1 },
            { value: 'Stability', count: 1 },
          ],
          source: [
            { value: 'Internal', count: 2 },
            { value: 'External', count: 1 },
          ],
        },
      },
      {
        key: '2026-02',
        start: '2026-02-01',
        end: '2026-02-28',
        minScore: 2,
        averageScore: 3.6,
        maxScore: 5,
        distribution: {
          energy: [
            { value: 'Low', count: 1 },
            { value: 'Medium', count: 1 },
            { value: 'High', count: 2 },
          ],
          alignment: [
            { value: 'Negative', count: 1 },
            { value: 'Medium', count: 1 },
            { value: 'Positive', count: 2 },
          ],
          direction: [
            { value: 'Harmony', count: 2 },
            { value: 'Defensive', count: 1 },
            { value: 'Offensive', count: 0 },
            { value: 'Stability', count: 1 },
          ],
          source: [
            { value: 'Internal', count: 4 },
            { value: 'External', count: 0 },
          ],
        },
      },
    ],
  }
}

function emptyDashboard(scale: string, period: string): MoodDashboardResponseFixture {
  return {
    scale: responseScale(scale),
    period,
    from: '2026-01-01',
    to: '2026-12-31',
    previousPeriod: '2025',
    nextPeriod: '2027',
    bucketGranularity: 'Month',
    entryCount: 0,
    scoreByDayOfWeek: [],
    distribution: { energy: [], alignment: [], direction: [], source: [] },
    buckets: [],
  }
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
