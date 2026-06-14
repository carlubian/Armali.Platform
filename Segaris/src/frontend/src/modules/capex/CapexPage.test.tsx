import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { CapexEntrySummary } from '@/app/api/capex'

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

function makeEntry(
  id: number,
  overrides: Partial<CapexEntrySummary> = {},
): CapexEntrySummary {
  return {
    id,
    title: `Entry ${id.toString().padStart(2, '0')}`,
    movementType: id % 2 === 0 ? 'Income' : 'Expense',
    status: 'Planning',
    dueDate: '2026-06-10',
    categoryId: 14,
    categoryName: 'Other',
    supplierId: null,
    supplierName: null,
    costCenterId: null,
    costCenterName: null,
    currencyId: 1,
    currencyCode: 'EUR',
    totalAmount: id * 10,
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

interface BackendOptions {
  entries?: CapexEntrySummary[]
}

function mockBackend(options: BackendOptions = {}) {
  const entries =
    options.entries ?? Array.from({ length: 3 }, (_, i) => makeEntry(i + 1))
  const requests: Array<{ method: string; url: string }> = []

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
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

      if (url.startsWith('/api/capex/categories')) {
        return json([{ id: 14, code: 'OTHER', name: 'Other' }])
      }
      if (url.startsWith('/api/configuration/suppliers')) {
        return json([{ id: 1, code: 'AMAZON', name: 'Amazon' }])
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

      if (url.startsWith('/api/capex/entries') && method === 'GET') {
        requests.push({ method, url })
        const parsed = new URL(url, 'http://localhost')
        const params = parsed.searchParams
        const page = Number(params.get('page') ?? '1')
        const pageSize = Number(params.get('pageSize') ?? '25')
        const search = params.get('search')?.toLowerCase() ?? ''
        const type = params.get('type')

        let filtered = entries
        if (search) {
          filtered = filtered.filter((e) => e.title.toLowerCase().includes(search))
        }
        if (type) {
          filtered = filtered.filter((e) => e.movementType === type)
        }

        const start = (page - 1) * pageSize
        const items = filtered.slice(start, start + pageSize)
        return json({ items, page, pageSize, totalCount: filtered.length })
      }

      throw new Error(`Unexpected request: ${method} ${url}`)
    })

  return { fetchMock, requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/capex')
})

afterEach(() => vi.restoreAllMocks())

describe('Capex entries table', () => {
  it('renders accessible entries with separate currencies', async () => {
    mockBackend({
      entries: [
        makeEntry(1, { currencyCode: 'USD', currencyId: 2, totalAmount: 12 }),
        makeEntry(2, { currencyCode: 'EUR', currencyId: 1, totalAmount: 34 }),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Entry 01')).toBeInTheDocument()
    const usdRow = screen.getByText('Entry 01').closest('tr') as HTMLElement
    const eurRow = screen.getByText('Entry 02').closest('tr') as HTMLElement
    expect(within(usdRow).getByText(/US\$|\$/)).toBeInTheDocument()
    expect(within(eurRow).getByText(/€/)).toBeInTheDocument()
  })

  it('serializes the search term into the entries request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Entry 01')
    await user.type(screen.getByLabelText('Search'), 'Entry 02')

    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('search=Entry+02'))).toBe(true),
    )
  })

  it('requests an ascending sort when a column header is activated', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Entry 01')
    await user.click(screen.getByRole('button', { name: /Sort by Title/ }))

    await waitFor(() =>
      expect(
        requests.some(
          (r) => r.url.includes('sort=title') && r.url.includes('sortDirection=asc'),
        ),
      ).toBe(true),
    )
  })

  it('shows an active-filter chip and removes it on click', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Entry 01')
    await user.selectOptions(screen.getByLabelText('Type'), 'Income')

    const chip = await screen.findByRole('button', { name: 'Remove Income filter' })
    expect(chip).toBeInTheDocument()

    await user.click(chip)
    expect(
      screen.queryByRole('button', { name: 'Remove Income filter' }),
    ).not.toBeInTheDocument()
  })

  it('paginates through the result set', async () => {
    const user = userEvent.setup()
    mockBackend({ entries: Array.from({ length: 30 }, (_, i) => makeEntry(i + 1)) })
    render(<App />)

    await screen.findByText('Entry 01')
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
    expect(screen.queryByText('Entry 26')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(await screen.findByText('Entry 26')).toBeInTheDocument()
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument()
  })

  it('shows an empty state when no entries match', async () => {
    mockBackend({ entries: [] })
    render(<App />)

    expect(
      await screen.findByText('No entries match this view yet.'),
    ).toBeInTheDocument()
  })
})

describe('Capex navigation', () => {
  it('loads the Capex module lazily from the launcher and returns to it', async () => {
    const user = userEvent.setup()
    mockBackend()
    window.history.replaceState({}, '', '/')
    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Capex/ }))

    expect(await screen.findByRole('heading', { name: 'Entries' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Launcher' }))
    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
  })
})
