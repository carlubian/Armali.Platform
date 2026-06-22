import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { MapPin } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import {
  destinationsApi,
  type DestinationListQuery,
  type DestinationSortField,
  type DestinationSummary,
  type DestinationVisibility,
} from '@/app/api/destinations'
import {
  EntitySelectorDialog,
  type EntityQueryResult,
  type EntityReference,
  type EntitySelectorColumn,
  type EntitySelectorFilter,
  type EntitySelectorLabels,
  type EntitySelectorState,
} from '@/components/entity-selection'
import { Badge, type BadgeTone } from '@/components/ui'

import { destinationsKeys } from './contracts'
import { useDestinationCategories } from './queries'

const visibilities: DestinationVisibility[] = ['Public', 'Private']
const defaultSort = { field: 'name', direction: 'asc' } as const

const visibilityTone: Record<DestinationVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

export function destinationReference(destination: {
  name: string
  country?: string | null
  categoryName?: string | null
}): EntityReference {
  const meta = [destination.country, destination.categoryName]
    .filter((value): value is string => value != null && value !== '')
    .join(' · ')
  return {
    primary: destination.name,
    secondary: meta === '' ? undefined : meta,
    icon: <MapPin size={19} aria-hidden="true" />,
  }
}

export interface DestinationEntitySelectorProps {
  currentDestinationId?: number | null
  forcedVisibility?: DestinationVisibility | null
  eyebrow?: ReactNode
  description?: ReactNode
  onSelect: (destination: DestinationSummary) => void
  onClose: () => void
}

export function DestinationEntitySelector({
  currentDestinationId,
  forcedVisibility,
  eyebrow,
  description,
  onSelect,
  onClose,
}: DestinationEntitySelectorProps) {
  const { t } = useTranslation('destinations')
  const categories = useDestinationCategories()

  const useEntities = (
    state: EntitySelectorState,
  ): EntityQueryResult<DestinationSummary> => {
    const query = buildDestinationQuery(state, forcedVisibility ?? null)
    const result = useQuery({
      queryKey: destinationsKeys.destinationList(query),
      queryFn: ({ signal }) => destinationsApi.listDestinations(query, signal),
      placeholderData: keepPreviousData,
    })
    return {
      items: result.data?.items ?? [],
      total: result.data?.totalCount ?? 0,
      isLoading: result.isLoading,
      isFetching: result.isFetching,
      isError: result.isError,
      refetch: () => void result.refetch(),
    }
  }

  const columns: ReadonlyArray<EntitySelectorColumn<DestinationSummary>> = [
    {
      id: 'name',
      header: t('gallery.sort.name'),
      sortField: 'name',
      width: 'minmax(0, 1.7fr)',
      render: (destination) => <strong>{destination.name}</strong>,
    },
    {
      id: 'category',
      header: t('editor.fields.category'),
      sortField: 'category',
      render: (destination) => destination.categoryName,
    },
    {
      id: 'country',
      header: t('editor.fields.country'),
      render: (destination) => destination.country ?? t('common.none'),
    },
    {
      id: 'schengen',
      header: t('editor.fields.isSchengenArea'),
      width: 'minmax(0, 0.9fr)',
      render: (destination) =>
        destination.isSchengenArea ? (
          <Badge tone="azure">{t('gallery.badge.schengen')}</Badge>
        ) : (
          t('common.none')
        ),
    },
    {
      id: 'visibility',
      header: t('editor.fields.visibility'),
      width: 'minmax(0, 0.9fr)',
      render: (destination) => (
        <Badge tone={visibilityTone[destination.visibility]}>
          {t(`visibility.${destination.visibility}`)}
        </Badge>
      ),
    },
  ]

  const filters: EntitySelectorFilter[] = [
    {
      id: 'category',
      label: t('gallery.filters.category'),
      options: [
        { value: '', label: t('selector.filters.allCategories') },
        ...(categories.data ?? []).map((category) => ({
          value: String(category.id),
          label: category.name,
        })),
      ],
    },
    {
      id: 'schengen',
      label: t('gallery.filters.schengen'),
      options: [
        { value: '', label: t('gallery.filters.anyOption') },
        { value: 'true', label: t('gallery.filters.schengenOnly') },
        { value: 'false', label: t('gallery.filters.nonSchengenOnly') },
      ],
    },
  ]

  if (forcedVisibility == null) {
    filters.push({
      id: 'visibility',
      label: t('editor.fields.visibility'),
      options: [
        { value: '', label: t('selector.filters.allVisibilities') },
        ...visibilities.map((value) => ({
          value,
          label: t(`visibility.${value}`),
        })),
      ],
    })
  }

  const labels: EntitySelectorLabels = {
    title: t('selector.title'),
    eyebrow: eyebrow ?? t('selector.eyebrow'),
    description: description ?? t('selector.description'),
    searchLabel: t('selector.searchLabel'),
    searchPlaceholder: t('selector.searchPlaceholder'),
    resultCount: (count) => t('selector.count', { count }),
    pageInfo: (start, end, total) => t('selector.pageInfo', { start, end, total }),
    clearAll: t('selector.clearAll'),
    removeFilter: (label) => t('selector.removeFilter', { label }),
    selectAction: t('selector.select'),
    currentTag: t('selector.current'),
    cancel: t('selector.cancel'),
    close: t('selector.close'),
    loading: t('selector.loading'),
    refetching: t('selector.refetching'),
    error: t('selector.error'),
    retry: t('selector.retry'),
    empty: t('selector.empty'),
    filteredEmpty: t('selector.emptyFiltered'),
    clearFilters: t('selector.clearFilters'),
    previousPage: t('selector.previousPage'),
    nextPage: t('selector.nextPage'),
  }

  return (
    <EntitySelectorDialog<DestinationSummary>
      useEntities={useEntities}
      columns={columns}
      filters={filters}
      labels={labels}
      rowId={(destination) => String(destination.id)}
      currentId={currentDestinationId == null ? null : String(currentDestinationId)}
      onSelect={onSelect}
      onClose={onClose}
      defaultSort={defaultSort}
      pageSize={10}
      width={1000}
    />
  )
}

function buildDestinationQuery(
  state: EntitySelectorState,
  forcedVisibility: DestinationVisibility | null,
): DestinationListQuery {
  const category = state.filters.category
  const schengen = state.filters.schengen
  const filterVisibility = state.filters.visibility
  const visibility = forcedVisibility ?? (filterVisibility ? filterVisibility : null)

  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: category ? Number(category) : null,
    isSchengenArea: schengen === '' ? null : schengen === 'true',
    visibility: visibility as DestinationVisibility | null,
    page: state.page,
    pageSize: state.pageSize,
    sort: (state.sort?.field ?? defaultSort.field) as DestinationSortField,
    sortDirection: state.sort?.direction ?? defaultSort.direction,
  }
}
