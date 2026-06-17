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
  sortOrder: number
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

interface BackendOptions {
  roles?: string[]
  suppliers?: Row[]
  costCenters?: Row[]
  currencies?: Row[]
  categories?: Row[]
  opexCategories?: Row[]
  /** Impact override keyed by `${catalog}:${id}`. */
  impacts?: Record<string, Partial<CatalogDeletionImpact>>
  /** Force a create response (e.g. a duplicate-name conflict). */
  createResponse?: () => Response
}

const catalogPaths: Record<string, string> = {
  'configuration/suppliers': 'suppliers',
  'configuration/cost-centers': 'costCenters',
  'configuration/currencies': 'currencies',
  'capex/categories': 'categories',
  'opex/categories': 'opexCategories',
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

      const match = url.match(
        /^\/api\/(configuration\/(?:suppliers|cost-centers|currencies)|capex\/categories|opex\/categories)(?:\/(\d+)(\/move|\/deletion-impact|\/replace-and-delete)?)?(?:\?.*)?$/,
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
          const input = body as { name: string; code?: string }
          const created: Row = {
            id: nextId++,
            name: input.name,
            code: input.code,
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
          const input = body as { name: string; code?: string }
          const target = rows.find((row) => row.id === id)
          if (target != null) {
            target.name = input.name
            if (input.code != null) target.code = input.code
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
