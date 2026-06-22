import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { I18nextProvider } from 'react-i18next'
import { afterEach, describe, expect, it, vi } from 'vitest'

import {
  destinationsApi,
  type DestinationListQuery,
  type DestinationSummary,
} from '@/app/api/destinations'
import { i18n } from '@/app/i18n/i18n'

import { DestinationEntitySelector } from './DestinationEntitySelector'

function makeDestination(
  id: number,
  overrides: Partial<DestinationSummary> = {},
): DestinationSummary {
  return {
    id,
    name: `Destination ${id}`,
    categoryId: 1,
    categoryName: 'City break',
    country: 'Spain',
    isSchengenArea: true,
    averagePlaceRating: null,
    ratedPlaceCount: 0,
    visibility: 'Public',
    thumbnail: { attachmentId: null, url: null, source: 'placeholder' },
    creatorId: 7,
    creatorName: 'Marina',
    ...overrides,
  }
}

interface RenderOptions {
  destinations?: DestinationSummary[]
  forcedVisibility?: 'Public' | 'Private' | null
  currentDestinationId?: number | null
}

function renderSelector(options: RenderOptions = {}) {
  const destinations = options.destinations ?? [makeDestination(1), makeDestination(2)]
  const onSelect = vi.fn()
  const onClose = vi.fn()

  const listDestinations = vi
    .spyOn(destinationsApi, 'listDestinations')
    .mockImplementation((query: DestinationListQuery = {}) =>
      Promise.resolve({
        items: destinations,
        page: query.page ?? 1,
        pageSize: query.pageSize ?? 10,
        totalCount: destinations.length,
      }),
    )
  vi.spyOn(destinationsApi, 'categories').mockResolvedValue([
    { id: 1, name: 'City break', sortOrder: 0 },
  ])

  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  render(
    <QueryClientProvider client={client}>
      <I18nextProvider i18n={i18n}>
        <DestinationEntitySelector
          currentDestinationId={options.currentDestinationId ?? null}
          forcedVisibility={options.forcedVisibility ?? null}
          onSelect={onSelect}
          onClose={onClose}
        />
      </I18nextProvider>
    </QueryClientProvider>,
  )

  return { listDestinations, onSelect }
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('DestinationEntitySelector', () => {
  it('lists destinations and selects one', async () => {
    const user = userEvent.setup()
    const { onSelect } = renderSelector()

    await screen.findByText('Destination 1')
    const row = screen.getByText('Destination 2').closest('[role="row"]') as HTMLElement
    await user.click(within(row).getByRole('button', { name: 'Select' }))

    expect(onSelect).toHaveBeenCalledWith(expect.objectContaining({ id: 2 }))
  })

  it('forces Public visibility and hides the visibility filter when requested', async () => {
    const { listDestinations } = renderSelector({ forcedVisibility: 'Public' })

    await screen.findByText('Destination 1')
    expect(listDestinations).toHaveBeenLastCalledWith(
      expect.objectContaining({ visibility: 'Public' }),
      expect.anything(),
    )
    expect(screen.queryByRole('combobox', { name: 'Visibility' })).toBeNull()
  })

  it('offers the visibility filter when no visibility is forced', async () => {
    renderSelector()
    await screen.findByText('Destination 1')
    expect(screen.getByRole('combobox', { name: 'Visibility' })).toBeInTheDocument()
  })

  it('marks the current destination instead of offering Select', async () => {
    renderSelector({ currentDestinationId: 1 })
    await screen.findByText('Destination 1')
    expect(screen.getByText('Linked')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: 'Select' })).toHaveLength(1)
  })
})
