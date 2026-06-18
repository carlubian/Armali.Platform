import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { ClothesGarmentSummary } from '@/app/api/clothes'

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

function makeGarment(
  id: number,
  overrides: Partial<ClothesGarmentSummary> = {},
): ClothesGarmentSummary {
  return {
    id,
    name: `Garment ${id.toString().padStart(2, '0')}`,
    categoryId: 1,
    categoryName: 'Tops',
    status: 'Active',
    size: 'M',
    colors: [{ id: 1, name: 'Black', colorValue: '#111111', sortOrder: 1 }],
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

interface BackendOptions {
  garments?: ClothesGarmentSummary[]
}

function mockBackend(options: BackendOptions = {}) {
  const garments = options.garments ?? [makeGarment(1)]
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
    if (url.startsWith('/api/clothes/categories')) {
      return json([{ id: 1, name: 'Tops', sortOrder: 1 }])
    }
    if (url.startsWith('/api/clothes/colors')) {
      return json([{ id: 1, name: 'Black', colorValue: '#111111', sortOrder: 1 }])
    }
    if (url.startsWith('/api/clothes/garments') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const search = parsed.searchParams.get('search')?.toLowerCase() ?? ''
      const filtered =
        search.length === 0
          ? garments
          : garments.filter((garment) => garment.name.toLowerCase().includes(search))
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
  window.history.replaceState({}, '', '/clothes')
})

afterEach(() => vi.restoreAllMocks())

describe('Clothes gallery', () => {
  it('renders garments with colour swatches and placeholder thumbnails', async () => {
    mockBackend({
      garments: [makeGarment(1), makeGarment(2, { name: 'Wool coat', size: null })],
    })
    render(<App />)

    expect(await screen.findByText('Garment 01')).toBeInTheDocument()
    expect(screen.getByText('Wool coat')).toBeInTheDocument()
    expect(screen.getAllByTitle('Black')).toHaveLength(2)
    expect(screen.getAllByText('No image')).toHaveLength(2)
  })

  it('serializes the search term into the garment request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Garment 01')
    await user.type(screen.getByLabelText('Search'), 'Garment')

    await waitFor(() =>
      expect(requests.some((request) => request.url.includes('search=Garment'))).toBe(
        true,
      ),
    )
  })
})
