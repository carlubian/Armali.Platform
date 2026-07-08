import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  TravelExpenseSummary,
  TravelTrip,
  TravelTripSummary,
} from '@/app/api/travel'
import type { DestinationSummary } from '@/app/api/destinations'

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
    destinationId: 10,
    destinationName: 'Porto',
    destinationCountry: 'Portugal',
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
    destinationId: 10,
    destinationName: 'Porto',
    destinationCountry: 'Portugal',
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

function makeDestination(id: number, name = 'Porto'): DestinationSummary {
  return {
    id,
    name,
    categoryId: 1,
    categoryName: 'City break',
    country: 'Portugal',
    isSchengenArea: true,
    averagePlaceRating: null,
    ratedPlaceCount: 0,
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
  }
}

interface BackendOptions {
  trips?: TravelTripSummary[]
  destinations?: DestinationSummary[]
  // When true, trip detail GETs never resolve once a trip has been updated,
  // simulating the user reopening the editor before the post-save refetch has
  // repopulated the cache. This forces the editor to rely on the cached trip.
  stallDetailAfterUpdate?: boolean
}

function mockBackend(options: BackendOptions = {}) {
  const trips = options.trips ?? [makeTrip(1)]
  const destinations = options.destinations ?? [makeDestination(10)]
  const requests: Array<{ method: string; url: string; body?: unknown }> = []
  // Persist trip details so that refetches after an edit reflect the saved
  // state, mirroring the real backend.
  const tripDetails = new Map<number, TravelTrip>()
  const tripDetail = (id: number): TravelTrip =>
    tripDetails.get(id) ?? makeTripDetail(id)
  let tripUpdated = false

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
    if (url.startsWith('/api/destinations/categories')) {
      return json([{ id: 1, name: 'City break', sortOrder: 1 }])
    }
    if (url.match(/^\/api\/destinations\/\d+$/) && method === 'GET') {
      const id = Number(url.match(/\/api\/destinations\/(\d+)/)?.[1] ?? '10')
      const destination =
        destinations.find((candidate) => candidate.id === id) ?? makeDestination(id)
      return json({
        ...destination,
        entryRequirements: null,
        notes: null,
        attachments: [],
        createdById: destination.creatorId,
        createdByName: destination.creatorName,
        createdAt: '2026-01-01T00:00:00Z',
        updatedById: null,
        updatedByName: null,
        updatedAt: null,
      })
    }
    if (url.startsWith('/api/destinations') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const visibility = parsed.searchParams.get('visibility')
      const filtered =
        visibility == null
          ? destinations
          : destinations.filter((destination) => destination.visibility === visibility)
      return json({
        items: filtered,
        page: Number(parsed.searchParams.get('page') ?? '1'),
        pageSize: Number(parsed.searchParams.get('pageSize') ?? '10'),
        totalCount: filtered.length,
      })
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
      if (options.stallDetailAfterUpdate && tripUpdated) {
        return new Promise<Response>(() => {})
      }
      return json(tripDetail(Number(tripDetailMatch[1])))
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
    if (url === '/api/travel/trips' && method === 'POST') {
      requests.push({ method, url, body })
      return json({ ...makeTripDetail(99), ...(body as object), id: 99 }, 201)
    }
    if (url.match(/^\/api\/travel\/trips\/\d+$/) && method === 'PUT') {
      const id = Number(url.match(/\/api\/travel\/trips\/(\d+)/)?.[1] ?? '1')
      requests.push({ method, url, body })
      const updated = { ...tripDetail(id), ...(body as object), id }
      tripDetails.set(id, updated)
      tripUpdated = true
      return json(updated)
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
    expect(screen.getAllByText('Porto · Portugal').length).toBeGreaterThan(0)
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

  it('opens a trip with details and expenses in separate tabs', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Open Trip 01' }))

    const dialog = await screen.findByRole('dialog', { name: 'Edit trip' })
    expect(within(dialog).getByText('Porto')).toBeInTheDocument()
    expect(within(dialog).getByDisplayValue('Flight to Porto')).toBeInTheDocument()
    expect(within(dialog).queryByText('Hotel Ribeira')).not.toBeInTheDocument()

    await user.click(within(dialog).getByRole('tab', { name: 'Expenses' }))

    expect(await within(dialog).findByText('Hotel Ribeira')).toBeInTheDocument()
    // The amount appears both as the per-currency total and on the expense row.
    expect(within(dialog).getAllByText('€250.00').length).toBeGreaterThanOrEqual(1)
  })

  it('selects a public destination for a public trip and submits its id', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      destinations: [makeDestination(10, 'Porto'), makeDestination(11, 'Barcelona')],
    })
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'New trip' }))
    const dialog = await screen.findByRole('dialog', { name: 'New trip' })

    await user.type(within(dialog).getByLabelText('Name'), 'Summer trip')
    await user.click(
      within(dialog).getByRole('button', { name: 'Browse destinations' }),
    )
    expect(await screen.findByText('Select a destination')).toBeInTheDocument()

    await waitFor(() =>
      expect(
        requests.some((request) => request.url.includes('visibility=Public')),
      ).toBe(true),
    )
    const row = screen.getByText('Barcelona').closest('[role="row"]') as HTMLElement
    await user.click(within(row).getByRole('button', { name: 'Select' }))
    await user.click(within(dialog).getByRole('button', { name: 'Create' }))

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/travel/trips' &&
            isRecord(request.body) &&
            request.body.destinationId === 11,
        ),
      ).toBe(true),
    )
  })

  it('shows the edited itinerary after saving and reopening the trip', async () => {
    const user = userEvent.setup()
    mockBackend({ stallDetailAfterUpdate: true })
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Open Trip 01' }))
    let dialog = await screen.findByRole('dialog', { name: 'Edit trip' })

    const titleField = within(dialog).getByDisplayValue('Flight to Porto')
    await user.clear(titleField)
    await user.type(titleField, 'Train to Lisbon')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Edit trip' }),
      ).not.toBeInTheDocument(),
    )

    await user.click(await screen.findByRole('button', { name: 'Open Trip 01' }))
    dialog = await screen.findByRole('dialog', { name: 'Edit trip' })

    expect(within(dialog).getByDisplayValue('Train to Lisbon')).toBeInTheDocument()
    expect(
      within(dialog).queryByDisplayValue('Flight to Porto'),
    ).not.toBeInTheDocument()
  })

  it('shows a neutral placeholder for an unresolved destination', async () => {
    mockBackend({
      trips: [
        makeTrip(1, {
          destinationId: 99,
          destinationName: null,
          destinationCountry: null,
        }),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Destination unavailable')).toBeInTheDocument()
  })
})

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value != null
}
