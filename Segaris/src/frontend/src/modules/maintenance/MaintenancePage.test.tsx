import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { MaintenanceTask, MaintenanceTaskSummary } from '@/app/api/maintenance'
import type { AssetSummary } from '@/app/api/assets'

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

function makeTask(
  id: number,
  overrides: Partial<MaintenanceTaskSummary> = {},
): MaintenanceTaskSummary {
  return {
    id,
    title: `Task ${id.toString().padStart(2, '0')}`,
    maintenanceTypeId: 1,
    maintenanceTypeName: 'Repair',
    status: 'Pending',
    priority: 'Medium',
    assetId: null,
    assetName: null,
    dueDate: '2026-07-01',
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeTaskDetail(
  id: number,
  overrides: Partial<MaintenanceTask> = {},
): MaintenanceTask {
  return {
    ...makeTask(id),
    completedDate: null,
    notes: 'Replace the filter.',
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

function makeAsset(id: number, overrides: Partial<AssetSummary> = {}): AssetSummary {
  return {
    id,
    name: `Asset ${id.toString().padStart(2, '0')}`,
    code: `A-${id}`,
    categoryId: 1,
    categoryName: 'Appliances',
    locationId: 1,
    locationName: 'Storage',
    status: 'Active',
    expectedEndOfLifeDate: null,
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function mockBackend(
  options: {
    tasks?: MaintenanceTaskSummary[]
    assets?: AssetSummary[]
  } = {},
) {
  const tasks = options.tasks ?? [makeTask(1)]
  const assets = options.assets ?? [
    makeAsset(1, { name: 'Public boiler', visibility: 'Public' }),
    makeAsset(2, { name: 'Private toolbox', visibility: 'Private' }),
  ]
  const requests: Array<{ method: string; url: string; body?: unknown }> = []

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
    if (url.startsWith('/api/launcher/attention')) {
      return json({ modules: [{ module: 'maintenance', requiresAttention: true }] })
    }
    if (url.startsWith('/api/maintenance/types')) {
      return json([
        { id: 1, name: 'Repair', sortOrder: 1 },
        { id: 2, name: 'Inspection', sortOrder: 2 },
      ])
    }
    if (url.startsWith('/api/assets/categories')) {
      return json([{ id: 1, name: 'Appliances', sortOrder: 0 }])
    }
    if (url.startsWith('/api/assets/locations')) {
      return json([{ id: 1, name: 'Storage', sortOrder: 0 }])
    }
    if (url.startsWith('/api/assets/items') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const visibility = parsed.searchParams.get('visibility')
      const filtered =
        visibility == null
          ? assets
          : assets.filter((asset) => asset.visibility === visibility)
      return json({
        items: filtered,
        page: 1,
        pageSize: Number(parsed.searchParams.get('pageSize') ?? '25'),
        totalCount: filtered.length,
      })
    }
    if (url.startsWith('/api/maintenance/tasks/') && url.endsWith('/attachments')) {
      requests.push({ method, url })
      if (method === 'GET') return json([])
      if (method === 'POST') {
        return json(
          {
            id: 'attachment-1',
            fileName: 'quote.pdf',
            contentType: 'application/pdf',
            size: 1024,
            createdById: 7,
            createdAt: '2026-01-01T00:00:00Z',
          },
          201,
        )
      }
    }
    if (url.startsWith('/api/maintenance/tasks/') && method === 'GET') {
      requests.push({ method, url })
      const id = Number(url.match(/\/api\/maintenance\/tasks\/(\d+)/)?.[1] ?? '1')
      return json(makeTaskDetail(id))
    }
    if (url === '/api/maintenance/tasks' && method === 'POST') {
      requests.push({
        method,
        url,
        body:
          typeof init?.body === 'string'
            ? (JSON.parse(init.body) as unknown)
            : undefined,
      })
      return json(
        makeTaskDetail(99, {
          id: 99,
          title: (JSON.parse(init?.body as string) as { title: string }).title,
        }),
        201,
      )
    }
    if (url.startsWith('/api/maintenance/tasks') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const search = parsed.searchParams.get('search')?.toLowerCase() ?? ''
      const filtered =
        search === ''
          ? tasks
          : tasks.filter((task) => task.title.toLowerCase().includes(search))
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
  window.history.replaceState({}, '', '/maintenance')
})

afterEach(() => vi.restoreAllMocks())

describe('Maintenance page', () => {
  it('renders tasks with formatted dates and the neutral asset placeholder', async () => {
    mockBackend({
      tasks: [
        makeTask(1, {
          title: 'Replace boiler filter',
          assetId: 5,
          assetName: null,
          priority: 'High',
        }),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Replace boiler filter')).toBeInTheDocument()
    expect(screen.getByText('Repair')).toBeInTheDocument()
    expect(screen.getByText('High')).toBeInTheDocument()
    expect(screen.getByText('01 Jul 2026')).toBeInTheDocument()
    expect(screen.getByText('Linked asset unavailable')).toBeInTheDocument()
  })

  it('serializes search and sort into the task list request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Task 01')
    await user.type(screen.getByLabelText('Search'), 'Task 01')
    await user.click(screen.getByRole('button', { name: 'Sort by Priority' }))

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('search=Task+01') &&
            request.url.includes('sort=priority'),
        ),
      ).toBe(true),
    )
  })

  it('opens the create dialog without losing table state', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Task 01')
    await user.type(screen.getByLabelText('Search'), 'Task 01')
    await user.click(screen.getByRole('button', { name: 'New task' }))

    expect(
      await screen.findByRole('dialog', { name: 'New maintenance task' }),
    ).toBeInTheDocument()
    expect(window.location.search).toContain('search=Task+01')
    expect(window.location.search).toContain('newTask=true')
  })

  it('validates required task fields before submission', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Task 01')
    await user.click(screen.getByRole('button', { name: 'New task' }))
    const dialog = await screen.findByRole('dialog', { name: 'New maintenance task' })
    await user.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await within(dialog).findByText('A title is required.')).toBeInTheDocument()
  })

  it('constrains the asset selector to public assets for public tasks', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Task 01')
    await user.click(screen.getByRole('button', { name: 'New task' }))
    const editor = await screen.findByRole('dialog', { name: 'New maintenance task' })

    // A public task forces the selector to list only public assets.
    await user.click(within(editor).getByRole('button', { name: 'Browse assets' }))
    const selector = await screen.findByRole('dialog', { name: /Select an asset/ })
    await within(selector).findByText('Public boiler')
    expect(within(selector).queryByText('Private toolbox')).not.toBeInTheDocument()
    expect(
      requests.some(
        (request) =>
          request.url.includes('/api/assets/items') &&
          request.url.includes('visibility=Public'),
      ),
    ).toBe(true)

    // Switching to a private task lets the selector list any accessible asset.
    await user.click(within(selector).getByRole('button', { name: 'Cancel' }))
    await user.click(within(editor).getByRole('radio', { name: 'Private' }))
    await user.click(within(editor).getByRole('button', { name: 'Browse assets' }))
    const privateSelector = await screen.findByRole('dialog', {
      name: /Select an asset/,
    })
    expect(await within(privateSelector).findByText('Private toolbox')).toBeVisible()
  })

  it('uploads staged attachments after creating a task', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Task 01')
    await user.click(screen.getByRole('button', { name: 'New task' }))
    const dialog = await screen.findByRole('dialog', { name: 'New maintenance task' })
    await user.type(within(dialog).getByLabelText('Title'), 'Replace air filter')
    await user.upload(
      within(dialog).getByLabelText('Add files'),
      new File(['quote'], 'quote.pdf', { type: 'application/pdf' }),
    )
    await user.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(
      await screen.findByRole('dialog', { name: 'Upload attachments' }),
    ).toBeInTheDocument()
    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/maintenance/tasks/99/attachments',
        ),
      ).toBe(true),
    )
  })
})
