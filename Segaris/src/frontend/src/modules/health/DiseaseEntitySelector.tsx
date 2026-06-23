import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import {
  healthApi,
  type DiseaseListQuery,
  type DiseaseSortField,
  type DiseaseSummary,
  type HealthVisibility,
} from '@/app/api/health'
import {
  EntitySelectorDialog,
  type EntityQueryResult,
  type EntitySelectorColumn,
  type EntitySelectorFilter,
  type EntitySelectorLabels,
  type EntitySelectorState,
} from '@/components/entity-selection'
import { Badge } from '@/components/ui'

import { healthKeys } from './contracts'
import { useDiseaseCategories } from './queries'

const visibilities: HealthVisibility[] = ['Public', 'Private']
const defaultSort = { field: 'name', direction: 'asc' } as const

export interface DiseaseEntitySelectorProps {
  medicineVisibility: HealthVisibility
  selectedIds: ReadonlySet<string>
  onToggle: (disease: DiseaseSummary, selected: boolean) => void
  onClose: () => void
}

export function DiseaseEntitySelector({
  medicineVisibility,
  selectedIds,
  onToggle,
  onClose,
}: DiseaseEntitySelectorProps) {
  const { t } = useTranslation('health')
  const categories = useDiseaseCategories()
  const forcedVisibility: HealthVisibility | null =
    medicineVisibility === 'Public' ? 'Public' : null

  const useEntities = (state: EntitySelectorState): EntityQueryResult<DiseaseSummary> => {
    const query = buildDiseaseQuery(state, forcedVisibility)
    const result = useQuery({
      queryKey: healthKeys.diseaseList(query),
      queryFn: ({ signal }) => healthApi.listDiseases(query, signal),
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

  const columns: ReadonlyArray<EntitySelectorColumn<DiseaseSummary>> = [
    {
      id: 'name',
      header: t('diseaseSelector.columns.name'),
      sortField: 'name',
      width: 'minmax(0, 1.6fr)',
      render: (disease) => <strong>{disease.name}</strong>,
    },
    {
      id: 'category',
      header: t('diseaseSelector.columns.category'),
      sortField: 'category',
      render: (disease) => disease.categoryName,
    },
    {
      id: 'medicines',
      header: t('diseaseSelector.columns.medicines'),
      width: 'minmax(0, 0.9fr)',
      render: (disease) =>
        t('diseases.associatedMedicines', {
          count: disease.associatedMedicineCount,
        }),
    },
    {
      id: 'visibility',
      header: t('diseaseSelector.columns.visibility'),
      width: 'minmax(0, 0.9fr)',
      render: (disease) => (
        <Badge tone={disease.visibility === 'Private' ? 'neutral' : 'azure'}>
          {t(`visibility.${disease.visibility}`)}
        </Badge>
      ),
    },
  ]

  const filters: EntitySelectorFilter[] = [
    {
      id: 'category',
      label: t('diseaseSelector.columns.category'),
      options: [
        { value: '', label: t('diseaseSelector.filters.allCategories') },
        ...(categories.data ?? []).map((category) => ({
          value: String(category.id),
          label: category.name,
        })),
      ],
    },
  ]

  if (forcedVisibility == null) {
    filters.push({
      id: 'visibility',
      label: t('diseaseSelector.columns.visibility'),
      options: [
        { value: '', label: t('diseaseSelector.filters.allVisibilities') },
        ...visibilities.map((value) => ({
          value,
          label: t(`visibility.${value}`),
        })),
      ],
    })
  }

  const labels: EntitySelectorLabels = {
    title: t('diseaseSelector.title'),
    eyebrow: t('diseaseSelector.eyebrow'),
    description: t('diseaseSelector.description'),
    searchLabel: t('diseaseSelector.searchLabel'),
    searchPlaceholder: t('diseaseSelector.searchPlaceholder'),
    resultCount: (count) => t('diseaseSelector.count', { count }),
    pageInfo: (start, end, total) =>
      t('diseaseSelector.pageInfo', { start, end, total }),
    clearAll: t('diseaseSelector.clearAll'),
    removeFilter: (label) => t('diseaseSelector.removeFilter', { label }),
    selectAction: t('diseaseSelector.add'),
    currentTag: t('diseaseSelector.added'),
    selectedAction: t('diseaseSelector.added'),
    done: t('diseaseSelector.done'),
    selectionCount: (count) => t('diseaseSelector.selectionCount', { count }),
    cancel: t('diseaseSelector.cancel'),
    close: t('diseaseSelector.close'),
    loading: t('diseaseSelector.loading'),
    refetching: t('diseaseSelector.refetching'),
    error: t('diseaseSelector.error'),
    retry: t('diseaseSelector.retry'),
    empty: t('diseaseSelector.empty'),
    filteredEmpty: t('diseaseSelector.emptyFiltered'),
    clearFilters: t('diseaseSelector.clearFilters'),
    previousPage: t('diseaseSelector.previousPage'),
    nextPage: t('diseaseSelector.nextPage'),
  }

  return (
    <EntitySelectorDialog<DiseaseSummary>
      useEntities={useEntities}
      columns={columns}
      filters={filters}
      labels={labels}
      rowId={(disease) => String(disease.id)}
      selectedIds={selectedIds}
      onToggle={onToggle}
      onSelect={() => undefined}
      onClose={onClose}
      defaultSort={defaultSort}
      pageSize={10}
      width={1000}
    />
  )
}

function buildDiseaseQuery(
  state: EntitySelectorState,
  forcedVisibility: HealthVisibility | null,
): DiseaseListQuery {
  const category = state.filters.category
  const filterVisibility = state.filters.visibility
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: category ? Number(category) : null,
    visibility:
      forcedVisibility ?? ((filterVisibility || null) as HealthVisibility | null),
    page: state.page,
    pageSize: state.pageSize,
    sort: (state.sort?.field ?? defaultSort.field) as DiseaseSortField,
    sortDirection: state.sort?.direction ?? defaultSort.direction,
  }
}
