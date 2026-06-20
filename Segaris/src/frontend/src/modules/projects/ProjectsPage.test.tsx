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

function mockBackend({ items = [projectItem, activityItem] } = {}) {
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
    if (url === '/api/projects/tree/programs/1/axes' && method === 'GET')
      return json(axes)
    if (url === '/api/projects/tree/axes/11/items' && method === 'GET') {
      return json(items)
    }
    if (url === '/api/projects/programs' && method === 'GET') return json(programs)
    if (url === '/api/projects/axes' && method === 'GET') return json(axes)
    if (url === '/api/projects/projects/10' && method === 'GET')
      return json(projectDetail())
    if (url === '/api/projects/projects' && method === 'POST') {
      requests.push({ method, url, body })
      return json({
        ...projectDetail(),
        id: 12,
        identifier: 'PRGMWEBB-000003 Created project',
        name: (body as { name: string }).name,
        status: (body as { status: Project['status'] }).status,
        visibility: (body as { visibility: Project['visibility'] }).visibility,
        axisId: (body as { axisId: number }).axisId,
      })
    }
    if (url === '/api/projects/activities/20' && method === 'GET')
      return json(activityDetail())
    if (url === '/api/projects/activities' && method === 'POST') {
      requests.push({ method, url, body })
      return json({
        ...activityDetail(),
        id: 21,
        identifier: 'PRGMWEBB-000003 Created activity',
        name: (body as { name: string }).name,
        status: (body as { status: Activity['status'] }).status,
        visibility: (body as { visibility: Activity['visibility'] }).visibility,
        axisId: (body as { axisId: number }).axisId,
      })
    }
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
    if (url === '/api/projects/projects/10/risks' && method === 'GET')
      return json(risks)
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
  it('lazily expands programmes and axes and renders compact tree rows without risk or badge clusters', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await user.click(buttonContaining(await screen.findByText('PRGM')))
    await user.click(buttonContaining((await screen.findAllByText('WEBB'))[0]))

    const tree = screen.getByRole('list', { name: 'Projects hierarchy' })
    const projectRow = await within(tree).findByRole('button', {
      name: `Select project ${projectItem.identifier}`,
    })
    const activityRow = within(tree).getByRole('button', {
      name: `Select activity ${activityItem.identifier}`,
    })

    expect(within(projectRow).getByText(projectItem.identifier)).toHaveClass(
      'seg-projects-tree__code-pill',
    )
    expect(within(projectRow).getByText(projectItem.name)).toHaveClass(
      'seg-projects-tree__leaf-name',
    )
    expect(projectRow.querySelectorAll('.seg-projects-tree__type-icon')).toHaveLength(1)
    expect(activityRow.querySelectorAll('.seg-projects-tree__type-icon')).toHaveLength(
      1,
    )
    expect(within(tree).queryByLabelText('Risk summary')).not.toBeInTheDocument()
    expect(within(tree).queryByText('Public')).not.toBeInTheDocument()
    expect(within(tree).queryByText('Private')).not.toBeInTheDocument()
  })

  it('maps every project status to the fixed tree icon colour class', async () => {
    const user = userEvent.setup()
    const statusItems: ProjectTreeItem[] = [
      {
        ...projectItem,
        id: 10,
        identifier: 'PRGMWEBB-000001 Planning',
        status: 'Planning',
      },
      {
        ...projectItem,
        id: 11,
        identifier: 'PRGMWEBB-000002 Active',
        status: 'Active',
      },
      {
        ...projectItem,
        id: 12,
        identifier: 'PRGMWEBB-000003 Completed',
        status: 'Completed',
      },
      {
        ...projectItem,
        id: 13,
        identifier: 'PRGMWEBB-000004 OnHold',
        status: 'OnHold',
      },
      {
        ...projectItem,
        id: 14,
        identifier: 'PRGMWEBB-000005 Cancelled',
        status: 'Cancelled',
      },
    ]
    mockBackend({ items: statusItems })
    render(<App />)

    await user.click(buttonContaining(await screen.findByText('PRGM')))
    await user.click(buttonContaining((await screen.findAllByText('WEBB'))[0]))

    const expectedClasses = {
      Planning: 'seg-projects-tree__type-icon--planning',
      Active: 'seg-projects-tree__type-icon--active',
      Completed: 'seg-projects-tree__type-icon--completed',
      OnHold: 'seg-projects-tree__type-icon--on-hold',
      Cancelled: 'seg-projects-tree__type-icon--cancelled',
    } as const

    for (const item of statusItems) {
      const row = await screen.findByRole('button', {
        name: `Select project ${item.identifier}`,
      })
      expect(row.querySelector('.seg-projects-tree__type-icon')).toHaveClass(
        expectedClasses[item.status],
      )
    }
  })

  it('keeps tree selection keyboard accessible while risk and edit actions stay in details', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await user.click(buttonContaining(await screen.findByText('PRGM')))
    await user.click(buttonContaining((await screen.findAllByText('WEBB'))[0]))

    const projectRow = await screen.findByRole('button', {
      name: `Select project ${projectItem.identifier}`,
    })
    projectRow.focus()
    expect(projectRow).toHaveFocus()

    await user.keyboard('[Enter]')

    expect(projectRow).toHaveAttribute('aria-current', 'true')
    expect(
      await screen.findByRole('heading', { name: projectItem.name }),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Risk summary')).toHaveTextContent('Low 1')
    expect(screen.getAllByRole('button', { name: 'Open risks' })).toHaveLength(2)
    expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()
    expect(
      within(screen.getByRole('list', { name: 'Projects hierarchy' })).queryByRole(
        'button',
        {
          name: 'Open risks',
        },
      ),
    ).not.toBeInTheDocument()
  })

  it('renders stable workspace containers for desktop and narrow responsive layouts', async () => {
    mockBackend()
    window.innerWidth = 540
    window.dispatchEvent(new Event('resize'))
    render(<App />)

    const workspace = await screen
      .findByText('Project tree')
      .then((heading) => heading.closest('.seg-projects__workspace'))
    expect(workspace).toHaveClass('seg-projects__workspace')
    expect(workspace?.querySelector('.seg-projects__tree-card')).toBeInTheDocument()
    expect(workspace?.querySelector('.seg-projects-detail')).toBeInTheDocument()
  })

  it('selects programs, axes, projects, and activities into the detail pane without opening dialogs', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    expect(await screen.findByText('Choose a tree node')).toBeInTheDocument()

    await user.click(buttonContaining(await screen.findByText('PRGM')))
    expect(await screen.findByRole('heading', { name: 'Platform' })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(window.location.search).toContain('selected=program-1')

    await user.click(buttonContaining((await screen.findAllByText('WEBB'))[0]))
    expect(await screen.findByRole('heading', { name: 'Web work' })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(window.location.search).toContain('selected=axis-11')

    await user.click(
      await screen.findByRole('button', {
        name: `Select project ${projectItem.identifier}`,
      }),
    )
    expect(
      await screen.findByRole('heading', { name: projectItem.name }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('dialog', { name: 'Edit project' }),
    ).not.toBeInTheDocument()
    expect(screen.getByText('Context and audit')).toBeInTheDocument()
    expect(await screen.findByText('result.pdf')).toBeInTheDocument()
    expect(window.location.search).toContain('selected=project-10')

    await user.click(screen.getAllByRole('button', { name: 'Open risks' })[0])
    expect(
      await screen.findByRole('dialog', { name: 'Project risks' }),
    ).toBeInTheDocument()
    expect(window.location.search).toContain('selected=project-10')
    await user.click(
      within(screen.getByRole('dialog', { name: 'Project risks' })).getAllByRole(
        'button',
        {
          name: 'Close',
        },
      )[0],
    )
    expect(window.location.search).toContain('selected=project-10')

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    const editDialog = await screen.findByRole('dialog', { name: 'Edit project' })
    expect(within(editDialog).queryByText('Result files')).not.toBeInTheDocument()
    expect(window.location.search).toContain('selected=project-10')
    await user.click(within(editDialog).getByRole('button', { name: 'Cancel' }))
    expect(window.location.search).toContain('selected=project-10')

    await user.click(
      screen.getByRole('button', {
        name: `Select activity ${activityItem.identifier}`,
      }),
    )
    expect(
      await screen.findByRole('heading', { name: activityItem.name }),
    ).toBeInTheDocument()
    expect(screen.getByText('Lightweight unit of work')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Open risks' })).not.toBeInTheDocument()
    expect(window.location.search).toContain('selected=activity-20')
  })

  it('opens one URL-backed create dialog and switches between item types', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await user.click(buttonContaining(await screen.findByText('PRGM')))
    await user.click(buttonContaining((await screen.findAllByText('WEBB'))[0]))
    await screen.findAllByText(projectItem.identifier)
    expect(
      screen.queryByRole('button', { name: 'New project' }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'New activity' }),
    ).not.toBeInTheDocument()

    await user.click(screen.getAllByRole('button', { name: 'New item' })[0])
    expect(
      await screen.findByRole('dialog', { name: 'New project' }),
    ).toBeInTheDocument()
    expect(window.location.search).toContain('newItem=axis-11')
    expect(screen.getAllByText(projectItem.identifier).length).toBeGreaterThan(0)

    await user.click(screen.getByRole('radio', { name: 'Activity' }))
    const activityDialog = await screen.findByRole('dialog', { name: 'New activity' })
    await user.type(
      within(activityDialog).getByRole('textbox', { name: 'Name' }),
      'Created activity',
    )
    await user.click(within(activityDialog).getByRole('button', { name: 'Create' }))

    await waitFor(() =>
      expect(
        requests.find((request) => request.url === '/api/projects/activities')?.body,
      ).toEqual({
        axisId: 11,
        name: 'Created activity',
        status: 'Planning',
        visibility: 'Public',
      }),
    )
  })

  it('computes the live risk score and submits risk CRUD requests', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    window.history.replaceState(
      {},
      '',
      '/projects?selected=project-10&riskProjectId=10',
    )
    render(<App />)

    const riskDialog = await screen.findByRole('dialog', { name: 'Project risks' })
    await user.click(within(riskDialog).getByRole('button', { name: 'New risk' }))

    const editor = await screen.findByRole('dialog', { name: 'New risk' })
    await user.type(
      within(editor).getByRole('textbox', { name: 'Description' }),
      'Budget shock',
    )
    await user.selectOptions(
      within(editor).getByRole('combobox', { name: 'Probability' }),
      '5',
    )
    await user.selectOptions(
      within(editor).getByRole('combobox', { name: 'Impact' }),
      '5',
    )
    await user.selectOptions(
      within(editor).getByRole('combobox', { name: 'Mitigation' }),
      '4',
    )

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
