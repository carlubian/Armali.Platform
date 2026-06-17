import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  TravelExpenseSummary,
  TravelTrip,
  TravelTripSummary,
} from '@/app/api/travel'

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

function makeTrip(
  id: number,
  overrides: Partial<TravelTripSummary> = {},
): TravelTripSummary {
  return {
    id,
    name: `Trip ${id.toString().padStart(2, '0')}`,
    tripTypeId: 1,
    tripTypeName: 'European',
    destination: 'Porto',
    startDate: '2026-06-20',
    endDate: '2026-06-25',
    status: 'Planned',
    visibility: 'Public',
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeTripDetail(id: number): TravelTrip {
  return {
    id,
    name: `Trip ${id.toString().padStart(2, '0')}`,
    tripTypeId: 1,
    tripTypeName: 'European',
    destination: 'Porto',
    startDate: '2026-06-20',
    endDate: '2026-06-25',
    status: 'Planned',
    notes: null,
    visibility: 'Public',
    itinerary: [
      {
        id: 1,
        date: '2026-06-20',
        time: '09:30',
        title: 'Flight to Porto',
        place: 'MAD',
        reservationLocator: 'ABC123',
        note: null,
        sortOrder: 1,
      },
    ],
    expenseTotals: [{ currencyId: 1, currencyCode: 'EUR', amount: 250 }],
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-01-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
  }
}

function makeExpense(id: number): TravelExpenseSummary {
  return {
    id,
    expenseCategoryId: 2,
    expenseCategoryName: 'Lodging',
    description: 'Hotel Ribeira',
    date: '2026-06-20',
    amount: 250,
    currencyId: 1,
    currencyCode: 'EUR',
    supplierId: null,
    supplierName: null,
    costCenterId: null,
    costCenterName: null,
  }
}

interface BackendOptions {
  trips?: TravelTripSummary[]
}

function mockBackend(options: BackendOptions = {}) {
  const trips = options.trips ?? [makeTrip(1)]
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

    if (url.startsWith('/api/travel/trip-types')) {
      return json([{ id: 1, name: 'European', sortOrder: 1 }])
    }
    if (url.startsWith('/api/travel/expense-categories')) {
      return json([
        { id: 1, name: 'Flight', sortOrder: 1 },
        { id: 2, name: 'Lodging', sortOrder: 2 },
      ])
    }
    if (url.startsWith('/api/configuration/suppliers')) {
      return json([{ id: 1, name: 'Iberia', sortOrder: 1 }])
    }
    if (url.startsWith('/api/configuration/currencies')) {
      return json([{ id: 1, code: 'EUR', name: 'Euro', sortOrder: 1 }])
    }
    if (url.startsWith('/api/configuration/cost-centers')) {
      return json([{ id: 1, name: 'Family', sortOrder: 1 }])
    }

    const expenseAttachMatch = url.match(
      /\/api\/travel\/trips\/\d+\/expenses\/\d+\/attachments/,
    )
    if (expenseAttachMatch != null && method === 'GET') return json([])

    const expensesMatch = url.match(/\/api\/travel\/trips\/(\d+)\/expenses(\?|$)/)
    if (expensesMatch != null && method === 'GET') {
      requests.push({ method, url })
      return json({ items: [makeExpense(1)], page: 1, pageSize: 100, totalCount: 1 })
    }

    const tripAttachMatch = url.match(/\/api\/travel\/trips\/\d+\/attachments/)
    if (tripAttachMatch != null && method === 'GET') return json([])

    const tripDetailMatch = url.match(/\/api\/travel\/trips\/(\d+)(\?|$)/)
    if (tripDetailMatch != null && method === 'GET') {
      requests.push({ method, url })
      return json(makeTripDetail(Number(tripDetailMatch[1])))
    }

    if (url.startsWith('/api/travel/trips') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const params = parsed.searchParams
      const search = params.get('search')?.toLowerCase() ?? ''
      let filtered = trips
      if (search)
        filtered = filtered.filter((trip) => trip.name.toLowerCase().includes(search))
      return json({
        items: filtered,
        page: 1,
        pageSize: 25,
        totalCount: filtered.length,
      })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/travel')
})

afterEach(() => vi.restoreAllMocks())

describe('Travel trips view', () => {
  it('renders trips with their type and destination', async () => {
    mockBackend({ trips: [makeTrip(1), makeTrip(2, { name: 'Trip 02' })] })
    render(<App />)

    expect(await screen.findByText('Trip 01')).toBeInTheDocument()
    expect(screen.getByText('Trip 02')).toBeInTheDocument()
  })

  it('serializes the search term into the trips request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Trip 01')
    await user.type(screen.getByLabelText('Search'), 'Trip 01')

    await waitFor(() =>
      expect(requests.some((r) => r.url.includes('search=Trip+01'))).toBe(true),
    )
  })

  it('opens the new trip editor', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Trip 01')
    await user.click(screen.getByRole('button', { name: 'New trip' }))

    expect(await screen.findByRole('dialog', { name: 'New trip' })).toBeInTheDocument()
  })

  it('opens a trip and shows its itinerary, expenses, and totals', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Open Trip 01' }))

    const dialog = await screen.findByRole('dialog', { name: 'Edit trip' })
    expect(within(dialog).getByDisplayValue('Flight to Porto')).toBeInTheDocument()
    expect(await within(dialog).findByText('Hotel Ribeira')).toBeInTheDocument()
    // The amount appears both as the per-currency total and on the expense row.
    expect(within(dialog).getAllByText('€250.00').length).toBeGreaterThanOrEqual(1)
  })
})
