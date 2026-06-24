import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { CalendarDailyNote, CalendarEntry } from '@/app/api/calendar'
import { resetCsrfToken } from '@/app/api/client'

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

const noteEntry: CalendarEntry = {
  id: 'calendar:note:5',
  sourceModule: 'calendar',
  sourceType: 'dailyNote',
  visualFamily: 'Note',
  title: 'Call the plumber',
  subtitle: 'Ring at noon',
  startDate: '2026-06-24',
  endDate: null,
  isAllDay: true,
  status: null,
  targetRoute: '/calendar?day=2026-06-24&noteId=5',
}

function noteDetail(overrides: Partial<CalendarDailyNote> = {}): CalendarDailyNote {
  return {
    id: 5,
    date: '2026-06-24',
    title: 'Call the plumber',
    body: 'Ring at noon',
    visibility: 'Private',
    createdById: 7,
    createdByName: 'marina',
    createdAt: '2026-06-20T10:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
    ...overrides,
  }
}

interface BackendOptions {
  entries?: CalendarEntry[]
  note?: CalendarDailyNote
  noteStatus?: number
  noteProblem?: unknown
  mutationStatus?: number
  mutationProblem?: unknown
}

function mockBackend(options: BackendOptions = {}) {
  const entries = options.entries ?? [noteEntry]
  const note = options.note ?? noteDetail()
  const requests: Array<{ method: string; url: string; body: unknown }> = []

  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    await Promise.resolve()
    const url = urlOf(input)
    const method = init?.method ?? 'GET'
    const body: unknown =
      typeof init?.body === 'string' ? JSON.parse(init.body) : undefined
    requests.push({ method, url, body })

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
      return json(entries)
    }
    if (url === '/api/calendar/notes/5' && method === 'GET') {
      if (options.noteStatus != null) {
        return json(options.noteProblem ?? {}, options.noteStatus)
      }
      return json(note)
    }
    if (url === '/api/calendar/notes' && method === 'POST') {
      if (options.mutationStatus != null) {
        return json(options.mutationProblem ?? {}, options.mutationStatus)
      }
      return json(noteDetail({ id: 9, ...(body as object) }), 201)
    }
    if (url === '/api/calendar/notes/5' && method === 'PUT') {
      if (options.mutationStatus != null) {
        return json(options.mutationProblem ?? {}, options.mutationStatus)
      }
      return json(noteDetail({ ...(body as object) }))
    }
    if (url === '/api/calendar/notes/5' && method === 'DELETE') {
      return new Response(null, { status: 204 })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return {
    requests,
    entriesRequests: () =>
      requests.filter(
        (request) =>
          request.method === 'GET' && request.url.includes('/api/calendar/entries'),
      ),
  }
}

let mockBackendSpy: ReturnType<typeof mockBackend>

beforeEach(() => {
  appQueryClient.clear()
  resetCsrfToken()
  window.history.replaceState({}, '', '/calendar?month=2026-06&day=2026-06-24')
})

afterEach(() => vi.restoreAllMocks())

async function openCreateDialog() {
  mockBackendSpy = mockBackend()
  const user = userEvent.setup()
  render(<App />)
  await screen.findByText('Call the plumber')
  await user.click(screen.getByRole('button', { name: 'New note' }))
  return user
}

