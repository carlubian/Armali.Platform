import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type {
  Destination,
  DestinationSummary,
  Place,
  PlaceSummary,
} from '@/app/api/destinations'

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

function makeDestination(
  id: number,
  overrides: Partial<DestinationSummary> = {},
): DestinationSummary {
  return {
    id,
    name: `Destination ${id.toString().padStart(2, '0')}`,
    categoryId: 1,
    categoryName: 'City break',
    country: 'Spain',
    isSchengenArea: true,
    averagePlaceRating: 4.5,
    ratedPlaceCount: 2,
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeDestinationDetail(id: number): Destination {
  return {
    ...makeDestination(id),
    entryRequirements: 'Passport or national ID.',
    notes: 'Stay near the centre.',
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-01-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
  }
}

function makePlace(id: number, overrides: Partial<PlaceSummary> = {}): PlaceSummary {
  return {
    id,
    destinationId: 1,
    name: id === 1 ? 'Park Guell' : `Place ${id}`,
    categoryId: 1,
    categoryName: 'Restaurant',
    rating: 5,
    review: 'Great views and easy to revisit.',
    address: 'Barcelona',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    ...overrides,
  }
}

function makePlaceDetail(id: number, overrides: Partial<Place> = {}): Place {
  return {
    ...makePlace(id),
    ...overrides,
  }
}

function mockBackend(
  options: { destinations?: DestinationSummary[]; places?: PlaceSummary[] } = {},
) {
  const destinations = options.destinations ?? [makeDestination(1)]
  const places = options.places ?? [makePlace(1)]
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
    if (url.startsWith('/api/launcher/attention')) return json({ modules: [] })
    if (url.startsWith('/api/destinations/categories')) {
      return json([{ id: 1, name: 'City break', sortOrder: 1 }])
    }
    if (url.startsWith('/api/destinations/place-categories')) {
      return json([{ id: 1, name: 'Restaurant', sortOrder: 1 }])
    }
    if (url.match(/^\/api\/destinations\/\d+\/attachments$/) && method === 'GET') {
      return json([])
    }
    if (url.match(/^\/api\/destinations\/\d+\/deletion-impact$/) && method === 'GET') {
      requests.push({ method, url })
      return json({ isReferenced: true, referenceCount: 2 })
    }
    if (url.match(/^\/api\/destinations\/\d+$/) && method === 'GET') {
      requests.push({ method, url })
      const id = Number(url.match(/\/api\/destinations\/(\d+)/)?.[1] ?? '1')
      return json(makeDestinationDetail(id))
    }
    if (url.match(/^\/api\/destinations\/\d+$/) && method === 'PUT') {
      requests.push({ method, url, body })
      const id = Number(url.match(/\/api\/destinations\/(\d+)/)?.[1] ?? '1')
      return json({ ...makeDestinationDetail(id), ...(body as object) })
    }
    if (url.match(/^\/api\/destinations\/\d+$/) && method === 'DELETE') {
      requests.push({ method, url })
      return new Response(null, { status: 204 })
    }
    if (url === '/api/destinations' && method === 'POST') {
      requests.push({ method, url, body })
      return json({ ...makeDestinationDetail(99), ...(body as object), id: 99 }, 201)
    }
    if (url.match(/^\/api\/destinations\/\d+\/places\/\d+$/) && method === 'GET') {
      requests.push({ method, url })
      const id = Number(url.match(/\/places\/(\d+)/)?.[1] ?? '1')
      return json(makePlaceDetail(id))
    }
    if (url.match(/^\/api\/destinations\/\d+\/places\/\d+$/) && method === 'PUT') {
      requests.push({ method, url, body })
      const id = Number(url.match(/\/places\/(\d+)/)?.[1] ?? '1')
      return json({ ...makePlaceDetail(id), ...(body as object) })
    }
    if (url.match(/^\/api\/destinations\/\d+\/places\/\d+$/) && method === 'DELETE') {
      requests.push({ method, url })
      return new Response(null, { status: 204 })
    }
    if (url.match(/^\/api\/destinations\/\d+\/places$/) && method === 'POST') {
      requests.push({ method, url, body })
      return json({ ...makePlaceDetail(99), ...(body as object), id: 99 }, 201)
    }
    if (url.match(/^\/api\/destinations\/\d+\/places/) && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const params = parsed.searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '25')
      const search = params.get('search')?.toLowerCase() ?? ''
      const category = params.get('category')
      const rating = params.get('rating')
      const filtered = places.filter(
        (place) =>
          (search === '' || place.name.toLowerCase().includes(search)) &&
          (category == null || String(place.categoryId) === category) &&
          (rating == null || String(place.rating) === rating),
      )
      return json({ items: filtered, page, pageSize, totalCount: filtered.length })
    }
    if (url.startsWith('/api/destinations') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const params = parsed.searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '25')
      const search = params.get('search')?.toLowerCase() ?? ''
      const category = params.get('category')
      const isSchengenArea = params.get('isSchengenArea')
      const filtered = destinations.filter(
        (destination) =>
          (search === '' || destination.name.toLowerCase().includes(search)) &&
          (category == null || String(destination.categoryId) === category) &&
          (isSchengenArea == null ||
            String(destination.isSchengenArea) === isSchengenArea),
      )
      return json({ items: filtered, page, pageSize, totalCount: filtered.length })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/destinations')
})

