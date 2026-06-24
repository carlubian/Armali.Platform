import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { CalendarEntry } from '@/app/api/calendar'

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

function entry(id: string, overrides: Partial<CalendarEntry>): CalendarEntry {
  return {
    id,
    sourceModule: 'calendar',
    sourceType: 'dailyNote',
    visualFamily: 'Note',
    title: id,
    subtitle: null,
    startDate: '2026-06-24',
    endDate: null,
    isAllDay: true,
    status: null,
    targetRoute: null,
    ...overrides,
  }
}

function mockBackend(entries: CalendarEntry[] = calendarEntries) {
  const requests: Array<{ method: string; url: string }> = []
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
    if (url.startsWith('/api/calendar/entries') && method === 'GET') {
      requests.push({ method, url })
      return json(entries)
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  return { requests }
}

const calendarEntries: CalendarEntry[] = [
  entry('trip-1', {
    sourceModule: 'travel',
    sourceType: 'trip',
    visualFamily: 'Travel',
    title: 'Girona & the Costa Brava',
    subtitle: 'Whole household',
    startDate: '2026-06-24',
    endDate: '2026-06-28',
    status: 'Confirmed',
    targetRoute: '/travel?tripId=1',
  }),
  entry('birthday-1', {
    sourceModule: 'firebird',
    sourceType: 'birthday',
    visualFamily: 'Birthday',
    title: 'Abuela Carmen',
    subtitle: 'Turns 79',
    status: 'Recurring',
  }),
  entry('note-1', {
    title: 'Call the plumber',
  }),
  entry('task-1', {
    sourceModule: 'maintenance',
    sourceType: 'maintenanceTaskDue',
    visualFamily: 'Other',
    title: 'Replace smoke-alarm batteries',
    status: 'In progress',
  }),
]

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/calendar?month=2026-06&day=2026-06-24')
})

afterEach(() => vi.restoreAllMocks())

describe('CalendarPage', () => {
  it('requests the visible Monday-first grid range including adjacent days', async () => {
    const { requests } = mockBackend()
    render(<App />)

    expect(await screen.findByText('Girona & the Costa Brava')).toBeInTheDocument()
    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('/api/calendar/entries') &&
            request.url.includes('from=2026-06-01') &&
            request.url.includes('to=2026-07-05'),
        ),
      ).toBe(true),
    )
  })

  it('renders current and selected day states with priority-plus-more indicators', async () => {
    mockBackend()
    render(<App />)

    const day = await screen.findByRole('button', {
      name: /Wednesday, 24 June 2026, 4 entries/,
    })
    expect(day).toHaveAttribute('aria-current', 'date')
    expect(day).toHaveAttribute('aria-pressed', 'true')
    expect(within(day).getByLabelText(/Travel entry: Girona/)).toBeInTheDocument()
    expect(within(day).getByLabelText('1 more entry families')).toBeInTheDocument()
  })

  it('navigates months without a page reload and refreshes the queried range', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByRole('button', {
      name: /Wednesday, 24 June 2026, 4 entries/,
    })
    await user.click(screen.getByRole('button', { name: 'Next month' }))

    expect(window.location.search).toContain('month=2026-07')
    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('from=2026-06-29') &&
            request.url.includes('to=2026-08-02'),
        ),
      ).toBe(true),
    )
  })

  it('backs source and visual-family filters with URL query state', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Girona & the Costa Brava')
    await user.click(screen.getByRole('button', { name: 'Toggle Birthday family' }))
    await user.click(screen.getByRole('button', { name: 'Toggle Calendar source' }))

    expect(window.location.search).toContain('visualFamily=Travel')
    expect(window.location.search).not.toContain('visualFamily=Birthday')
    expect(window.location.search).toContain('sourceModule=travel')
    expect(window.location.search).not.toContain('sourceModule=calendar')

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('visualFamily=Travel') &&
            request.url.includes('visualFamily=Note') &&
            request.url.includes('sourceModule=travel') &&
            !request.url.includes('sourceModule=calendar'),
        ),
      ).toBe(true),
    )
  })

  it('surfaces loading failure with a retry action', async () => {
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
      if (url.startsWith('/api/calendar/entries')) {
        return json({ title: 'Broken' }, 400)
      }
      throw new Error(`Unexpected request: ${method} ${url}`)
    })

    render(<App />)

    expect(
      await screen.findByText('Calendar entries could not be loaded. Please try again.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
  })
})