describe('Calendar daily-note workflow', () => {
  it('opens the create editor with the Private default and the selected day', async () => {
    await openCreateDialog()

    const dialog = await screen.findByRole('dialog', { name: 'New note' })
    expect(window.location.search).toContain('newNote=true')
    expect(within(dialog).getByLabelText('Date')).toHaveValue('2026-06-24')
    expect(within(dialog).getByRole('radio', { name: 'Private' })).toBeChecked()
    expect(within(dialog).getByRole('radio', { name: 'Public' })).not.toBeChecked()
  })

  it('blocks submission and shows a body error when the note is empty', async () => {
    const user = await openCreateDialog()
    const { requests } = mockBackendSpy

    await user.click(screen.getByRole('button', { name: 'Save note' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'The note body is required.',
    )
    expect(requests.some((request) => request.method === 'POST')).toBe(false)
  })

  it('creates a note, refetches entries, confirms with a toast, and closes', async () => {
    const user = await openCreateDialog()
    const dialog = await screen.findByRole('dialog', { name: 'New note' })
    const before = mockBackendSpy.entriesRequests().length

    await user.type(within(dialog).getByLabelText('Note'), 'Buy bread')
    await user.click(within(dialog).getByRole('radio', { name: 'Public' }))
    await user.click(within(dialog).getByRole('button', { name: 'Save note' }))

    await waitFor(() =>
      expect(
        mockBackendSpy.requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/calendar/notes' &&
            (request.body as { body: string }).body === 'Buy bread' &&
            (request.body as { visibility: string }).visibility === 'Public' &&
            (request.body as { date: string }).date === '2026-06-24',
        ),
      ).toBe(true),
    )
    expect(await screen.findByText('Note created')).toBeInTheDocument()
    await waitFor(() => expect(window.location.search).not.toContain('newNote'))
    await waitFor(() =>
      expect(mockBackendSpy.entriesRequests().length).toBeGreaterThan(before),
    )
  })

  it('opens the edit editor from a day-detail note and prefills it', async () => {
    const user = userEvent.setup()
    mockBackendSpy = mockBackend()
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Edit note Call the plumber' }),
    )

    const dialog = await screen.findByRole('dialog', { name: 'Edit note' })
    expect(window.location.search).toContain('noteId=5')
    expect(within(dialog).getByLabelText('Note')).toHaveValue('Ring at noon')
    expect(within(dialog).getByRole('radio', { name: 'Private' })).toBeChecked()
  })

  it('updates a note through the edit editor', async () => {
    const user = userEvent.setup()
    mockBackendSpy = mockBackend()
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Edit note Call the plumber' }),
    )
    const dialog = await screen.findByRole('dialog', { name: 'Edit note' })
    const body = within(dialog).getByLabelText('Note')
    await user.clear(body)
    await user.type(body, 'Ring at three')
    await user.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    await waitFor(() =>
      expect(
        mockBackendSpy.requests.some(
          (request) =>
            request.method === 'PUT' &&
            request.url === '/api/calendar/notes/5' &&
            (request.body as { body: string }).body === 'Ring at three',
        ),
      ).toBe(true),
    )
    expect(await screen.findByText('Note updated')).toBeInTheDocument()
  })

  it('requires confirmation before deleting a note', async () => {
    const user = userEvent.setup()
    mockBackendSpy = mockBackend()
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Edit note Call the plumber' }),
    )
    await user.click(await screen.findByRole('button', { name: 'Delete' }))

    const confirm = await screen.findByRole('dialog', { name: 'Delete this note?' })
    expect(mockBackendSpy.requests.some((request) => request.method === 'DELETE')).toBe(
      false,
    )

    await user.click(within(confirm).getByRole('button', { name: 'Delete note' }))

    await waitFor(() =>
      expect(
        mockBackendSpy.requests.some(
          (request) =>
            request.method === 'DELETE' && request.url === '/api/calendar/notes/5',
        ),
      ).toBe(true),
    )
    expect(await screen.findByText('Note deleted')).toBeInTheDocument()
  })

  it('confirms a dirty close and keeps user input when staying', async () => {
    const user = await openCreateDialog()
    const dialog = await screen.findByRole('dialog', { name: 'New note' })

    await user.type(within(dialog).getByLabelText('Note'), 'Draft note')
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }))

    const confirm = await screen.findByRole('dialog', {
      name: 'Discard your changes?',
    })
    // The close (X) button and the footer action share the "Keep editing" name;
    // the footer action is the last match.
    const keep = within(confirm).getAllByRole('button', { name: 'Keep editing' })
    await user.click(keep[keep.length - 1])

    expect(within(dialog).getByLabelText('Note')).toHaveValue('Draft note')
    expect(window.location.search).toContain('newNote=true')
  })

  it('surfaces a privacy-safe not-found message when an edited note is gone', async () => {
    const user = userEvent.setup()
    mockBackendSpy = mockBackend({
      noteStatus: 404,
      noteProblem: { code: 'calendar.note.not_found' },
    })
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Edit note Call the plumber' }),
    )

    expect(
      await screen.findByText('This note no longer exists or is not available to you.'),
    ).toBeInTheDocument()
  })

  it('locks visibility for a note created by another user', async () => {
    const user = userEvent.setup()
    mockBackendSpy = mockBackend({ note: noteDetail({ createdById: 99 }) })
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Edit note Call the plumber' }),
    )

    const dialog = await screen.findByRole('dialog', { name: 'Edit note' })
    expect(within(dialog).getByRole('radio', { name: 'Private' })).toBeDisabled()
    expect(
      within(dialog).getByText('Only the note creator can change visibility.'),
    ).toBeInTheDocument()
  })
})
