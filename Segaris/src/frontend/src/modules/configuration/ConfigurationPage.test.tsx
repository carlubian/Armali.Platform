import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import axe from 'axe-core'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { CatalogDeletionImpact } from '@/app/api/catalogs'

interface Row {
  id: number
  name: string
  code?: string
  colorValue?: string
  sortOrder: number
  programId?: number
}

const adminSession = {
  userId: 1,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['Admin'],
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

const defaultImpact: CatalogDeletionImpact = {
  isReferenced: false,
  canDeleteDirectly: true,
  canClearReferences: false,
  requiresExchangeRate: false,
  hasReplacementCandidates: true,
}

const defaultStructureImpact = {
  childCount: 0,
  hasCompatibleTarget: true,
}

interface BackendOptions {
  roles?: string[]
  suppliers?: Row[]
  costCenters?: Row[]
  currencies?: Row[]
  categories?: Row[]
  opexCategories?: Row[]
  travelTripTypes?: Row[]
  travelExpenseCategories?: Row[]
  clothingCategories?: Row[]
  clothingColors?: Row[]
  assetCategories?: Row[]
  assetLocations?: Row[]
  maintenanceTypes?: Row[]
  processCategories?: Row[]
  personCategories?: Row[]
  usernamePlatforms?: Row[]
  programs?: Row[]
  axes?: Row[]
  /** Impact override keyed by `${catalog}:${id}`. */
  impacts?: Record<string, Partial<CatalogDeletionImpact>>
  /** Projects structure impact override keyed by `programs:${id}` or `axes:${id}`. */
  structureImpacts?: Record<string, Partial<typeof defaultStructureImpact>>
  /** Force a create response (e.g. a duplicate-name conflict). */
  createResponse?: () => Response
  /** Force a Projects structure create response (e.g. a duplicate-code conflict). */
  structureCreateResponse?: () => Response
}

const catalogPaths: Record<string, string> = {
  'configuration/suppliers': 'suppliers',
  'configuration/cost-centers': 'costCenters',
  'configuration/currencies': 'currencies',
  'capex/categories': 'categories',
  'opex/categories': 'opexCategories',
  'travel/trip-types': 'travelTripTypes',
  'travel/expense-categories': 'travelExpenseCategories',
  'clothes/categories': 'clothingCategories',
  'clothes/colors': 'clothingColors',
  'assets/categories': 'assetCategories',
  'assets/locations': 'assetLocations',
  'maintenance/types': 'maintenanceTypes',
  'processes/categories': 'processCategories',
  'people/categories': 'personCategories',
  'people/platforms': 'usernamePlatforms',
}

function mockBackend(options: BackendOptions = {}) {
  const session = { ...adminSession, roles: options.roles ?? ['Admin'] }
  const data: Record<string, Row[]> = {
    suppliers: options.suppliers ?? [
      { id: 1, name: 'Amazon', sortOrder: 1 },
      { id: 2, name: 'Ikea', sortOrder: 2 },
    ],
    costCenters: options.costCenters ?? [{ id: 1, name: 'Household', sortOrder: 1 }],
    currencies: options.currencies ?? [
      { id: 1, code: 'EUR', name: 'Euro', sortOrder: 1 },
      { id: 2, code: 'USD', name: 'US Dollar', sortOrder: 2 },
    ],
    categories: options.categories ?? [{ id: 1, name: 'Other', sortOrder: 1 }],
    opexCategories: options.opexCategories ?? [
      { id: 1, name: 'Subscriptions', sortOrder: 1 },
    ],
    travelTripTypes: options.travelTripTypes ?? [
      { id: 1, name: 'Regional', sortOrder: 1 },
    ],
    travelExpenseCategories: options.travelExpenseCategories ?? [
      { id: 1, name: 'Transport', sortOrder: 1 },
    ],
    clothingCategories: options.clothingCategories ?? [
      { id: 1, name: 'Tops', sortOrder: 1 },
    ],
    clothingColors: options.clothingColors ?? [
      { id: 1, name: 'Black', colorValue: '#111111', sortOrder: 1 },
      { id: 2, name: 'White', colorValue: '#FAFAFA', sortOrder: 2 },
    ],
    assetCategories: options.assetCategories ?? [
      { id: 1, name: 'Furniture', sortOrder: 1 },
      { id: 2, name: 'Tools', sortOrder: 2 },
    ],
    assetLocations: options.assetLocations ?? [
      { id: 1, name: 'Living room', sortOrder: 1 },
      { id: 2, name: 'Garage', sortOrder: 2 },
    ],
    maintenanceTypes: options.maintenanceTypes ?? [
      { id: 1, name: 'Repair', sortOrder: 1 },
      { id: 2, name: 'Inspection', sortOrder: 2 },
    ],
    processCategories: options.processCategories ?? [
      { id: 1, name: 'Administrative', sortOrder: 1 },
      { id: 2, name: 'Legal', sortOrder: 2 },
    ],
    personCategories: options.personCategories ?? [
      { id: 1, name: 'Family', sortOrder: 1 },
      { id: 2, name: 'Friend', sortOrder: 2 },
    ],
    usernamePlatforms: options.usernamePlatforms ?? [
      { id: 1, name: 'Email', sortOrder: 1 },
      { id: 2, name: 'Discord', sortOrder: 2 },
    ],
    programs: options.programs ?? [
      { id: 1, code: 'HOME', name: 'Household', sortOrder: 1 },
      { id: 2, code: 'WORK', name: 'Work', sortOrder: 2 },
    ],
    axes: options.axes ?? [
      { id: 10, code: 'PLAN', name: 'Planning', programId: 1, sortOrder: 1 },
      { id: 11, code: 'OPSX', name: 'Operations', programId: 2, sortOrder: 2 },
    ],
  }
  const calls: Array<{ method: string; url: string; body?: unknown }> = []
  let nextId = 1000

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
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
      if (url.startsWith('/api/launcher/attention')) return json({ modules: [] })

      const structureMatch = url.match(
        /^\/api\/projects\/(programs|axes)(?:\/(\d+)(\/deletion-impact|\/reassign-and-delete)?)?(?:\?.*)?$/,
      )
      if (structureMatch) {
        const key = structureMatch[1]
        const id = structureMatch[2] != null ? Number(structureMatch[2]) : null
        const suffix = structureMatch[3]
        const rows = data[key]

        if (id == null && method === 'GET') return json(rows)
        if (id == null && method === 'POST') {
          calls.push({ method, url, body })
          if (options.structureCreateResponse) return options.structureCreateResponse()
          const input = body as { name: string; code: string; programId?: number }
          const created: Row = {
            id: nextId++,
            name: input.name,
            code: input.code,
            programId: input.programId,
            sortOrder: rows.length + 1,
          }
          rows.push(created)
          return json(created, 201)
        }
        if (id != null && suffix === '/deletion-impact' && method === 'GET') {
          return json({
            ...defaultStructureImpact,
            ...(options.structureImpacts?.[`${key}:${id}`] ?? {}),
          })
        }
        if (id != null && suffix === '/reassign-and-delete' && method === 'POST') {
          calls.push({ method, url, body })
          return new Response(null, { status: 204 })
        }
        if (id != null && suffix == null && method === 'PUT') {
          calls.push({ method, url, body })
          const input = body as { name: string; code: string; programId?: number }
          const target = rows.find((row) => row.id === id)
          if (target != null) {
            target.name = input.name
            target.code = input.code
            if (input.programId != null) target.programId = input.programId
          }
          return json(target ?? {})
        }
        if (id != null && suffix == null && method === 'DELETE') {
          calls.push({ method, url })
          return new Response(null, { status: 204 })
        }
      }

      const match = url.match(
        /^\/api\/(configuration\/(?:suppliers|cost-centers|currencies)|capex\/categories|opex\/categories|travel\/(?:trip-types|expense-categories)|clothes\/(?:categories|colors)|assets\/(?:categories|locations)|maintenance\/types|processes\/categories|people\/(?:categories|platforms))(?:\/(\d+)(\/move|\/deletion-impact|\/replace-and-delete)?)?(?:\?.*)?$/,
      )
      if (match) {
        const key = catalogPaths[match[1]]
        const id = match[2] != null ? Number(match[2]) : null
        const suffix = match[3]
        const rows = data[key]

        if (id == null && method === 'GET') return json(rows)
        if (id == null && method === 'POST') {
          calls.push({ method, url, body })
          if (options.createResponse) return options.createResponse()
          const input = body as { name: string; code?: string; colorValue?: string }
          const created: Row = {
            id: nextId++,
            name: input.name,
            code: input.code,
            colorValue: input.colorValue,
            sortOrder: rows.length + 1,
          }
          rows.push(created)
          return json(created, 201)
        }
        if (id != null && suffix === '/deletion-impact' && method === 'GET') {
          return json({
            ...defaultImpact,
            ...(options.impacts?.[`${key}:${id}`] ?? {}),
          })
        }
        if (id != null && suffix === '/move' && method === 'POST') {
          calls.push({ method, url, body })
          return new Response(null, { status: 204 })
        }
        if (id != null && suffix === '/replace-and-delete' && method === 'POST') {
          calls.push({ method, url, body })
          return new Response(null, { status: 204 })
        }
        if (id != null && suffix == null && method === 'PUT') {
          calls.push({ method, url, body })
          const input = body as { name: string; code?: string; colorValue?: string }
          const target = rows.find((row) => row.id === id)
          if (target != null) {
            target.name = input.name
            if (input.code != null) target.code = input.code
            if (input.colorValue != null) target.colorValue = input.colorValue
          }
          return json(target ?? {})
        }
        if (id != null && suffix == null && method === 'DELETE') {
          calls.push({ method, url })
          return new Response(null, { status: 204 })
        }
      }

      throw new Error(`Unexpected request: ${method} ${url}`)
    })

  return { fetchMock, calls, data }
}

