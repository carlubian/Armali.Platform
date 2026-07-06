import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  InventoryItem,
  InventoryItemPriceHistory,
  InventoryItemSummary,
  InventoryOrderSummary,
} from '@/app/api/inventory'

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

function makeItem(
  id: number,
  overrides: Partial<InventoryItemSummary> = {},
): InventoryItemSummary {
  return {
    id,
    name: `Item ${id.toString().padStart(2, '0')}`,
    status: 'Active',
    categoryId: 1,
    categoryName: 'Cleaning',
    locationId: 1,
    locationName: 'Pantry',
    currentStock: 10,
    minimumStock: 2,
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeItemDetail(id: number): InventoryItem {
  return {
    id,
    name: `Item ${id.toString().padStart(2, '0')}`,
    status: 'Active',
    notes: null,
    categoryId: 1,
    categoryName: 'Cleaning',
    locationId: 1,
    locationName: 'Pantry',
    currentStock: 11,
    minimumStock: 2,
    visibility: 'Public',
    suppliers: [{ supplierId: 1, supplierName: 'Endesa' }],
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-01-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: '2026-01-01T00:00:00Z',
  }
}

function makeOrder(
  id: number,
  overrides: Partial<InventoryOrderSummary> = {},
): InventoryOrderSummary {
  return {
    id,
    supplierId: 1,
    supplierName: 'Endesa',
    status: 'Active',
    orderDate: '2026-06-01',
    expectedReceiptDate: '2026-06-08',
    currencyId: 1,
    currencyCode: 'EUR',
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makePriceHistory(item: InventoryItemSummary): InventoryItemPriceHistory {
  return {
    itemId: item.id,
    itemName: item.name,
    cutoffDate: '2025-07-06',
    minimumRecentOrderCount: 24,
    returnedOrderCount: 2,
    entries: [
      {
        orderId: 2,
        lineId: 22,
        supplierName: 'Endesa',
        status: 'Received',
        orderDate: '2026-06-01',
        currencyCode: 'EUR',
        quantity: 2,
        lineTotal: 9.98,
        unitPrice: 4.99,
      },
      {
        orderId: 1,
        lineId: 11,
        supplierName: 'Endesa',
        status: 'Active',
        orderDate: '2026-05-01',
        currencyCode: 'EUR',
        quantity: 1,
        lineTotal: 6.5,
        unitPrice: 6.5,
      },
    ],
  }
}

interface BackendOptions {
  items?: InventoryItemSummary[]
  orders?: InventoryOrderSummary[]
}

function mockBackend(options: BackendOptions = {}) {
  const items = options.items ?? [makeItem(1, { currentStock: 0, minimumStock: 5 })]
  const orders = options.orders ?? [makeOrder(1)]
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

    if (url.startsWith('/api/inventory/categories')) {
      return json([{ id: 1, name: 'Cleaning', sortOrder: 1 }])
    }
    if (url.startsWith('/api/inventory/locations')) {
      return json([{ id: 1, name: 'Pantry', sortOrder: 1 }])
    }
    if (url.startsWith('/api/configuration/suppliers')) {
      return json([{ id: 1, name: 'Endesa', sortOrder: 1 }])
    }
    if (url.startsWith('/api/configuration/currencies')) {
      return json([{ id: 1, code: 'EUR', name: 'Euro', sortOrder: 1 }])
    }

    const historyMatch = url.match(/\/api\/inventory\/items\/(\d+)\/price-history/)
    if (historyMatch != null && method === 'GET') {
      requests.push({ method, url })
      const item = items.find((candidate) => candidate.id === Number(historyMatch[1]))
      return json(makePriceHistory(item ?? makeItem(Number(historyMatch[1]))))
    }

    const stockMatch = url.match(/\/api\/inventory\/items\/(\d+)\/stock-adjustments/)
    if (stockMatch != null && method === 'POST') {
      requests.push({ method, url })
      return json(makeItemDetail(Number(stockMatch[1])))
    }

    if (url.startsWith('/api/inventory/items') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const params = parsed.searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '25')
      const search = params.get('search')?.toLowerCase() ?? ''
      let filtered = items
      if (search)
        filtered = filtered.filter((i) => i.name.toLowerCase().includes(search))
      const start = (page - 1) * pageSize
      const slice = filtered.slice(start, start + pageSize)
      return json({ items: slice, page, pageSize, totalCount: filtered.length })
    }

    if (url.startsWith('/api/inventory/orders') && method === 'GET') {
      requests.push({ method, url })
      return json({ items: orders, page: 1, pageSize: 25, totalCount: orders.length })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/inventory')
})

afterEach(() => vi.restoreAllMocks())

describe('Inventory items view', () => {
  it('renders items and flags low stock', async () => {
    mockBackend({
      items: [
        makeItem(1, { currentStock: 0, minimumStock: 5 }),
        makeItem(2, { currentStock: 10, minimumStock: 2 }),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Item 01')).toBeInTheDocument()
    const lowRow = screen.getByText('Item 01').closest('tr') as HTMLElement
    const okRow = screen.getByText('Item 02').closest('tr') as HTMLElement
    expect(within(lowRow).getByText('Low')).toBeInTheDocument()
    expect(within(okRow).queryByText('Low')).not.toBeInTheDocument()
  })

  it('serializes the search term into the items request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Item 01')
    await user.type(screen.getByLabelText('Search'), 'Item 01')

    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('search=Item+01'))).toBe(true),
    )
  })

  it('applies a quick stock adjustment from the row action', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Item 01')
    await user.click(screen.getByRole('button', { name: 'Adjust stock for Item 01' }))

    expect(await screen.findByText('Quick stock adjustment')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Apply' }))

    await waitFor(() =>
      expect(
        requests.some(
          (r) => r.url.includes('/stock-adjustments') && r.method === 'POST',
        ),
      ).toBe(true),
    )
    expect(await screen.findByText('Stock updated')).toBeInTheDocument()
  })

  it('opens the price history popup from the row action', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Item 01')
    await user.click(
      screen.getByRole('button', { name: 'View price history for Item 01' }),
    )

    expect(
      await screen.findByRole('dialog', { name: 'Price history: Item 01' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('img', { name: 'Unit price history chart' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Unit price' })).toBeInTheDocument()
    expect(screen.getByText('€4.99')).toBeInTheDocument()
    expect(requests.some((r) => r.url === '/api/inventory/items/1/price-history')).toBe(
      true,
    )
  })

  it('opens the new item editor', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Item 01')
    await user.click(screen.getByRole('button', { name: 'New item' }))

    expect(await screen.findByRole('dialog', { name: 'New item' })).toBeInTheDocument()
  })
})

describe('Inventory orders view', () => {
  it('switches to the orders tab and lists orders', async () => {
    const user = userEvent.setup()
    mockBackend({ orders: [makeOrder(1, { supplierName: 'Endesa' })] })
    render(<App />)

    await screen.findByText('Item 01')
    await user.click(screen.getByRole('tab', { name: 'Orders' }))

    expect(await screen.findByText('Endesa')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New order' })).toBeInTheDocument()
  })
})
