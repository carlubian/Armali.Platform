import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  Activity,
  AxisNode,
  ProgramNode,
  Project,
  ProjectRisk,
  ProjectTreeItem,
} from '@/app/api/projects'

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['User'],
  avatarUrl: null as string | null,
}

const programs: ProgramNode[] = [{ id: 1, code: 'PRGM', name: 'Platform' }]
const axes: AxisNode[] = [{ id: 11, code: 'WEBB', name: 'Web work', programId: 1 }]
const projectItem: ProjectTreeItem = {
  id: 10,
  kind: 'Project',
  number: 1,
  identifier: 'PRGMWEBB-000001 Frontend tree',
  name: 'Frontend tree',
  status: 'Active',
  visibility: 'Public',
  riskSummary: { low: 1, medium: 1, high: 1 },
}
const activityItem: ProjectTreeItem = {
  id: 20,
  kind: 'Activity',
  number: 2,
  identifier: 'PRGMWEBB-000002 Clean backlog',
  name: 'Clean backlog',
  status: 'Planning',
  visibility: 'Private',
  riskSummary: null,
}

function projectDetail(): Project {
  return {
    id: 10,
    number: 1,
    identifier: projectItem.identifier,
    name: projectItem.name,
    status: 'Active',
    visibility: 'Public',
    axisId: 11,
    riskSummary: { low: 1, medium: 1, high: 1 },
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-06-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
  }
}

function activityDetail(): Activity {
  return {
    id: 20,
    number: 2,
    identifier: activityItem.identifier,
    name: activityItem.name,
    status: 'Planning',
    visibility: 'Private',
    axisId: 11,
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-06-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
  }
}

const risks: ProjectRisk[] = [
  {
    id: 100,
    description: 'Supplier delay',
    probability: 3,
    impact: 4,
    mitigation: 5,
    score: 60,
    band: 'Medium',
  },
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

function mockBackend() {
  const requests: Array<{ method: string; url: string; body?: unknown }> = []
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
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
    if (url.startsWith('/api/launcher/attention')) {
      return json({ modules: [{ module: 'projects', requiresAttention: false }] })
    }
    if (url === '/api/projects/tree/programs' && method === 'GET') return json(programs)
    if (url === '/api/projects/tree/programs/1/axes' && method === 'GET') return json(axes)
    if (url === '/api/projects/tree/axes/11/items' && method === 'GET') {
      return json([projectItem, activityItem])
    }
    if (url === '/api/projects/programs' && method === 'GET') return json(programs)
    if (url === '/api/projects/axes' && method === 'GET') return json(axes)
    if (url === '/api/projects/projects/10' && method === 'GET') return json(projectDetail())
    if (url === '/api/projects/activities/20' && method === 'GET') return json(activityDetail())
    if (url === '/api/projects/projects/10/attachments' && method === 'GET') {
      return json([
        {
          id: 'att-1',
          fileName: 'result.pdf',
          contentType: 'application/pdf',
          size: 2048,
          createdById: 7,
          createdAt: '2026-06-01T00:00:00Z',
        },
      ])
    }
    if (url === '/api/projects/projects/10/risks' && method === 'GET') return json(risks)
    if (url === '/api/projects/projects/10/risks' && method === 'POST') {
      requests.push({ method, url, body })
      return json({
        id: 101,
        description: (body as { description: string }).description,
        probability: (body as { probability: number }).probability,
        impact: (body as { impact: number }).impact,
        mitigation: (body as { mitigation: number }).mitigation,
        score: 100,
        band: 'High',
      })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })
  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/projects')
})

afterEach(() => vi.restoreAllMocks())

describe('Projects page', () => {
  it('lazily expands programmes and axes and renders unified identifiers', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await user.click(buttonContaining(await screen.findByText('PRGM')))
    await user.click(buttonContaining(await screen.findByText('WEBB')))

    expect(await screen.findByText(projectItem.identifier)).toBeInTheDocument()
    expect(screen.getByText(activityItem.identifier)).toBeInTheDocument()
    expect(screen.getByLabelText('Risk summary')).toHaveTextContent('Low 1')
    expect(screen.getByLabelText('Risk summary')).toHaveTextContent('Medium 1')
    expect(screen.getByLabelText('Risk summary')).toHaveTextContent('High 1')
  })

  it('opens an URL-backed create dialog without collapsing the expanded tree', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await user.click(buttonContaining(await screen.findByText('PRGM')))
    await user.click(buttonContaining(await screen.findByText('WEBB')))
    await screen.findByText(projectItem.identifier)
    await user.click(screen.getByRole('button', { name: 'New project' }))

    expect(await screen.findByRole('dialog', { name: 'New project' })).toBeInTheDocument()
    expect(window.location.search).toContain('newProject=axis-11')
    expect(screen.getByText(projectItem.identifier)).toBeInTheDocument()
  })

  it('computes the live risk score and submits risk CRUD requests', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    window.history.replaceState({}, '', '/projects?projectId=10&risks=true')
    render(<App />)

    const riskDialog = await screen.findByRole('dialog', { name: 'Project risks' })
    await user.click(within(riskDialog).getByRole('button', { name: 'New risk' }))

    const editor = await screen.findByRole('dialog', { name: 'New risk' })
    await user.type(
      within(editor).getByRole('textbox', { name: 'Description' }),
      'Budget shock',
    )
    await user.selectOptions(within(editor).getByRole('combobox', { name: 'Probability' }), '5')
    await user.selectOptions(within(editor).getByRole('combobox', { name: 'Impact' }), '5')
    await user.selectOptions(within(editor).getByRole('combobox', { name: 'Mitigation' }), '4')

    expect(within(editor).getByText('100 · High')).toBeInTheDocument()
    await user.click(within(editor).getByRole('button', { name: 'Save changes' }))

    await waitFor(() =>
      expect(
        requests.find((request) => request.url === '/api/projects/projects/10/risks')
          ?.body,
      ).toEqual({
        description: 'Budget shock',
        probability: 5,
        impact: 5,
        mitigation: 4,
      }),
    )
  })
})

function buttonContaining(element: HTMLElement): HTMLButtonElement {
  const button = element.closest('button')
  if (button == null) throw new Error('Expected element to be inside a button')
  return button
}