function renderAt(path: string) {
  window.history.replaceState({}, '', path)
  return render(<App />)
}

const suppliersHome = '/configuration/global?catalog=suppliers'

beforeEach(() => {
  appQueryClient.clear()
})
afterEach(() => vi.restoreAllMocks())

describe('Configuration launcher and routing', () => {
  it('shows the Configuration launcher card to administrators', async () => {
    mockBackend()
    renderAt('/')
    await screen.findByRole('heading', { name: 'Choose a module' })
    expect(screen.getByText('Configuration')).toBeInTheDocument()
  })

  it('hides the Configuration launcher card from normal users', async () => {
    mockBackend({ roles: ['User'] })
    renderAt('/')
    await screen.findByRole('heading', { name: 'Choose a module' })
    expect(screen.queryByText('Configuration')).not.toBeInTheDocument()
  })

  it('denies access to non-administrators at the route', async () => {
    mockBackend({ roles: ['User'] })
    renderAt(suppliersHome)
    expect(
      await screen.findByText('This area is for administrators'),
    ).toBeInTheDocument()
  })

  it('redirects a bare /configuration to Global Suppliers', async () => {
    mockBackend()
    renderAt('/configuration')
    expect(await screen.findByText('Amazon')).toBeInTheDocument()
    expect(window.location.search).toContain('catalog=suppliers')
  })

  it('falls back to Global Suppliers for an unknown catalog slug', async () => {
    mockBackend()
    renderAt('/configuration/global?catalog=unknown')
    expect(await screen.findByText('Amazon')).toBeInTheDocument()
    expect(window.location.search).toContain('catalog=suppliers')
  })

  it('falls back to Global Suppliers for an unknown section', async () => {
    mockBackend()
    renderAt('/configuration/nonsense')
    expect(await screen.findByText('Amazon')).toBeInTheDocument()
  })
})

