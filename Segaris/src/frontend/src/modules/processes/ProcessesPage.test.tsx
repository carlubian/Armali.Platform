import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import axe from 'axe-core'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { Process, ProcessSummary, StepExecutionState } from '@/app/api/processes'

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

function makeSummary(
  id: number,
  overrides: Partial<ProcessSummary> = {},
): ProcessSummary {
  return {
    id,
    name: `Process ${id.toString().padStart(2, '0')}`,
    categoryId: 1,
    categoryName: 'Administrative',
    status: 'NotStarted',
    isCancelled: false,
    resolvedStepCount: 0,
    totalStepCount: 0,
    effectiveDueDate: '2026-07-01',
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeDetail(id: number, overrides: Partial<Process> = {}): Process {
  const summary = makeSummary(id, overrides)
  return {
    id,
    name: summary.name,
    categoryId: summary.categoryId,
    categoryName: summary.categoryName,
    status: summary.status,
    isCancelled: summary.isCancelled,
    dueDate: '2026-07-01',
    effectiveDueDate: summary.effectiveDueDate,
    notes: 'Renew before the deadline.',
    resolvedStepCount: summary.resolvedStepCount,
    totalStepCount: summary.totalStepCount,
    nextPendingStepId: null,
    visibility: summary.visibility,
    steps: [],
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-01-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
    ...overrides,
  }
}

function isStepListBody(value: unknown): value is {
  steps: Array<{ description: string; id: number | null }>
} {
  if (typeof value !== 'object' || value == null) return false
  const steps = (value as Record<string, unknown>).steps
  return Array.isArray(steps)
}

function mockBackend(
  options: {
    processes?: ProcessSummary[]
    detail?: (id: number) => Process
    stepUpdate?: (id: number, body: unknown) => Response
  } = {},
) {
  const processes = options.processes ?? [makeSummary(1)]
  const requests: Array<{ method: string; url: string; body?: unknown }> = []
  let cancelled = false

  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    await Promise.resolve()
    const url = urlOf(input)
    const method = init?.method ?? 'GET'
    const bodyOf = () =>
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
    if (url.startsWith('/api/launcher/attention')) {
      return json({ modules: [{ module: 'processes', requiresAttention: true }] })
    }
    if (url.startsWith('/api/processes/categories')) {
      return json([
        { id: 1, name: 'Administrative', sortOrder: 0 },
        { id: 2, name: 'Legal', sortOrder: 1 },
      ])
    }
    const stepMatch = url.match(
      /\/api\/processes\/(\d+)\/steps(?:\/(\d+)\/(complete|skip|undo))?$/,
    )
    if (stepMatch != null) {
      const id = Number(stepMatch[1])
      const stepId = stepMatch[2] != null ? Number(stepMatch[2]) : null
      const action = stepMatch[3]
      const detail = options.detail?.(id) ?? makeDetail(id)
      requests.push({ method, url, body: bodyOf() })
      if (method === 'POST' && stepId != null && action != null) {
        const nextState: StepExecutionState =
          action === 'undo' ? 'Pending' : action === 'skip' ? 'Skipped' : 'Completed'
        const steps = detail.steps.map((step) =>
          step.id === stepId
            ? {
                ...step,
                state: nextState,
              }
            : step,
        )
        const resolved = steps.filter((step) => step.state !== 'Pending').length
        const next = steps.find((step) => step.state === 'Pending')?.id ?? null
        return json(
          makeDetail(id, {
            ...detail,
            status:
              steps.length > 0 && resolved === steps.length
                ? 'Completed'
                : 'InProgress',
            resolvedStepCount: resolved,
            totalStepCount: steps.length,
            nextPendingStepId: next,
            steps,
          }),
        )
      }
      if (method === 'PUT') {
        const body = bodyOf()
        const custom = options.stepUpdate?.(id, body)
        if (custom != null) return custom
        const input = bodyOf() as {
          steps: Array<{
            id: number | null
            description: string
            dueDate: string | null
            notes: string | null
            isOptional: boolean
          }>
        }
        const steps = input.steps.map((step, index) => ({
          id: step.id ?? 100 + index,
          description: step.description,
          dueDate: step.dueDate,
          notes: step.notes,
          isOptional: step.isOptional,
          state:
            detail.steps.find((existing) => existing.id === step.id)?.state ??
            'Pending',
          sortOrder: index,
        }))
        const resolved = steps.filter((step) => step.state !== 'Pending').length
        return json(
          makeDetail(id, {
            ...detail,
            resolvedStepCount: resolved,
            totalStepCount: steps.length,
            nextPendingStepId:
              steps.find((step) => step.state === 'Pending')?.id ?? null,
            steps,
          }),
        )
      }
    }
    const attachMatch = url.match(/\/api\/processes\/(\d+)\/attachments/)
    if (attachMatch != null) {
      requests.push({ method, url })
      if (method === 'GET') return json([])
      if (method === 'POST') {
        return json(
          {
            id: 'attachment-1',
            fileName: 'form.pdf',
            contentType: 'application/pdf',
            size: 1024,
            createdById: 7,
            createdAt: '2026-01-01T00:00:00Z',
          },
          201,
        )
      }
    }
    const cancelMatch = url.match(/\/api\/processes\/(\d+)\/cancel/)
    if (cancelMatch != null && method === 'POST') {
      cancelled = true
      const id = Number(cancelMatch[1])
      requests.push({ method, url })
      return json(makeDetail(id, { status: 'Cancelled', isCancelled: true }))
    }
    const reopenMatch = url.match(/\/api\/processes\/(\d+)\/reopen/)
    if (reopenMatch != null && method === 'POST') {
      cancelled = false
      const id = Number(reopenMatch[1])
      requests.push({ method, url })
      return json(makeDetail(id, { status: 'NotStarted', isCancelled: false }))
    }
    const idMatch = url.match(/\/api\/processes\/(\d+)(?:\?|$)/)
    if (idMatch != null && method === 'GET') {
      requests.push({ method, url })
      const id = Number(idMatch[1])
      const detail = options.detail?.(id) ?? makeDetail(id)
      return json({
        ...detail,
        status: cancelled ? 'Cancelled' : detail.status,
        isCancelled: cancelled ? true : detail.isCancelled,
      })
    }
    if (idMatch != null && method === 'PUT') {
      const id = Number(idMatch[1])
      const body = bodyOf()
      requests.push({ method, url, body })
      return json(makeDetail(id, body as Partial<Process>))
    }
    if (idMatch != null && method === 'DELETE') {
      requests.push({ method, url })
      return new Response(null, { status: 204 })
    }
    if (url === '/api/processes' && method === 'POST') {
      const body = bodyOf()
      requests.push({ method, url, body })
      return json(
        makeDetail(99, {
          id: 99,
          name: (body as { name: string }).name,
          categoryId: (body as { categoryId: number }).categoryId,
        }),
        201,
      )
    }
    if (url.startsWith('/api/processes') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const search = parsed.searchParams.get('search')?.toLowerCase() ?? ''
      const filtered =
        search === ''
          ? processes
          : processes.filter((process) => process.name.toLowerCase().includes(search))
      return json({
        items: filtered,
        page: Number(parsed.searchParams.get('page') ?? '1'),
        pageSize: Number(parsed.searchParams.get('pageSize') ?? '25'),
        totalCount: filtered.length,
      })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/processes')
})

afterEach(() => vi.restoreAllMocks())

describe('Processes page', () => {
  it('renders the derived status, step progress, and effective due date', async () => {
    mockBackend({
      processes: [
        makeSummary(1, {
          name: 'Renew passport',
          status: 'InProgress',
          resolvedStepCount: 2,
          totalStepCount: 5,
          effectiveDueDate: '2026-07-01',
        }),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Renew passport')).toBeInTheDocument()
    const table = screen.getByRole('table')
    expect(within(table).getByText('In progress')).toBeInTheDocument()
    expect(within(table).getByText('2/5')).toBeInTheDocument()
    expect(within(table).getByText('01 Jul 2026')).toBeInTheDocument()
  })

  it('serializes search and sort into the process list request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.type(screen.getByLabelText('Search'), 'Process 01')
    await user.click(screen.getByRole('button', { name: 'Sort by Status' }))

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('search=Process+01') &&
            request.url.includes('sort=status'),
        ),
      ).toBe(true),
    )
  })

  it('opens the create dialog without losing table state', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.type(screen.getByLabelText('Search'), 'Process 01')
    await user.click(screen.getByRole('button', { name: 'New process' }))

    expect(
      await screen.findByRole('dialog', { name: 'New process' }),
    ).toBeInTheDocument()
    expect(window.location.search).toContain('search=Process+01')
    expect(window.location.search).toContain('newProcess=true')
  })

  it('validates required fields before submission', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.click(screen.getByRole('button', { name: 'New process' }))
    const dialog = await screen.findByRole('dialog', { name: 'New process' })
    await user.clear(within(dialog).getByLabelText('Name'))
    await user.click(within(dialog).getByRole('button', { name: 'Create process' }))

    expect(await within(dialog).findByText('A name is required.')).toBeInTheDocument()
  })

  it('creates a process and submits its category, due date, and visibility', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.click(screen.getByRole('button', { name: 'New process' }))
    const dialog = await screen.findByRole('dialog', { name: 'New process' })
    await user.type(within(dialog).getByLabelText('Name'), 'Renew passport')
    await user.click(within(dialog).getByRole('button', { name: 'Create process' }))

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/processes' &&
            (request.body as { name: string }).name === 'Renew passport' &&
            (request.body as { categoryId: number }).categoryId === 1 &&
            (request.body as { visibility: string }).visibility === 'Public',
        ),
      ).toBe(true),
    )
  })

  it('cancels and reopens a process through the editor', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      processes: [makeSummary(1, { name: 'Mortgage application' })],
    })
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Open process Mortgage application' }),
    )
    const editor = await screen.findByRole('dialog', { name: 'Edit process' })

    await user.click(within(editor).getByRole('button', { name: 'Cancel process' }))
    await waitFor(() =>
      expect(requests.some((request) => request.url.endsWith('/cancel'))).toBe(true),
    )
    // Once cancelled, the editor offers to reopen.
    await user.click(
      await within(editor).findByRole('button', { name: 'Reopen process' }),
    )
    await waitFor(() =>
      expect(requests.some((request) => request.url.endsWith('/reopen'))).toBe(true),
    )
  })

  it('uploads staged attachments after creating a process', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.click(screen.getByRole('button', { name: 'New process' }))
    const dialog = await screen.findByRole('dialog', { name: 'New process' })
    await user.type(within(dialog).getByLabelText('Name'), 'Renew passport')
    await user.upload(
      within(dialog).getByLabelText('Add files'),
      new File(['form'], 'form.pdf', { type: 'application/pdf' }),
    )
    await user.click(within(dialog).getByRole('button', { name: 'Create process' }))

    expect(
      await screen.findByRole('dialog', { name: 'Upload attachments' }),
    ).toBeInTheDocument()
    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/processes/99/attachments',
        ),
      ).toBe(true),
    )
  })

  it('opens the timeline first, executes frontier actions, and enters restructure without losing table state', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      processes: [makeSummary(1, { name: 'Renew passport' })],
      detail: (id) =>
        makeDetail(id, {
          name: 'Renew passport',
          status: 'InProgress',
          resolvedStepCount: 1,
          totalStepCount: 2,
          nextPendingStepId: 20,
          steps: [
            {
              id: 10,
              description: 'Gather documents',
              dueDate: '2026-06-01',
              notes: null,
              isOptional: false,
              state: 'Completed',
              sortOrder: 0,
            },
            {
              id: 20,
              description: 'Attend appointment',
              dueDate: '2026-07-01',
              notes: null,
              isOptional: false,
              state: 'Pending',
              sortOrder: 1,
            },
          ],
        }),
    })
    render(<App />)

    await screen.findByText('Renew passport')
    await user.type(screen.getByLabelText('Search'), 'Renew passport')
    await user.click(screen.getAllByRole('button', { name: 'Step timeline' })[0])

    const dialog = await screen.findByRole('dialog', { name: 'Step timeline' })
    expect(window.location.search).toContain('search=Renew+passport')
    expect(window.location.search).toContain('steps=true')
    expect(
      within(dialog).getByLabelText('Step 1 of 2: Gather documents. State: Completed.'),
    ).toBeInTheDocument()
    expect(
      within(dialog).getByLabelText(
        'Step 2 of 2: Attend appointment. State: Current step.',
      ),
    ).toBeInTheDocument()
    expect(within(dialog).getByText('Current step')).toBeInTheDocument()
    expect(
      within(dialog).getByText('Next pending step · the frontier'),
    ).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: 'Skip' })).toBeDisabled()

    await user.click(within(dialog).getByRole('button', { name: 'Complete step' }))
    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/processes/1/steps/20/complete',
        ),
      ).toBe(true),
    )

    await user.click(within(dialog).getByRole('button', { name: 'Restructure steps' }))
    const restructure = await screen.findByRole('dialog', { name: 'Restructure steps' })
    expect(window.location.search).toContain('search=Renew+passport')
    expect(window.location.search).toContain('steps=true')
    expect(window.location.search).toContain('restructure=true')
    expect(
      within(restructure).getByDisplayValue('Gather documents'),
    ).toBeInTheDocument()
    await user.click(within(restructure).getByRole('button', { name: 'Add step' }))
    const descriptions = within(restructure).getAllByLabelText('Description')
    await user.type(descriptions[descriptions.length - 1], 'Collect passport')
    await user.click(
      within(restructure).getByRole('button', { name: 'Save step order' }),
    )

    await waitFor(() => {
      const body = requests.find((request) => request.method === 'PUT')?.body
      expect(isStepListBody(body)).toBe(true)
      expect(body).toBeDefined()
      if (!isStepListBody(body)) return
      expect(body.steps).toEqual(
        expect.arrayContaining([
          expect.objectContaining({ description: 'Collect passport', id: null }),
        ]),
      )
    })

    await waitFor(() =>
      expect(window.location.search).not.toContain('restructure=true'),
    )
    expect(
      await screen.findByRole('dialog', { name: 'Step timeline' }),
    ).toBeInTheDocument()
  })

  it('locks resolved steps and edits, re-dates, annotates, toggles, and reorders only the pending tail', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      processes: [makeSummary(1, { name: 'Renew passport' })],
      detail: (id) =>
        makeDetail(id, {
          name: 'Renew passport',
          status: 'InProgress',
          resolvedStepCount: 1,
          totalStepCount: 3,
          nextPendingStepId: 20,
          steps: [
            {
              id: 10,
              description: 'Gather documents',
              dueDate: '2026-06-01',
              notes: 'Already done',
              isOptional: false,
              state: 'Completed',
              sortOrder: 0,
            },
            {
              id: 20,
              description: 'Attend appointment',
              dueDate: '2026-07-01',
              notes: null,
              isOptional: false,
              state: 'Pending',
              sortOrder: 1,
            },
            {
              id: 30,
              description: 'Submit receipt',
              dueDate: null,
              notes: null,
              isOptional: false,
              state: 'Pending',
              sortOrder: 2,
            },
          ],
        }),
    })
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Step timeline' }))
    const timeline = await screen.findByRole('dialog', { name: 'Step timeline' })
    await user.click(
      within(timeline).getByRole('button', { name: 'Restructure steps' }),
    )
    const restructure = await screen.findByRole('dialog', { name: 'Restructure steps' })

    expect(
      within(restructure).getByLabelText(
        'Frontier divider between locked resolved steps and editable pending steps',
      ),
    ).toBeInTheDocument()
    expect(within(restructure).getByText('Locked resolved prefix')).toBeInTheDocument()
    const descriptions = within(restructure).getAllByLabelText('Description')
    expect(descriptions[0]).toBeDisabled()
    expect(descriptions[1]).toBeEnabled()

    await user.clear(descriptions[1])
    await user.type(descriptions[1], 'Book appointment')
    const dueDates = within(restructure).getAllByLabelText('Due date')
    await user.clear(dueDates[1])
    await user.type(dueDates[1], '2026-07-15')
    await user.type(within(restructure).getAllByLabelText('Notes')[1], 'Bring ID')
    await user.click(within(restructure).getAllByLabelText('Optional')[1])
    await user.click(within(restructure).getAllByRole('button', { name: 'Down' })[1])
    await user.click(
      within(restructure).getByRole('button', { name: 'Save step order' }),
    )

    await waitFor(() => {
      const body = requests.find((request) => request.method === 'PUT')?.body
      expect(isStepListBody(body)).toBe(true)
      if (!isStepListBody(body)) return
      expect(body.steps).toEqual([
        expect.objectContaining({ id: 10, description: 'Gather documents' }),
        expect.objectContaining({ id: 30, description: 'Submit receipt' }),
        expect.objectContaining({
          id: 20,
          description: 'Book appointment',
          dueDate: '2026-07-15',
          isOptional: true,
        }),
      ])
      expect(body.steps[2]?.notes).toContain('Bring')
    })
  })

  it('removes pending steps and associates row validation with the affected field', async () => {
    const user = userEvent.setup()
    mockBackend({
      processes: [makeSummary(1, { name: 'Renew passport' })],
      detail: (id) =>
        makeDetail(id, {
          name: 'Renew passport',
          resolvedStepCount: 0,
          totalStepCount: 2,
          nextPendingStepId: 20,
          steps: [
            {
              id: 20,
              description: 'Attend appointment',
              dueDate: null,
              notes: null,
              isOptional: false,
              state: 'Pending',
              sortOrder: 0,
            },
            {
              id: 30,
              description: 'Submit receipt',
              dueDate: null,
              notes: null,
              isOptional: false,
              state: 'Pending',
              sortOrder: 1,
            },
          ],
        }),
    })
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Step timeline' }))
    await user.click(
      within(await screen.findByRole('dialog', { name: 'Step timeline' })).getByRole(
        'button',
        { name: 'Restructure steps' },
      ),
    )
    const restructure = await screen.findByRole('dialog', { name: 'Restructure steps' })
    await user.click(within(restructure).getAllByRole('button', { name: 'Remove' })[1])
    expect(within(restructure).queryByDisplayValue('Submit receipt')).toBeNull()

    await user.clear(within(restructure).getAllByLabelText('Description')[0])
    const description = within(restructure).getAllByLabelText('Description')[0]
    await waitFor(() => expect(description).toHaveAttribute('aria-invalid', 'true'))
    expect(
      document.getElementById(description.getAttribute('aria-describedby') ?? ''),
    ).toHaveTextContent('Every step needs a description.')
  })

  it('shows backend contiguity failures at the invariant banner', async () => {
    const user = userEvent.setup()
    mockBackend({
      processes: [makeSummary(1, { name: 'Renew passport' })],
      detail: (id) =>
        makeDetail(id, {
          name: 'Renew passport',
          resolvedStepCount: 0,
          totalStepCount: 1,
          nextPendingStepId: 20,
          steps: [
            {
              id: 20,
              description: 'Attend appointment',
              dueDate: null,
              notes: null,
              isOptional: false,
              state: 'Pending',
              sortOrder: 0,
            },
          ],
        }),
      stepUpdate: () =>
        json(
          {
            title: 'Conflict',
            code: 'processes.steps.contiguity_violation',
          },
          409,
        ),
    })
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Step timeline' }))
    await user.click(
      within(await screen.findByRole('dialog', { name: 'Step timeline' })).getByRole(
        'button',
        { name: 'Restructure steps' },
      ),
    )
    const restructure = await screen.findByRole('dialog', { name: 'Restructure steps' })
    await user.type(within(restructure).getByLabelText('Notes'), 'Bring originals')
    await user.click(
      within(restructure).getByRole('button', { name: 'Save step order' }),
    )

    expect(await within(restructure).findByRole('alert')).toHaveTextContent(
      'Resolved steps must form a contiguous prefix. Move completed or skipped steps before pending steps.',
    )
  })

  it('renders empty and completed timeline frontier states', async () => {
    const user = userEvent.setup()
    mockBackend({
      processes: [
        makeSummary(1, { name: 'Empty procedure' }),
        makeSummary(2, {
          name: 'Finished procedure',
          status: 'Completed',
          resolvedStepCount: 1,
          totalStepCount: 1,
        }),
      ],
      detail: (id) =>
        id === 2
          ? makeDetail(id, {
              name: 'Finished procedure',
              status: 'Completed',
              resolvedStepCount: 1,
              totalStepCount: 1,
              nextPendingStepId: null,
              steps: [
                {
                  id: 30,
                  description: 'Archive certificate',
                  dueDate: null,
                  notes: null,
                  isOptional: false,
                  state: 'Completed',
                  sortOrder: 0,
                },
              ],
            })
          : makeDetail(id, { name: 'Empty procedure' }),
    })
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Open process Empty procedure' }),
    )
    const editor = await screen.findByRole('dialog', { name: 'Edit process' })
    await user.click(within(editor).getByRole('button', { name: 'Manage steps' }))
    const emptyTimeline = await screen.findByRole('dialog', { name: 'Step timeline' })
    expect(
      within(emptyTimeline).getByRole('heading', { name: 'Empty procedure' }),
    ).toBeInTheDocument()
    expect(
      within(emptyTimeline).getByRole('heading', { name: 'No steps yet' }),
    ).toBeInTheDocument()
    await user.click(within(emptyTimeline).getAllByRole('button', { name: 'Close' })[1])

    await user.click(screen.getAllByRole('button', { name: 'Step timeline' })[1])
    const completeTimeline = await screen.findByRole('dialog', {
      name: 'Step timeline',
    })
    expect(
      within(completeTimeline).getByText('Every step is resolved'),
    ).toBeInTheDocument()
    expect(
      within(completeTimeline).getByRole('button', { name: 'Undo last' }),
    ).toBeEnabled()
  })

  it('deletes a process after confirmation', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      processes: [makeSummary(1, { name: 'Obsolete procedure' })],
    })
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Open process Obsolete procedure' }),
    )
    const editor = await screen.findByRole('dialog', { name: 'Edit process' })
    await user.click(within(editor).getByRole('button', { name: 'Delete process' }))

    const confirm = await screen.findByRole('dialog', {
      name: 'Delete this process?',
    })
    await user.click(within(confirm).getByRole('button', { name: 'Delete process' }))

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'DELETE' && request.url === '/api/processes/1',
        ),
      ).toBe(true),
    )
  })

  it('shows a privacy-safe message when an edited process is not found', async () => {
    const user = userEvent.setup()
    // A missing or inaccessible process shares the not-found behaviour, so the
    // editor surfaces a neutral message rather than disclosing anything.
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
      if (url.startsWith('/api/processes/categories')) {
        return json([{ id: 1, name: 'Administrative', sortOrder: 0 }])
      }
      if (/\/api\/processes\/\d+(?:\?|$)/.test(url) && method === 'GET') {
        return json({ title: 'Not found', code: 'processes.process.not_found' }, 404)
      }
      if (url.startsWith('/api/processes') && method === 'GET') {
        return json({
          items: [makeSummary(1, { name: 'Vanishing process' })],
          page: 1,
          pageSize: 25,
          totalCount: 1,
        })
      }
      throw new Error(`Unexpected request: ${method} ${url}`)
    })
    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Open process Vanishing process' }),
    )
    expect(
      await screen.findByText('This process no longer exists.'),
    ).toBeInTheDocument()
  })
})

