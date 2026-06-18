import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { CreateMoodEntryRequest, MoodEntry, MoodEntryList } from '@/app/api/mood'

import { moodEntry } from './testing/moodFixtures'

// Pin "today" so the default week (Mon 15 – Sun 21 Jun 2026) is deterministic.
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
  return new Response(status === 204 ? null : JSON.stringify(body), {
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

const weekEntries: MoodEntry[] = [
  moodEntry({
    id: 901,
    entryDate: '2026-06-15',
    score: 4,
    energy: 'Medium',
    alignment: 'Positive',
    direction: 'Harmony',
    source: 'Internal',
    derivedEmotion: 'Grateful',
    notes: 'A good walk by the harbour.',
  }),
  moodEntry({
    id: 921,
    entryDate: '2026-06-17',
    score: 3,
    energy: 'High',
    alignment: 'Medium',
    direction: 'Stability',
    source: 'External',
    derivedEmotion: 'Focused',
    notes: null,
  }),
]

function listFor(from: string, to: string, entries: MoodEntry[]): MoodEntryList {
  const dates = entries.map((entry) => entry.entryDate)
  return {
    from,
    to,
    entries,
    dailyAverages: Array.from({ length: 7 }, (_, index) => {
      const date = isoAddDays(from, index)
      const dayScores = entries
        .filter((entry) => entry.entryDate === date)
        .map((entry) => entry.score)
      return {
        entryDate: date,
        averageScore: dates.includes(date)
          ? dayScores.reduce((total, score) => total + score, 0) / dayScores.length
          : null,
      }
    }),
  }
}

function isoAddDays(iso: string, days: number): string {
  const [year, month, day] = iso.split('-').map(Number)
  const date = new Date(Date.UTC(year, month - 1, day))
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}

interface BackendOptions {
  currentWeek?: MoodEntry[]
  entryStatus?: number
}

function mockBackend(options: BackendOptions = {}) {
  let currentWeek = options.currentWeek ?? weekEntries
  const requests: Array<{ method: string; url: string; body?: string }> = []

  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    await Promise.resolve()
    const url = urlOf(input)
    const method = init?.method ?? 'GET'
    requests.push({ method, url, body: init?.body as string | undefined })

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

    if (url.startsWith('/api/mood/derived-emotion') && method === 'GET') {
      const parsed = new URL(url, 'http://localhost')
      const energy = parsed.searchParams.get('energy')
      const alignment = parsed.searchParams.get('alignment')
      const direction = parsed.searchParams.get('direction')
      const source = parsed.searchParams.get('source')
      const derivedEmotion =
        energy === 'High' &&
        alignment === 'Positive' &&
        direction === 'Harmony' &&
        source === 'Internal'
          ? 'Happy'
          : 'Optimistic'
      return json({ derivedEmotion })
    }

    const entryDetail = url.match(/\/api\/mood\/entries\/(\d+)(\?|$)/)
    if (entryDetail != null && method === 'GET') {
      if (options.entryStatus === 404)
        return json({ code: 'mood.entry.not_found' }, 404)
      const found = currentWeek.find((entry) => entry.id === Number(entryDetail[1]))
      return found != null ? json(found) : json({ code: 'mood.entry.not_found' }, 404)
    }
    if (entryDetail != null && method === 'PUT') {
      const body = JSON.parse(init?.body as string) as CreateMoodEntryRequest
      const id = Number(entryDetail[1])
      const updated = {
        ...(currentWeek.find((entry) => entry.id === id) ?? currentWeek[0]),
        ...body,
        id,
      }
      currentWeek = currentWeek.map((entry) => (entry.id === id ? updated : entry))
      return json(updated)
    }
    if (entryDetail != null && method === 'DELETE') return json(null, 204)

    if (url.startsWith('/api/mood/entries') && method === 'POST') {
      const body = JSON.parse(init?.body as string) as CreateMoodEntryRequest
      const created: MoodEntry = {
        ...moodEntry(),
        ...body,
        id: 999,
        derivedEmotion: 'Energetic',
      }
      return json(created, 201)
    }

    if (url.startsWith('/api/mood/entries') && method === 'GET') {
      const parsed = new URL(url, 'http://localhost')
      const from = parsed.searchParams.get('from') ?? ''
      const to = parsed.searchParams.get('to') ?? ''
      const entries = from === '2026-06-15' ? currentWeek : []
      return json(listFor(from, to, entries))
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/mood/log')
})

afterEach(() => vi.restoreAllMocks())

describe('Mood log view', () => {
  it('renders the selected week with derived emotion and criteria', async () => {
    mockBackend()
    render(<App />)

    expect(await screen.findByText('Grateful')).toBeInTheDocument()
    expect(screen.getByText('Focused')).toBeInTheDocument()
    // Criteria render as labelled pills, not raw notes.
    expect(screen.getAllByText('Positive').length).toBeGreaterThan(0)
    expect(screen.queryByText('A good walk by the harbour.')).not.toBeInTheDocument()
  })

  it('defaults to the week containing today and requests its inclusive range', async () => {
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Grateful')
    expect(
      requests.some(
        (r) => r.url.includes('from=2026-06-15') && r.url.includes('to=2026-06-21'),
      ),
    ).toBe(true)
  })

  it('navigates to the previous week and back to today', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Grateful')
    expect(screen.getByRole('button', { name: 'Today' })).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Previous week' }))
    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('from=2026-06-08'))).toBe(true),
    )
    expect(screen.getByRole('button', { name: 'Today' })).toBeEnabled()
  })

  it('reads the selected week from the URL', async () => {
    window.history.replaceState({}, '', '/mood/log?week=2026-06-08')
    const { requests } = mockBackend()
    render(<App />)

    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('from=2026-06-08'))).toBe(true),
    )
  })

  it('renders the weekly average chart with a missing-day alternative', async () => {
    mockBackend()
    render(<App />)

    await screen.findByText('Grateful')
    const chart = screen.getByRole('img', { name: 'Average score per day this week' })
    expect(chart).toBeInTheDocument()
  })

  it('validates the new entry form before submitting', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Grateful')
    await user.click(screen.getByRole('button', { name: 'New entry' }))

    const dialog = await screen.findByRole('dialog')
    // The date defaults to today regardless of the selected week.
    expect(within(dialog).getByLabelText(/Entry date/)).toHaveValue(TODAY)

    await user.click(within(dialog).getByRole('button', { name: 'Save entry' }))
    expect(await screen.findByText('Choose a score from 1 to 5.')).toBeInTheDocument()
    expect(requests.some((r) => r.method === 'POST')).toBe(false)
  })

  it('creates an entry and shows success feedback', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Grateful')
    await user.click(screen.getByRole('button', { name: 'New entry' }))
    const dialog = await screen.findByRole('dialog')

    await user.click(
      within(within(dialog).getByRole('group', { name: 'Score' })).getByRole('button', {
        name: '4',
      }),
    )
    await user.click(
      within(within(dialog).getByRole('group', { name: 'Energy' })).getByRole(
        'button',
        { name: 'High' },
      ),
    )
    await user.click(
      within(within(dialog).getByRole('group', { name: 'Alignment' })).getByRole(
        'button',
        { name: 'Positive' },
      ),
    )
    await user.click(
      within(within(dialog).getByRole('group', { name: 'Direction' })).getByRole(
        'button',
        { name: 'Harmony' },
      ),
    )
    await user.click(
      within(within(dialog).getByRole('group', { name: 'Source' })).getByRole(
        'button',
        { name: 'Internal' },
      ),
    )
    expect(await within(dialog).findByText('Happy')).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Save entry' }))

    await waitFor(() => expect(requests.some((r) => r.method === 'POST')).toBe(true))
    expect(await screen.findByText('Entry saved')).toBeInTheDocument()
  })

  it('keeps notes focused while typing and reopens with saved notes', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: /Grateful/ }))
    let dialog = await screen.findByRole('dialog')
    let notes = within(dialog).getByLabelText(/Notes/) as HTMLTextAreaElement
    expect(notes).toHaveValue('A good walk by the harbour.')

    await user.clear(notes)
    expect(notes).toHaveFocus()
    await user.type(notes, 'Updated private note')
    expect(notes).toHaveFocus()

    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))
    await waitFor(() => expect(requests.some((r) => r.method === 'PUT')).toBe(true))
    expect(await screen.findByText('Entry updated')).toBeInTheDocument()

    await user.click(await screen.findByRole('button', { name: /Grateful/ }))
    dialog = await screen.findByRole('dialog')
    notes = within(dialog).getByLabelText(/Notes/) as HTMLTextAreaElement
    expect(notes).toHaveValue('Updated private note')
  })

  it('confirms before discarding a dirty entry', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Grateful')
    await user.click(screen.getByRole('button', { name: 'New entry' }))
    const dialog = await screen.findByRole('dialog')

    await user.click(
      within(within(dialog).getByRole('group', { name: 'Score' })).getByRole('button', {
        name: '4',
      }),
    )
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }))

    expect(await screen.findByText('Discard unsaved changes?')).toBeInTheDocument()
  })

  it('opens an entry and deletes it', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: /Grateful/ }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByText('Edit entry')).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))
    await user.click(await screen.findByRole('button', { name: 'Delete entry' }))

    await waitFor(() => expect(requests.some((r) => r.method === 'DELETE')).toBe(true))
    expect(await screen.findByText('Entry deleted')).toBeInTheDocument()
  })

  it('shows a privacy-safe message when an entry is not found', async () => {
    window.history.replaceState({}, '', '/mood/log?entryId=555')
    mockBackend({ entryStatus: 404 })
    render(<App />)

    expect(
      await screen.findByText('This entry no longer exists. It may have been deleted.'),
    ).toBeInTheDocument()
  })
})