describe('Configuration navigation and states', () => {
  it('switches Global tabs and shows the currency code column', async () => {
    mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('tab', { name: 'Currencies' }))

    expect(await screen.findByText('Euro')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Code' })).toBeInTheDocument()
    expect(screen.getByText('EUR')).toBeInTheDocument()
  })

  it('shows the Capex categories section', async () => {
    mockBackend()
    renderAt('/configuration/capex')
    expect(await screen.findByText('Other')).toBeInTheDocument()
  })

  it('shows the Opex categories section', async () => {
    mockBackend()
    renderAt('/configuration/opex')
    expect(
      await screen.findByRole('heading', { name: 'Opex categories' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Subscriptions')).toBeInTheDocument()
  })

  it('shows the Travel catalog tabs', async () => {
    mockBackend()
    renderAt('/configuration/travel?catalog=trip-types')
    expect(
      await screen.findByRole('heading', { name: 'Travel trip types' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Regional')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: 'Expense categories' }))

    expect(
      await screen.findByRole('heading', { name: 'Travel expense categories' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Transport')).toBeInTheDocument()
  })

  it('shows the Clothes catalog tabs and the colour swatch column', async () => {
    mockBackend()
    renderAt('/configuration/clothes?catalog=categories')
    expect(
      await screen.findByRole('heading', { name: 'Clothing categories' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Tops')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: 'Colours' }))

    expect(
      await screen.findByRole('heading', { name: 'Clothing colours' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Black')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Colour' })).toBeInTheDocument()
    expect(screen.getByText('#111111')).toBeInTheDocument()
  })

  it('shows the Assets catalog tabs', async () => {
    mockBackend()
    renderAt('/configuration/assets?catalog=categories')
    expect(
      await screen.findByRole('heading', { name: 'Asset categories' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Furniture')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: 'Locations' }))

    expect(
      await screen.findByRole('heading', { name: 'Asset locations' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Garage')).toBeInTheDocument()
  })

  it('shows the Maintenance types section', async () => {
    mockBackend()
    renderAt('/configuration/maintenance')
    expect(
      await screen.findByRole('heading', { name: 'Maintenance types' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Repair')).toBeInTheDocument()
  })

  it('shows the Processes categories section', async () => {
    mockBackend()
    renderAt('/configuration/processes')
    expect(
      await screen.findByRole('heading', { name: 'Process categories' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Administrative')).toBeInTheDocument()
  })

  it('shows the Firebird catalog tabs', async () => {
    mockBackend()
    renderAt('/configuration/firebird?catalog=person-categories')
    expect(
      await screen.findByRole('heading', { name: 'Person categories' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Family')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: 'Username platforms' }))

    expect(
      await screen.findByRole('heading', { name: 'Username platforms' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Discord')).toBeInTheDocument()
  })

  it('shows the Projects programs and axes tabs ordered by code', async () => {
    mockBackend({
      programs: [
        { id: 1, code: 'WORK', name: 'Work', sortOrder: 1 },
        { id: 2, code: 'HOME', name: 'Household', sortOrder: 2 },
      ],
      axes: [
        { id: 10, code: 'ZZZZ', name: 'Later', programId: 1, sortOrder: 1 },
        { id: 11, code: 'AAAA', name: 'First', programId: 2, sortOrder: 2 },
      ],
    })
    renderAt('/configuration/projects')
    expect(
      await screen.findByRole('heading', { name: 'Project programs' }),
    ).toBeInTheDocument()
    await screen.findByText('Household')
    const programRows = screen.getAllByRole('row')
    expect(within(programRows[1]).getByText('HOME')).toBeInTheDocument()
    expect(within(programRows[2]).getByText('WORK')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: 'Axes' }))

    expect(
      await screen.findByRole('heading', { name: 'Project axes' }),
    ).toBeInTheDocument()
    const axisRows = screen.getAllByRole('row')
    expect(within(axisRows[1]).getByText('AAAA')).toBeInTheDocument()
    expect(within(axisRows[1]).getByText('HOME - Household')).toBeInTheDocument()
    expect(within(axisRows[2]).getByText('ZZZZ')).toBeInTheDocument()
  })

  it('renders the empty state for a catalog with no rows', async () => {
    mockBackend({ suppliers: [] })
    renderAt(suppliersHome)
    expect(
      await screen.findByText(
        'No suppliers yet. Add the first one so forms can use it.',
      ),
    ).toBeInTheDocument()
  })
})

describe('Configuration creation and editing', () => {
  it('shows a validation error when the name is empty', async () => {
    mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('button', { name: 'New supplier' }))
    const dialog = await screen.findByRole('dialog', { name: 'New supplier' })
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await within(dialog).findByText('Enter a name.')).toBeInTheDocument()
  })

  it('creates a supplier and shows a confirmation toast', async () => {
    const { calls } = mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('button', { name: 'New supplier' }))
    const dialog = await screen.findByRole('dialog', { name: 'New supplier' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Leroy Merlin')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await screen.findByText('Added')).toBeInTheDocument()
    const created = calls.find((call) => call.method === 'POST')
    expect(created?.body).toEqual({ name: 'Leroy Merlin' })
  })

  it('creates an asset category through the Assets section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/assets?catalog=categories')
    await screen.findByText('Furniture')

    await userEvent.click(screen.getByRole('button', { name: 'New category' }))
    const dialog = await screen.findByRole('dialog', { name: 'New category' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Appliances')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await screen.findByText('Added')).toBeInTheDocument()
    expect(
      calls.find(
        (call) => call.method === 'POST' && call.url === '/api/assets/categories',
      )?.body,
    ).toEqual({ name: 'Appliances' })
  })

  it('updates an asset location through the Assets section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/assets?catalog=locations')
    await screen.findByText('Living room')

    await userEvent.click(screen.getByRole('button', { name: 'Edit Living room' }))
    const dialog = await screen.findByRole('dialog', { name: 'Edit location' })
    const name = within(dialog).getByLabelText('Name')
    await userEvent.clear(name)
    await userEvent.type(name, 'Storage room')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByText('Saved')).toBeInTheDocument()
    expect(
      calls.find(
        (call) => call.method === 'PUT' && call.url === '/api/assets/locations/1',
      )?.body,
    ).toEqual({ name: 'Storage room' })
  })

  it('creates a maintenance type through the Maintenance section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/maintenance')
    await screen.findByText('Repair')

    await userEvent.click(screen.getByRole('button', { name: 'New type' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'New maintenance type',
    })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Preventive')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await screen.findByText('Added')).toBeInTheDocument()
    expect(
      calls.find(
        (call) => call.method === 'POST' && call.url === '/api/maintenance/types',
      )?.body,
    ).toEqual({ name: 'Preventive' })
  })

  it('creates a process category through the Processes section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/processes')
    await screen.findByText('Administrative')

    await userEvent.click(screen.getByRole('button', { name: 'New category' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'New process category',
    })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Vehicle')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await screen.findByText('Added')).toBeInTheDocument()
    expect(
      calls.find(
        (call) => call.method === 'POST' && call.url === '/api/processes/categories',
      )?.body,
    ).toEqual({ name: 'Vehicle' })
  })

  it('creates a Projects program and refetches Projects structure caches', async () => {
    const { calls, fetchMock } = mockBackend()
    renderAt('/configuration/projects')
    await screen.findByText('Household')

    await userEvent.click(screen.getByRole('button', { name: 'New program' }))
    const dialog = await screen.findByRole('dialog', { name: 'New program' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Growth')
    await userEvent.type(within(dialog).getByLabelText('Code'), 'grow')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    await waitFor(() =>
      expect(calls.find((call) => call.url === '/api/projects/programs')?.body).toEqual(
        {
          name: 'Growth',
          code: 'GROW',
        },
      ),
    )
    expect(await screen.findByText('Added')).toBeInTheDocument()
    await waitFor(() =>
      expect(
        fetchMock.mock.calls.filter(([input]) =>
          urlOf(input).endsWith('/api/projects/programs'),
        ).length,
      ).toBeGreaterThan(1),
    )
    expect(
      fetchMock.mock.calls.filter(([input]) =>
        urlOf(input).endsWith('/api/projects/axes'),
      ).length,
    ).toBeGreaterThan(1)
  })

  it('updates a Projects axis and preserves the selected parent program', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/projects')
    await screen.findByText('Household')
    await userEvent.click(screen.getByRole('tab', { name: 'Axes' }))
    await screen.findByText('Planning')

    await userEvent.click(screen.getByRole('button', { name: 'Edit Planning' }))
    const dialog = await screen.findByRole('dialog', { name: 'Edit axis' })
    await userEvent.clear(within(dialog).getByLabelText('Name'))
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Roadmap')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Save changes' }))

    await waitFor(() =>
      expect(calls.find((call) => call.url === '/api/projects/axes/10')?.body).toEqual({
        name: 'Roadmap',
        code: 'PLAN',
        programId: 1,
      }),
    )
  })

  it('maps a Projects duplicate-code server error onto the code field', async () => {
    mockBackend({
      structureCreateResponse: () =>
        json({ code: 'projects.program.duplicate_code' }, 409),
    })
    renderAt('/configuration/projects')
    await screen.findByText('Household')

    await userEvent.click(screen.getByRole('button', { name: 'New program' }))
    const dialog = await screen.findByRole('dialog', { name: 'New program' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Duplicate')
    await userEvent.type(within(dialog).getByLabelText('Code'), 'HOME')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(
      await within(dialog).findByText('Another entry already uses this code.'),
    ).toBeInTheDocument()
  })

  it('validates the three-letter currency code on the client', async () => {
    mockBackend()
    renderAt('/configuration/global?catalog=currencies')
    await screen.findByText('Euro')

    await userEvent.click(screen.getByRole('button', { name: 'New currency' }))
    const dialog = await screen.findByRole('dialog', { name: 'New currency' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Pound')
    await userEvent.type(within(dialog).getByLabelText('Code'), 'GB')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(
      await within(dialog).findByText('Use exactly three letters, for example EUR.'),
    ).toBeInTheDocument()
  })

  it('creates a clothing colour with its hex colour value', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/clothes?catalog=colors')
    await screen.findByText('Black')

    await userEvent.click(screen.getByRole('button', { name: 'New colour' }))
    const dialog = await screen.findByRole('dialog', { name: 'New colour' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Navy')
    const hex = within(dialog).getByLabelText('Colour value')
    await userEvent.clear(hex)
    await userEvent.type(hex, '#1A2B3C')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await screen.findByText('Added')).toBeInTheDocument()
    const created = calls.find((call) => call.method === 'POST')
    expect(created?.body).toEqual({ name: 'Navy', colorValue: '#1A2B3C' })
  })

  it('validates the hex colour value on the client', async () => {
    mockBackend()
    renderAt('/configuration/clothes?catalog=colors')
    await screen.findByText('Black')

    await userEvent.click(screen.getByRole('button', { name: 'New colour' }))
    const dialog = await screen.findByRole('dialog', { name: 'New colour' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Broken')
    const hex = within(dialog).getByLabelText('Colour value')
    await userEvent.clear(hex)
    await userEvent.type(hex, 'not-a-colour')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(
      await within(dialog).findByText('Enter a hex colour value, for example #1A2B3C.'),
    ).toBeInTheDocument()
  })

  it('maps a duplicate-name server error onto the name field', async () => {
    mockBackend({
      createResponse: () => json({ code: 'configuration.catalog.duplicate_name' }, 409),
    })
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('button', { name: 'New supplier' }))
    const dialog = await screen.findByRole('dialog', { name: 'New supplier' })
    await userEvent.type(within(dialog).getByLabelText('Name'), 'Amazon')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(
      await within(dialog).findByText('Another entry already uses this name.'),
    ).toBeInTheDocument()
  })

  it('confirms before discarding unsaved edits', async () => {
    mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('button', { name: 'Edit Amazon' }))
    const dialog = await screen.findByRole('dialog', { name: 'Edit supplier' })
    await userEvent.type(within(dialog).getByLabelText('Name'), ' Web')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }))

    expect(
      await screen.findByRole('dialog', { name: 'Discard unsaved changes?' }),
    ).toBeInTheDocument()
  })
})

describe('Configuration reordering', () => {
  it('disables the up control on the first row and moves on demand', async () => {
    const { calls } = mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    expect(screen.getByRole('button', { name: 'Move Amazon up' })).toBeDisabled()
    await userEvent.click(screen.getByRole('button', { name: 'Move Amazon down' }))

    await waitFor(() =>
      expect(calls.find((call) => call.url.endsWith('/move'))?.body).toEqual({
        direction: 'down',
      }),
    )
  })

  it('returns focus to the move control after a reorder', async () => {
    mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    const down = screen.getByRole('button', { name: 'Move Amazon down' })
    await userEvent.click(down)

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Move Amazon down' })).toHaveFocus(),
    )
  })

  it('reorders asset locations through the Assets section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/assets?catalog=locations')
    await screen.findByText('Living room')

    await userEvent.click(screen.getByRole('button', { name: 'Move Living room down' }))

    await waitFor(() =>
      expect(
        calls.find(
          (call) =>
            call.method === 'POST' && call.url === '/api/assets/locations/1/move',
        )?.body,
      ).toEqual({ direction: 'down' }),
    )
  })

  it('reorders maintenance types through the Maintenance section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/maintenance')
    await screen.findByText('Repair')

    await userEvent.click(screen.getByRole('button', { name: 'Move Repair down' }))

    await waitFor(() =>
      expect(
        calls.find(
          (call) =>
            call.method === 'POST' && call.url === '/api/maintenance/types/1/move',
        )?.body,
      ).toEqual({ direction: 'down' }),
    )
  })

  it('reorders process categories through the Processes section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/processes')
    await screen.findByText('Administrative')

    await userEvent.click(
      screen.getByRole('button', { name: 'Move Administrative down' }),
    )

    await waitFor(() =>
      expect(
        calls.find(
          (call) =>
            call.method === 'POST' && call.url === '/api/processes/categories/1/move',
        )?.body,
      ).toEqual({ direction: 'down' }),
    )
  })

  it('reorders Firebird username platforms through the Firebird section', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/firebird?catalog=username-platforms')
    await screen.findByText('Email')

    await userEvent.click(screen.getByRole('button', { name: 'Move Email down' }))

    await waitFor(() =>
      expect(
        calls.find(
          (call) =>
            call.method === 'POST' && call.url === '/api/people/platforms/1/move',
        )?.body,
      ).toEqual({ direction: 'down' }),
    )
  })
})

describe('Configuration deletion', () => {
  it('deletes an unreferenced value directly', async () => {
    const { calls } = mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Amazon' }))
    const dialog = await screen.findByRole('dialog', { name: 'Delete Amazon?' })
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.some(
          (call) =>
            call.method === 'DELETE' &&
            call.url.endsWith('/api/configuration/suppliers/1'),
        ),
      ).toBe(true),
    )
    expect(await screen.findByText('Removed')).toBeInTheDocument()
  })

  it('replaces references before deleting a referenced value', async () => {
    const { calls } = mockBackend({
      impacts: {
        'suppliers:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: true,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Amazon' }))
    const dialog = await screen.findByRole('dialog', { name: 'Remove Amazon' })
    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find((call) => call.url.endsWith('/replace-and-delete'))?.body,
      ).toEqual({ replacementId: 2, clearReferences: false, exchangeRate: null }),
    )
  })

  it('clears references when chosen for an optional catalog', async () => {
    const { calls } = mockBackend({
      impacts: {
        'suppliers:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: true,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Amazon' }))
    const dialog = await screen.findByRole('dialog', { name: 'Remove Amazon' })
    await userEvent.click(
      within(dialog).getByRole('radio', { name: /Leave the value empty/ }),
    )
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find((call) => call.url.endsWith('/replace-and-delete'))?.body,
      ).toEqual({ replacementId: null, clearReferences: true, exchangeRate: null }),
    )
  })

  it('clears a referenced clothing colour through the optional clear path', async () => {
    const { calls } = mockBackend({
      impacts: {
        'clothingColors:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: true,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt('/configuration/clothes?catalog=colors')
    await screen.findByText('Black')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Black' }))
    const dialog = await screen.findByRole('dialog', { name: 'Remove Black' })
    await userEvent.click(
      within(dialog).getByRole('radio', { name: /Leave the value empty/ }),
    )
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find((call) => call.url.endsWith('/replace-and-delete'))?.body,
      ).toEqual({ replacementId: null, clearReferences: true, exchangeRate: null }),
    )
  })

  it('requires replacement for a referenced asset category', async () => {
    const { calls } = mockBackend({
      impacts: {
        'assetCategories:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: false,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt('/configuration/assets?catalog=categories')
    await screen.findByText('Furniture')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Furniture' }))
    const dialog = await screen.findByRole('dialog', { name: 'Remove Furniture' })

    expect(
      within(dialog).queryByRole('radio', { name: /Leave the value empty/ }),
    ).not.toBeInTheDocument()

    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find((call) => call.url.endsWith('/replace-and-delete'))?.body,
      ).toEqual({ replacementId: 2, clearReferences: false, exchangeRate: null }),
    )
  })

  it('requires replacement for a referenced asset location', async () => {
    const { calls } = mockBackend({
      impacts: {
        'assetLocations:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: false,
          hasReplacementCandidates: true,
        },
      },
    })

    renderAt('/configuration/assets?catalog=locations')
    await screen.findByText('Living room')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Living room' }))
    const dialog = await screen.findByRole('dialog', { name: 'Remove Living room' })

    expect(
      within(dialog).queryByRole('radio', { name: /Leave the value empty/ }),
    ).not.toBeInTheDocument()

    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find((call) => call.url.endsWith('/replace-and-delete'))?.body,
      ).toEqual({ replacementId: 2, clearReferences: false, exchangeRate: null }),
    )
  })

  it('requires replacement for a referenced maintenance type', async () => {
    const { calls } = mockBackend({
      impacts: {
        'maintenanceTypes:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: false,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt('/configuration/maintenance')
    await screen.findByText('Repair')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Repair' }))
    const dialog = await screen.findByRole('dialog', { name: 'Remove Repair' })

    expect(
      within(dialog).queryByRole('radio', { name: /Leave the value empty/ }),
    ).not.toBeInTheDocument()

    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find((call) => call.url === '/api/maintenance/types/1/replace-and-delete')
          ?.body,
      ).toEqual({ replacementId: 2, clearReferences: false, exchangeRate: null }),
    )
  })

  it('requires replacement for a referenced process category', async () => {
    const { calls } = mockBackend({
      impacts: {
        'processCategories:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: false,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt('/configuration/processes')
    await screen.findByText('Administrative')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Administrative' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'Remove Administrative',
    })

    expect(
      within(dialog).queryByRole('radio', { name: /Leave the value empty/ }),
    ).not.toBeInTheDocument()
    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find(
          (call) => call.url === '/api/processes/categories/1/replace-and-delete',
        )?.body,
      ).toEqual({ replacementId: 2, clearReferences: false, exchangeRate: null }),
    )
  })

  it('requires replacement for a referenced Firebird person category', async () => {
    const { calls } = mockBackend({
      impacts: {
        'personCategories:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          canClearReferences: false,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt('/configuration/firebird?catalog=person-categories')
    await screen.findByText('Family')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Family' }))
    const dialog = await screen.findByRole('dialog', { name: 'Remove Family' })

    expect(
      within(dialog).queryByRole('radio', { name: /Leave the value empty/ }),
    ).not.toBeInTheDocument()
    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.find((call) => call.url === '/api/people/categories/1/replace-and-delete')
          ?.body,
      ).toEqual({ replacementId: 2, clearReferences: false, exchangeRate: null }),
    )
  })

  it('deletes an empty Projects program directly', async () => {
    const { calls } = mockBackend()
    renderAt('/configuration/projects')
    await screen.findByText('Household')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Work' }))
    const dialog = await screen.findByRole('dialog', { name: 'Delete Work?' })
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() =>
      expect(
        calls.some(
          (call) => call.method === 'DELETE' && call.url === '/api/projects/programs/2',
        ),
      ).toBe(true),
    )
  })

  it('reassigns children before deleting a non-empty Projects program', async () => {
    const { calls } = mockBackend({
      structureImpacts: {
        'programs:1': { childCount: 2, hasCompatibleTarget: true },
      },
    })
    renderAt('/configuration/projects')
    await screen.findByText('Household')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Household' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'Reassign and remove Household',
    })
    expect(
      within(dialog).getByText(
        'Impact summary: 2 children will be reassigned. No private item details are shown.',
      ),
    ).toBeInTheDocument()
    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.click(
      within(dialog).getByRole('button', { name: 'Reassign and delete' }),
    )

    await waitFor(() =>
      expect(
        calls.find(
          (call) => call.url === '/api/projects/programs/1/reassign-and-delete',
        )?.body,
      ).toEqual({ targetNodeId: 2 }),
    )
    expect(await screen.findByText('Reassigned')).toBeInTheDocument()
  })

  it('blocks deletion of a non-empty Projects axis when no target exists', async () => {
    const { calls } = mockBackend({
      axes: [{ id: 10, code: 'PLAN', name: 'Planning', programId: 1, sortOrder: 1 }],
      structureImpacts: {
        'axes:10': { childCount: 1, hasCompatibleTarget: false },
      },
    })
    renderAt('/configuration/projects')
    await screen.findByText('Household')
    await userEvent.click(screen.getByRole('tab', { name: 'Axes' }))
    await screen.findByText('Planning')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Planning' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'Cannot remove Planning',
    })

    expect(
      within(dialog).getByText(
        'Impact summary: 1 child will be reassigned. No private item details are shown.',
      ),
    ).toBeInTheDocument()
    expect(within(dialog).queryByRole('combobox')).not.toBeInTheDocument()
    expect(calls.some((call) => call.url.includes('reassign-and-delete'))).toBe(false)
  })

  it('converts a referenced currency with a matching formula and command', async () => {
    const { calls } = mockBackend({
      impacts: {
        'currencies:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          requiresExchangeRate: true,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt('/configuration/global?catalog=currencies')
    await screen.findByText('Euro')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Euro' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'Convert and remove Euro',
    })
    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.type(within(dialog).getByLabelText('Exchange rate'), '1.25')

    // The displayed formula must match the submitted conversion command.
    expect(within(dialog).getByText('1 EUR = 1.25 USD')).toBeInTheDocument()

    await userEvent.click(
      within(dialog).getByRole('button', { name: 'Convert and remove' }),
    )

    await waitFor(() =>
      expect(
        calls.find((call) => call.url.endsWith('/replace-and-delete'))?.body,
      ).toEqual({ replacementId: 2, clearReferences: false, exchangeRate: 1.25 }),
    )
  })

  it('rejects a non-positive or too-precise exchange rate before submitting', async () => {
    const { calls } = mockBackend({
      impacts: {
        'currencies:1': {
          isReferenced: true,
          canDeleteDirectly: false,
          requiresExchangeRate: true,
          hasReplacementCandidates: true,
        },
      },
    })
    renderAt('/configuration/global?catalog=currencies')
    await screen.findByText('Euro')

    await userEvent.click(screen.getByRole('button', { name: 'Delete Euro' }))
    const dialog = await screen.findByRole('dialog', {
      name: 'Convert and remove Euro',
    })
    await userEvent.selectOptions(within(dialog).getByRole('combobox'), '2')
    await userEvent.type(within(dialog).getByLabelText('Exchange rate'), '0')
    await userEvent.click(
      within(dialog).getByRole('button', { name: 'Convert and remove' }),
    )

    expect(
      await within(dialog).findByText(
        'Enter a positive rate with up to eight decimal places.',
      ),
    ).toBeInTheDocument()
    expect(calls.some((call) => call.url.includes('replace-and-delete'))).toBe(false)
  })
})

describe('Configuration accessibility', () => {
  it('has no automated accessibility violations', async () => {
    mockBackend()
    renderAt(suppliersHome)
    await screen.findByText('Amazon')

    const result = await axe.run(document.body, {
      rules: { 'color-contrast': { enabled: false } },
    })
    expect(result.violations).toEqual([])
  })
})
