import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Package } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import {
  inventoryApi,
  type InventoryItemListQuery,
  type InventoryItemSortField,
  type InventoryItemStatus,
  type InventoryItemSummary,
  type InventoryVisibility,
} from '@/app/api/inventory'
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
import { inventoryKeys, useInventoryCategories, useInventoryLocations } from '@/modules/inventory/queries'

const statuses: InventoryItemStatus[] = ['Candidate', 'Active', 'Deprecated']
const visibilities: InventoryVisibility[] = ['Public', 'Private']
const defaultSort = { field: 'name', direction: 'asc' } as const

const statusTone: Record<InventoryItemStatus, BadgeTone> = {
  Candidate: 'gold',
  Active: 'success',
  Deprecated: 'neutral',
}

const visibilityTone: Record<InventoryVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

export function inventoryItemReference(item: {
  name: string
  categoryName?: string | null
  locationName?: string | null
}): EntityReference {
  const meta = [item.categoryName, item.locationName]
    .filter((value): value is string => value != null && value !== '')
    .join(' · ')
  return {
    primary: item.name,
    secondary: meta === '' ? undefined : meta,
    icon: <Package size={19} aria-hidden="true" />,
  }
}

export interface InventoryItemEntitySelectorProps {
  currentItemId?: number | null
  forcedVisibility?: InventoryVisibility | null
  eyebrow?: ReactNode
  description?: ReactNode
  onSelect: (item: InventoryItemSummary) => void
  onClose: () => void
}

export function InventoryItemEntitySelector({
  currentItemId,
  forcedVisibility,
  eyebrow,
  description,
  onSelect,
  onClose,
}: InventoryItemEntitySelectorProps) {
  const { t } = useTranslation(['recipes', 'inventory'])
  const categories = useInventoryCategories()
  const locations = useInventoryLocations()

  const useEntities = (
    state: EntitySelectorState,
  ): EntityQueryResult<InventoryItemSummary> => {
    const query = buildItemQuery(state, forcedVisibility ?? null)
    const result = useQuery({
      queryKey: inventoryKeys.itemList(query),
      queryFn: ({ signal }) => inventoryApi.listItems(query, signal),
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

  const columns: ReadonlyArray<EntitySelectorColumn<InventoryItemSummary>> = [
    {
      id: 'name',
      header: t('inventory:items.columns.name'),
      sortField: 'name',
      width: 'minmax(0, 1.7fr)',
      render: (item) => <strong>{item.name}</strong>,
    },
    {
      id: 'category',
      header: t('inventory:items.columns.category'),
      sortField: 'category',
      render: (item) => item.categoryName,
    },
    {
      id: 'location',
      header: t('inventory:items.columns.location'),
      sortField: 'location',
      render: (item) => item.locationName,
    },
    {
      id: 'stock',
      header: t('inventory:items.columns.currentStock'),
      sortField: 'currentStock',
      width: 'minmax(0, 0.8fr)',
      render: (item) => item.currentStock,
    },
    {
      id: 'status',
      header: t('inventory:items.columns.status'),
      sortField: 'status',
      width: 'minmax(0, 0.9fr)',
      render: (item) => (
        <Badge tone={statusTone[item.status]} dot>
          {t(`inventory:items.status.${item.status}`)}
        </Badge>
      ),
    },
    {
      id: 'visibility',
      header: t('inventory:items.columns.visibility'),
      sortField: 'visibility',
      width: 'minmax(0, 0.9fr)',
      render: (item) => (
        <Badge tone={visibilityTone[item.visibility]}>
          {t(`inventory:items.visibility.${item.visibility}`)}
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
      label: t('inventory:items.filters.status'),
      options: [
        { value: '', label: t('recipes:selector.filters.allStatuses') },
        ...statuses.map((value) => ({
          value,
          label: t(`inventory:items.status.${value}`),
        })),
      ],
    },
    {
      id: 'category',
      label: t('inventory:items.filters.category'),
      options: catalogOptions(
        categories.data,
        t('recipes:selector.filters.allCategories'),
      ),
    },
    {
      id: 'location',
      label: t('inventory:items.filters.location'),
      options: catalogOptions(
        locations.data,
        t('recipes:selector.filters.allLocations'),
      ),
    },
  ]

  if (forcedVisibility == null) {
    filters.push({
      id: 'visibility',
      label: t('inventory:items.filters.visibility'),
      options: [
        { value: '', label: t('recipes:selector.filters.allVisibilities') },
        ...visibilities.map((value) => ({
          value,
          label: t(`inventory:items.visibility.${value}`),
        })),
      ],
    })
  }

  const labels: EntitySelectorLabels = {
    title: t('recipes:selector.title'),
    eyebrow: eyebrow ?? t('recipes:selector.eyebrow'),
    description: description ?? t('recipes:selector.description'),
    searchLabel: t('recipes:selector.searchLabel'),
    searchPlaceholder: t('recipes:selector.searchPlaceholder'),
    resultCount: (count) => t('recipes:selector.count', { count }),
    pageInfo: (start, end, total) =>
      t('recipes:selector.pageInfo', { start, end, total }),
    clearAll: t('recipes:selector.clearAll'),
    removeFilter: (label) => t('recipes:selector.removeFilter', { label }),
    selectAction: t('recipes:selector.select'),
    currentTag: t('recipes:selector.current'),
    cancel: t('recipes:selector.cancel'),
    close: t('recipes:selector.close'),
    loading: t('recipes:selector.loading'),
    refetching: t('recipes:selector.refetching'),
    error: t('recipes:selector.error'),
    retry: t('recipes:selector.retry'),
    empty: t('recipes:selector.empty'),
    filteredEmpty: t('recipes:selector.emptyFiltered'),
    clearFilters: t('recipes:selector.clearFilters'),
    previousPage: t('recipes:selector.previousPage'),
    nextPage: t('recipes:selector.nextPage'),
  }

  return (
    <EntitySelectorDialog<InventoryItemSummary>
      useEntities={useEntities}
      columns={columns}
      filters={filters}
      labels={labels}
      rowId={(item) => String(item.id)}
      currentId={currentItemId == null ? null : String(currentItemId)}
      onSelect={onSelect}
      onClose={onClose}
      defaultSort={defaultSort}
      pageSize={10}
      width={1080}
    />
  )
}

function buildItemQuery(
  state: EntitySelectorState,
  forcedVisibility: InventoryVisibility | null,
): InventoryItemListQuery {
  const status = state.filters.status
  const category = state.filters.category
  const location = state.filters.location
  const filterVisibility = state.filters.visibility
  const visibility = forcedVisibility ?? (filterVisibility ? filterVisibility : null)

  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    status: status ? (status as InventoryItemStatus) : null,
    category: category ? Number(category) : null,
    location: location ? Number(location) : null,
    visibility: visibility as InventoryVisibility | null,
    page: state.page,
    pageSize: state.pageSize,
    sort: (state.sort?.field ?? defaultSort.field) as InventoryItemSortField,
    sortDirection: state.sort?.direction ?? defaultSort.direction,
  }
}
