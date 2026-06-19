import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { Asset, AssetSummary } from '@/app/api/assets'

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

function makeAsset(id: number, overrides: Partial<AssetSummary> = {}): AssetSummary {
  return {
    id,
    name: `Asset ${id.toString().padStart(2, '0')}`,
    code: `A-${id}`,
    categoryId: 1,
    categoryName: 'Appliances',
    locationId: 1,
    locationName: 'Storage',
    status: 'Active',
    expectedEndOfLifeDate: '2026-07-01',
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina Velasco',
    ...overrides,
  }
}

function makeAssetDetail(id: number): Asset {
  return {
    ...makeAsset(id),
    brandModel: 'Bosch Drill',
    serialNumber: 'SN-001',
    acquisitionDate: '2026-01-15',
    notes: null,
    attachments: [],
    createdById: 7,
    createdByName: 'Marina Velasco',
    createdAt: '2026-01-01T00:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
  }
}

function mockBackend(options: { assets?: AssetSummary[] } = {}) {
  const assets = options.assets ?? [makeAsset(1)]
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
    if (url.startsWith('/api/launcher/attention')) {
      return json({ modules: [{ module: 'assets', requiresAttention: true }] })
    }
    if (url.startsWith('/api/assets/categories')) {
      return json([{ id: 1, name: 'Appliances', sortOrder: 1 }])
    }
    if (url.startsWith('/api/assets/locations')) {
      return json([{ id: 1, name: 'Storage', sortOrder: 1 }])
    }
    if (url.startsWith('/api/assets/items/') && method === 'GET') {
      requests.push({ method, url })
      const id = Number(url.match(/\/api\/assets\/items\/(\d+)/)?.[1] ?? '1')
      return json(makeAssetDetail(id))
    }
    if (url.startsWith('/api/assets/items') && method === 'GET') {
      requests.push({ method, url })
      const parsed = new URL(url, 'http://localhost')
      const params = parsed.searchParams
      const page = Number(params.get('page') ?? '1')
      const pageSize = Number(params.get('pageSize') ?? '25')
      const search = params.get('search')?.toLowerCase() ?? ''
      const filtered =
        search === ''
          ? assets
          : assets.filter((asset) => asset.name.toLowerCase().includes(search))
      return json({ items: filtered, page, pageSize, totalCount: filtered.length })
    }

    throw new Error(`Unexpected request: ${method} ${url}`)
  })

  return { requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/assets')
})

afterEach(() => vi.restoreAllMocks())

describe('Assets page', () => {
  it('renders assets with the thumbnail fallback and end-of-life date', async () => {
    mockBackend({ assets: [makeAsset(1, { name: 'Cordless drill' })] })
    render(<App />)

    expect(await screen.findByText('Cordless drill')).toBeInTheDocument()
    expect(screen.getByText('Appliances')).toBeInTheDocument()
    expect(screen.getByText('Storage')).toBeInTheDocument()
    expect(screen.getByText('01 Jul 2026')).toBeInTheDocument()
  })

  it('serializes search into the asset list request', async () => {
    const user = userEvent.setup()
    const { requests } = mockBackend()
    render(<App />)

    await screen.findByText('Asset 01')
    await user.type(screen.getByLabelText('Search'), 'Asset 01')

    await waitFor(() =>
      expect(requests.some((request) => request.url.includes('search=Asset+01'))).toBe(
        true,
      ),
    )
  })

  it('opens the create dialog without losing table state', async () => {
    const user = userEvent.setup()
    mockBackend()
    render(<App />)

    await screen.findByText('Asset 01')
    await user.type(screen.getByLabelText('Search'), 'Asset 01')
    await user.click(screen.getByRole('button', { name: 'New asset' }))

    expect(
      await screen.findByRole('dialog', { name: 'New asset' }),
    ).toBeInTheDocument()
    expect(window.location.search).toContain('search=Asset+01')
    expect(window.location.search).toContain('newAsset=true')
  })
})