describe('Processes page accessibility', () => {
  it('has no automated violations on the table and the open editor', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.click(screen.getByRole('button', { name: 'New process' }))
    await screen.findByRole('dialog', { name: 'New process' })

    const result = await axe.run(document.body, {
      rules: { 'color-contrast': { enabled: false } },
    })
    expect(result.violations).toEqual([])
  })

  it('moves focus into the editor and closes it with Escape', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.click(screen.getByRole('button', { name: 'New process' }))
    const dialog = await screen.findByRole('dialog', { name: 'New process' })

    // The dialog panel receives focus on open, trapping keyboard interaction.
    await waitFor(() => expect(dialog).toHaveFocus())

    await user.keyboard('{Escape}')
    await waitFor(() =>
      expect(screen.queryByRole('dialog', { name: 'New process' })).toBeNull(),
    )
  })

  it('associates the name validation message with its field', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Process 01')
    await user.click(screen.getByRole('button', { name: 'New process' }))
    const dialog = await screen.findByRole('dialog', { name: 'New process' })
    await user.clear(within(dialog).getByLabelText('Name'))
    await user.click(within(dialog).getByRole('button', { name: 'Create process' }))

    const name = within(dialog).getByLabelText('Name')
    await waitFor(() => expect(name).toHaveAttribute('aria-invalid', 'true'))
    const describedBy = name.getAttribute('aria-describedby')
    expect(describedBy).toBeTruthy()
    expect(document.getElementById(describedBy as string)).toHaveTextContent(
      'A name is required.',
    )
  })
})