afterEach(() => vi.restoreAllMocks())

describe('Destinations page', () => {
  it('renders the destination gallery with Schengen and rating badges', async () => {
    mockBackend({
      destinations: [makeDestination(1, { name: 'Barcelona', country: 'Spain' })],
    })
    render(<App />)

    expect(await screen.findByText('Barcelona')).toBeInTheDocument()
    expect(screen.getByText('City break · Spain')).toBeInTheDocument()
    expect(screen.getAllByText('Schengen').length).toBeGreaterThan(0)
    expect(screen.getByText('4.5 average')).toBeInTheDocument()
  })

  it('serializes URL-backed search and filters into the list request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Destination 01')
    await user.type(screen.getByLabelText('Search'), 'Destination 01')
    await user.selectOptions(screen.getByRole('combobox', { name: 'Category' }), '1')
    await user.selectOptions(screen.getByRole('combobox', { name: 'Schengen' }), 'true')

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('search=Destination+01') &&
            request.url.includes('category=1') &&
            request.url.includes('isSchengenArea=true'),
        ),
      ).toBe(true),
    )
  })

  it('opens the create dialog without losing gallery state', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Destination 01')
    await user.type(screen.getByLabelText('Search'), 'Destination 01')
    await user.click(screen.getByRole('button', { name: 'New destination' }))

    expect(
      await screen.findByRole('dialog', { name: 'New destination' }),
    ).toBeInTheDocument()
    expect(window.location.search).toContain('search=Destination+01')
    expect(window.location.search).toContain('newDestination=true')
  })

  it('shows privacy-neutral trip impact before deleting a destination', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend({
      destinations: [makeDestination(1, { name: 'Barcelona' })],
    })
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Open Barcelona' }))
    const editor = await screen.findByRole('dialog', { name: 'Edit destination' })
    await user.click(within(editor).getByRole('button', { name: 'Delete destination' }))

    expect(
      await screen.findByText(
        '2 trips reference this destination. Deleting it will clear those trip links without revealing trip details.',
      ),
    ).toBeInTheDocument()

    const confirm = screen.getByRole('dialog', { name: 'Delete this destination?' })
    await user.click(
      within(confirm).getByRole('button', { name: 'Delete destination' }),
    )

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'GET' &&
            request.url === '/api/destinations/1/deletion-impact',
        ),
      ).toBe(true),
    )
    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'DELETE' && request.url === '/api/destinations/1',
        ),
      ).toBe(true),
    )
  })

  it('navigates from a card to the destination-scoped places route', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Destination 01')
    await user.click(
      screen.getByRole('button', { name: 'Open places for Destination 01' }),
    )

    expect(window.location.pathname).toBe('/destinations/1/places')
    expect(
      await screen.findByRole('heading', { name: 'Destination 01 places' }),
    ).toBeInTheDocument()
    expect(await screen.findByText('Park Guell')).toBeInTheDocument()
    expect(screen.getByText('⭐⭐⭐⭐⭐')).toBeInTheDocument()
    expect(screen.getAllByText('Restaurant').length).toBeGreaterThan(0)
  })

  it('keeps place filters scoped to the selected destination route', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    window.history.replaceState({}, '', '/destinations/1/places')
    render(<App />)

    await screen.findByText('Park Guell')
    await user.type(screen.getByLabelText('Search places'), 'Park')
    await user.selectOptions(
      screen.getByRole('combobox', { name: 'Place category' }),
      '1',
    )
    await user.selectOptions(screen.getByRole('combobox', { name: 'Rating' }), '5')

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.url.includes('/api/destinations/1/places') &&
            request.url.includes('search=Park') &&
            request.url.includes('category=1') &&
            request.url.includes('rating=5'),
        ),
      ).toBe(true),
    )
    expect(window.location.pathname).toBe('/destinations/1/places')
  })

  it('creates a destination-scoped place without losing list state', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    window.history.replaceState({}, '', '/destinations/1/places?search=Park')
    render(<App />)

    await screen.findByText('Park Guell')
    await user.click(screen.getByRole('button', { name: 'New place' }))
    const dialog = await screen.findByRole('dialog', { name: 'New place' })
    expect(window.location.search).toContain('search=Park')
    expect(window.location.search).toContain('newPlace=true')

    await user.clear(within(dialog).getByLabelText('Name'))
    await user.type(within(dialog).getByLabelText('Name'), 'Casa Mila')
    expect(within(dialog).getByRole('option', { name: '⭐⭐⭐⭐' })).toBeInTheDocument()
    await user.selectOptions(within(dialog).getByLabelText('Rating'), '4')
    await user.type(within(dialog).getByLabelText('Address'), 'Passeig de Gracia')
    await user.click(within(dialog).getByRole('button', { name: 'Create' }))

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'POST' &&
            request.url === '/api/destinations/1/places' &&
            (request.body as { name?: string }).name === 'Casa Mila',
        ),
      ).toBe(true),
    )
  })

  it('validates place name before submitting', async () => {
    const user = userEvent.setup()
    mockBackend()
    window.history.replaceState({}, '', '/destinations/1/places')
    render(<App />)

    await screen.findByText('Park Guell')
    await user.click(screen.getByRole('button', { name: 'New place' }))
    const dialog = await screen.findByRole('dialog', { name: 'New place' })
    await user.clear(within(dialog).getByLabelText('Name'))
    await user.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await within(dialog).findByText('A name is required.')).toBeInTheDocument()
  })

  it('validates destination name before submitting', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Destination 01')
    await user.click(screen.getByRole('button', { name: 'New destination' }))
    const dialog = await screen.findByRole('dialog', { name: 'New destination' })
    await user.clear(within(dialog).getByLabelText('Name'))
    await user.click(within(dialog).getByRole('button', { name: 'Create' }))

    expect(await within(dialog).findByText('A name is required.')).toBeInTheDocument()
  })

  it('deletes a destination with a privacy-neutral confirmation', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Destination 01')
    await user.click(screen.getByRole('button', { name: 'Open Destination 01' }))
    const editor = await screen.findByRole('dialog', { name: 'Edit destination' })
    await user.click(within(editor).getByRole('button', { name: 'Delete destination' }))

    const confirm = await screen.findByRole('dialog', {
      name: 'Delete this destination?',
    })
    expect(
      within(confirm).getByText(/trips reference this destination/),
    ).toBeInTheDocument()
    await user.click(
      within(confirm).getByRole('button', { name: 'Delete destination' }),
    )

    await waitFor(() =>
      expect(
        requests.some(
          (request) =>
            request.method === 'DELETE' && request.url === '/api/destinations/1',
        ),
      ).toBe(true),
    )
  })
})
