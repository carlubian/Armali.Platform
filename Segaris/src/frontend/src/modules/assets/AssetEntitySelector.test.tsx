import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { I18nextProvider } from 'react-i18next'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { assetsApi, type AssetListQuery, type AssetSummary } from '@/app/api/assets'
import { i18n } from '@/app/i18n/i18n'

import { AssetEntitySelector } from './AssetEntitySelector'

function makeAsset(id: number, overrides: Partial<AssetSummary> = {}): AssetSummary {
  return {
    id,
    name: `Asset ${id}`,
    code: `A-${id}`,
    categoryId: 1,
    categoryName: 'Appliances',
    locationId: 1,
    locationName: 'Storage',
    status: 'Active',
    expectedEndOfLifeDate: null,
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina',
    ...overrides,
  }
}

interface RenderOptions {
  assets?: AssetSummary[]
  forcedVisibility?: 'Public' | 'Private' | null
  currentAssetId?: string | null
}

function renderSelector(options: RenderOptions = {}) {
  const assets = options.assets ?? [makeAsset(1), makeAsset(2)]
  const onSelect = vi.fn()
  const onClose = vi.fn()

  const listAssets = vi
    .spyOn(assetsApi, 'listAssets')
    .mockImplementation((query: AssetListQuery = {}) =>
      Promise.resolve({
        items: assets,
        page: query.page ?? 1,
        pageSize: query.pageSize ?? 10,
        totalCount: assets.length,
      }),
    )
  vi.spyOn(assetsApi, 'categories').mockResolvedValue([
    { id: 1, name: 'Appliances', sortOrder: 0 },
  ])
  vi.spyOn(assetsApi, 'locations').mockResolvedValue([
    { id: 1, name: 'Storage', sortOrder: 0 },
  ])

  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  render(
    <QueryClientProvider client={client}>
      <I18nextProvider i18n={i18n}>
        <AssetEntitySelector
          currentAssetId={options.currentAssetId ?? null}
          forcedVisibility={options.forcedVisibility ?? null}
          onSelect={onSelect}
          onClose={onClose}
        />
      </I18nextProvider>
    </QueryClientProvider>,
  )

  return { listAssets, onSelect, onClose }
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('AssetEntitySelector', () => {
  it('lists assets from the API and selects one', async () => {
    const user = userEvent.setup()
    const { onSelect } = renderSelector()

    await screen.findByText('Asset 1')
    const row = screen.getByText('Asset 2').closest('[role="row"]') as HTMLElement
    await user.click(within(row).getByRole('button', { name: 'Select' }))

    expect(onSelect).toHaveBeenCalledWith(expect.objectContaining({ id: 2 }))
  })

  it('forces Public visibility and hides the visibility filter when requested', async () => {
    const { listAssets } = renderSelector({ forcedVisibility: 'Public' })

    await screen.findByText('Asset 1')
    expect(listAssets).toHaveBeenLastCalledWith(
      expect.objectContaining({ visibility: 'Public' }),
      expect.anything(),
    )
    expect(screen.queryByRole('combobox', { name: 'Visibility' })).toBeNull()
  })

  it('offers the visibility filter when no visibility is forced', async () => {
    renderSelector()
    await screen.findByText('Asset 1')
    expect(screen.getByRole('combobox', { name: 'Visibility' })).toBeInTheDocument()
  })

  it('maps sortable headers to asset sort fields', async () => {
    const user = userEvent.setup()
    const { listAssets } = renderSelector()

    await screen.findByText('Asset 1')
    await user.click(screen.getByRole('button', { name: 'Code' }))

    await waitFor(() =>
      expect(listAssets).toHaveBeenLastCalledWith(
        expect.objectContaining({ sort: 'code', sortDirection: 'asc' }),
        expect.anything(),
      ),
    )
  })

  it('marks the current asset instead of offering Select', async () => {
    renderSelector({ currentAssetId: '1' })
    await screen.findByText('Asset 1')
    expect(screen.getByText('Linked')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: 'Select' })).toHaveLength(1)
  })
})
