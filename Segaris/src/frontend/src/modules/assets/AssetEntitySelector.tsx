import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Boxes } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import {
  assetsApi,
  type AssetListQuery,
  type AssetSortField,
  type AssetStatus,
  type AssetSummary,
  type AssetVisibility,
} from '@/app/api/assets'
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

import { assetsKeys } from './contracts'
import { useAssetCategories, useAssetLocations } from './queries'

import './AssetEntitySelector.css'

const statuses: AssetStatus[] = ['Active', 'Stored', 'Retired']
const visibilities: AssetVisibility[] = ['Public', 'Private']

const statusTone: Record<AssetStatus, BadgeTone> = {
  Active: 'success',
  Stored: 'gold',
  Retired: 'neutral',
}

const visibilityTone: Record<AssetVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

const defaultSort = { field: 'name', direction: 'asc' } as const

/**
 * Maps an asset onto the display metadata used by `EntityReferenceField` when it
 * shows a selected link. Keeps Asset-specific presentation in this adapter.
 */
export function assetReference(asset: AssetSummary): EntityReference {
  const meta = [asset.code, asset.categoryName, asset.locationName]
    .filter((value): value is string => value != null && value !== '')
    .join(' · ')
  return {
    primary: asset.name,
    secondary: meta === '' ? undefined : meta,
    icon: <Boxes size={19} aria-hidden="true" />,
  }
}

export interface AssetEntitySelectorProps {
  /** The currently linked asset, marked as current in the table. */
  currentAssetId?: string | null
  /**
   * When set, the selector lists only assets of this visibility and hides the
   * visibility filter. Maintenance forces `Public` for public tasks; the Assets
   * API stays the authoritative integrity boundary.
   */
  forcedVisibility?: AssetVisibility | null
  /** Overrides the default eyebrow/description for a specific consumer. */
  eyebrow?: ReactNode
  description?: ReactNode
  onSelect: (asset: AssetSummary) => void
  onClose: () => void
}

/**
 * Asset-specific adapter around the shared {@link EntitySelectorDialog}. It owns
 * the Asset list query, catalog-backed filters, table columns, sort-field
 * mapping, and the public/private visibility constraint, so consumers only wire
 * a current id and a selection callback.
 */
export function AssetEntitySelector({
  currentAssetId,
  forcedVisibility,
  eyebrow,
  description,
  onSelect,
  onClose,
}: AssetEntitySelectorProps) {
  const { t } = useTranslation('assets')
  const categories = useAssetCategories()
  const locations = useAssetLocations()

  const useEntities = (state: EntitySelectorState): EntityQueryResult<AssetSummary> => {
    const query = buildAssetQuery(state, forcedVisibility ?? null)
    const result = useQuery({
      queryKey: assetsKeys.assetList(query),
      queryFn: ({ signal }) => assetsApi.listAssets(query, signal),
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

  const columns: ReadonlyArray<EntitySelectorColumn<AssetSummary>> = [
    {
      id: 'name',
      header: t('assets.columns.name'),
      sortField: 'name',
      width: 'minmax(0, 1.8fr)',
      render: (asset) => <span className="seg-asset-sel__name">{asset.name}</span>,
    },
    {
      id: 'code',
      header: t('assets.columns.code'),
      sortField: 'code',
      width: 'minmax(0, 0.8fr)',
      render: (asset) => asset.code ?? t('common.none'),
    },
    {
      id: 'category',
      header: t('assets.columns.category'),
      sortField: 'category',
      render: (asset) => asset.categoryName,
    },
    {
      id: 'location',
      header: t('assets.columns.location'),
      sortField: 'location',
      render: (asset) => asset.locationName,
    },
    {
      id: 'status',
      header: t('assets.columns.status'),
      sortField: 'status',
      width: 'minmax(0, 0.9fr)',
      render: (asset) => (
        <Badge tone={statusTone[asset.status]} dot>
          {t(`assets.status.${asset.status}`)}
        </Badge>
      ),
    },
    {
      id: 'visibility',
      header: t('assets.columns.visibility'),
      sortField: 'visibility',
      width: 'minmax(0, 0.9fr)',
      render: (asset) => (
        <Badge tone={visibilityTone[asset.visibility]}>
          {t(`assets.visibility.${asset.visibility}`)}
        </Badge>
      ),
    },
  ]

  const catalogOptions = (
    items: ReadonlyArray<{ id: number; name: string }> | undefined,
    allLabel: string,
  ) => [
    { value: '', label: allLabel },
    ...(items ?? []).map((item) => ({ value: String(item.id), label: item.name })),
  ]

  const filters: EntitySelectorFilter[] = [
    {
      id: 'status',
      label: t('assets.filters.status'),
      options: [
        { value: '', label: t('selector.filters.allStatuses') },
        ...statuses.map((value) => ({ value, label: t(`assets.status.${value}`) })),
      ],
    },
    {
      id: 'category',
      label: t('assets.filters.category'),
      options: catalogOptions(categories.data, t('selector.filters.allCategories')),
    },
    {
      id: 'location',
      label: t('assets.filters.location'),
      options: catalogOptions(locations.data, t('selector.filters.allLocations')),
    },
  ]
  if (forcedVisibility == null) {
    filters.push({
      id: 'visibility',
      label: t('assets.filters.visibility'),
      options: [
        { value: '', label: t('selector.filters.allVisibilities') },
        ...visibilities.map((value) => ({
          value,
          label: t(`assets.visibility.${value}`),
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
    <EntitySelectorDialog<AssetSummary>
      useEntities={useEntities}
      columns={columns}
      filters={filters}
      labels={labels}
      rowId={(asset) => String(asset.id)}
      currentId={currentAssetId ?? null}
      onSelect={onSelect}
      onClose={onClose}
      defaultSort={defaultSort}
      pageSize={10}
      width={1000}
    />
  )
}

function buildAssetQuery(
  state: EntitySelectorState,
  forcedVisibility: AssetVisibility | null,
): AssetListQuery {
  const status = state.filters.status
  const category = state.filters.category
  const location = state.filters.location
  const filterVisibility = state.filters.visibility
  const visibility = forcedVisibility ?? (filterVisibility ? filterVisibility : null)

  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    status: status ? (status as AssetStatus) : null,
    category: category ? Number(category) : null,
    location: location ? Number(location) : null,
    visibility: visibility as AssetVisibility | null,
    page: state.page,
    pageSize: state.pageSize,
    sort: (state.sort?.field ?? defaultSort.field) as AssetSortField,
    sortDirection: state.sort?.direction ?? defaultSort.direction,
  }
}
