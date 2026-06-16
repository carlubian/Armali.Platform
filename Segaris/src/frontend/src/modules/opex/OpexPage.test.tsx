import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { OpexContractSummary } from '@/app/api/opex'

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

function makeContract(
  id: number,
  overrides: Partial<OpexContractSummary> = {},
): OpexContractSummary {
  return {
    id,
    name: `Contract ${id.toString().padStart(2, '0')}`,
    movementType: id % 2 === 0 ? 'Income' : 'Expense',
    status: 'Active',
    categoryId: 1,
    categoryName: 'Utilities',
    supplierId: null,
    supplierName: null,
    costCenterId: null,
    costCenterName: null,
    currencyId: 1,
    currencyCode: 'EUR',
    expectedFrequency: 'Monthly',
    estimatedAnnualAmount: 120,
    realizedCurrentYearAmount: 60,
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

interface BackendOptions {
  contracts?: OpexContractSummary[]
}

function mockBackend(options: BackendOptions = {}) {
  const contracts =
    options.contracts ?? Array.from({ length: 3 }, (_, i) => makeContract(i + 1))
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

    if (url.startsWith('/api/opex/categories')) {
      return json([{ id: 1, name: 'Utilities', sortOrder: 1 }])
    }
    if (url.startsWith('/api/configuration/suppliers')) {
      return json([{ id: 1, code: 'ENDESA', name: 'Endesa' }])
    }
    if (url.startsWith('/api/configuration/cost-centers')) {
      return json([{ id: 1, code: 'HOUSEHOLD', name: 'Household' }])
    }
    if (url.startsWith('/api/configuration/currencies')) {
      return json([
        { id: 1, code: 'EUR', name: 'Euro' },
        { id: 2, code: 'USD', name: 'US Dollar' },
      ])
    }

    if (url.startsWith('/api/opex/contracts') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const params = parsed.searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '25')
      const search = params.get('search')?.toLowerCase() ?? ''
      const type = params.get('type')

      let filtered = contracts
      if (search) {
        filtered = filtered.filter((c) => c.name.toLowerCase().includes(search))
      }
      if (type) {
        filtered = filtered.filter((c) => c.movementType === type)
      }

      const start = (page - 1) * pageSize
      const items = filtered.slice(start, start + pageSize)
      return json({ items, page, pageSize, totalCount: filtered.length })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/opex')
})

afterEach(() => vi.restoreAllMocks())

describe('Opex contracts table', () => {
  it('renders accessible contracts with type and frequency columns', async () => {
    mockBackend({
      contracts: [
        makeContract(1, { expectedFrequency: 'Monthly', movementType: 'Expense' }),
        makeContract(2, { expectedFrequency: 'Annual', movementType: 'Income' }),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Contract 01')).toBeInTheDocument()
    const row1 = screen.getByText('Contract 01').closest('tr') as HTMLElement
    const row2 = screen.getByText('Contract 02').closest('tr') as HTMLElement
    expect(within(row1).getByText('Monthly')).toBeInTheDocument()
    expect(within(row2).getByText('Annual')).toBeInTheDocument()
    expect(within(row2).getByText('Income')).toBeInTheDocument()
  })

  it('serializes the search term into the contracts request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Contract 01')
    await user.type(screen.getByLabelText('Search'), 'Contract 02')

    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('search=Contract+02'))).toBe(true),
    )
  })

  it('requests an ascending sort when a column header is activated', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Contract 01')
    await user.click(screen.getByRole('button', { name: /Sort by Status/ }))

    await waitFor(() =>
      expect(
        requests.some(
          (r) => r.url.includes('sort=status') && r.url.includes('sortDirection=asc'),
        ),
      ).toBe(true),
    )
  })

  it('shows an active-filter chip and removes it on click', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Contract 01')
    await user.selectOptions(screen.getByLabelText('Type'), 'Expense')

    const chip = await screen.findByRole('button', { name: 'Remove Expense filter' })
    expect(chip).toBeInTheDocument()

    await user.click(chip)
    expect(
      screen.queryByRole('button', { name: 'Remove Expense filter' }),
    ).not.toBeInTheDocument()
  })

  it('paginates through the result set', async () => {
    const user = userEvent.setup()
    mockBackend({
      contracts: Array.from({ length: 30 }, (_, i) => makeContract(i + 1)),
    })
    render(<App />)

    await screen.findByText('Contract 01')
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
    expect(screen.queryByText('Contract 26')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(await screen.findByText('Contract 26')).toBeInTheDocument()
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument()
  })

  it('shows an empty state when no contracts match', async () => {
    mockBackend({ contracts: [] })
    render(<App />)

    expect(
      await screen.findByText('No contracts match this view yet.'),
    ).toBeInTheDocument()
  })
})

describe('Opex navigation', () => {
  it('loads the Opex module lazily from the launcher and returns to it', async () => {
    const user = userEvent.setup()
    mockBackend()
    window.history.replaceState({}, '', '/')
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Opex/ }))

    expect(
      await screen.findByRole('heading', { name: 'Contracts' }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Launcher' }))
    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
  })
})
