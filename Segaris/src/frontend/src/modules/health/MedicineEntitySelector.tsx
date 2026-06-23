import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'

import {
  healthApi,
  type HealthVisibility,
  type MedicineListQuery,
  type MedicineSortField,
  type MedicineSummary,
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
import { useMedicineCategories } from './queries'

const visibilities: HealthVisibility[] = ['Public', 'Private']
const defaultSort = { field: 'name', direction: 'asc' } as const

export interface MedicineEntitySelectorProps {
  /**
   * The owning disease's visibility. A public disease may only be associated
   * with public medicines, so its selector forces the public list; a private
   * disease may associate any accessible medicine.
   */
  diseaseVisibility: HealthVisibility
  /** Medicine ids (as strings) already in the working set. */
  selectedIds: ReadonlySet<string>
  onToggle: (medicine: MedicineSummary, selected: boolean) => void
  onClose: () => void
}

export function MedicineEntitySelector({
  diseaseVisibility,
  selectedIds,
  onToggle,
  onClose,
}: MedicineEntitySelectorProps) {
  const { t } = useTranslation('health')
  const categories = useMedicineCategories()
  const forcedVisibility: HealthVisibility | null =
    diseaseVisibility === 'Public' ? 'Public' : null

  const useEntities = (
    state: EntitySelectorState,
  ): EntityQueryResult<MedicineSummary> => {
    const query = buildMedicineQuery(state, forcedVisibility)
    const result = useQuery({
      queryKey: healthKeys.medicineList(query),
      queryFn: ({ signal }) => healthApi.listMedicines(query, signal),
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

  const columns: ReadonlyArray<EntitySelectorColumn<MedicineSummary>> = [
    {
      id: 'name',
      header: t('medicineSelector.columns.name'),
      sortField: 'name',
      width: 'minmax(0, 1.6fr)',
      render: (medicine) => <strong>{medicine.name}</strong>,
    },
    {
      id: 'category',
      header: t('medicineSelector.columns.category'),
      sortField: 'category',
      render: (medicine) => medicine.categoryName,
    },
    {
      id: 'prescription',
      header: t('medicineSelector.columns.prescription'),
      width: 'minmax(0, 0.9fr)',
      render: (medicine) =>
        medicine.requiresPrescription ? (
          <Badge tone="gold">{t('medicineSelector.prescription.required')}</Badge>
        ) : (
          t('medicineSelector.prescription.notRequired')
        ),
    },
    {
      id: 'visibility',
      header: t('medicineSelector.columns.visibility'),
      width: 'minmax(0, 0.9fr)',
      render: (medicine) => (
        <Badge tone={medicine.visibility === 'Private' ? 'neutral' : 'azure'}>
          {t(`visibility.${medicine.visibility}`)}
        </Badge>
      ),
    },
  ]

  const filters: EntitySelectorFilter[] = [
    {
      id: 'category',
      label: t('medicineSelector.columns.category'),
      options: [
        { value: '', label: t('medicineSelector.filters.allCategories') },
        ...(categories.data ?? []).map((category) => ({
          value: String(category.id),
          label: category.name,
        })),
      ],
    },
    {
      id: 'prescription',
      label: t('medicineSelector.filters.prescription'),
      options: [
        { value: '', label: t('medicineSelector.filters.anyPrescription') },
        { value: 'true', label: t('medicineSelector.filters.requiresPrescription') },
        { value: 'false', label: t('medicineSelector.filters.noPrescription') },
      ],
    },
  ]

  if (forcedVisibility == null) {
    filters.push({
      id: 'visibility',
      label: t('medicineSelector.columns.visibility'),
      options: [
        { value: '', label: t('medicineSelector.filters.allVisibilities') },
        ...visibilities.map((value) => ({
          value,
          label: t(`visibility.${value}`),
        })),
      ],
    })
  }

  const labels: EntitySelectorLabels = {
    title: t('medicineSelector.title'),
    eyebrow: t('medicineSelector.eyebrow'),
    description: t('medicineSelector.description'),
    searchLabel: t('medicineSelector.searchLabel'),
    searchPlaceholder: t('medicineSelector.searchPlaceholder'),
    resultCount: (count) => t('medicineSelector.count', { count }),
    pageInfo: (start, end, total) =>
      t('medicineSelector.pageInfo', { start, end, total }),
    clearAll: t('medicineSelector.clearAll'),
    removeFilter: (label) => t('medicineSelector.removeFilter', { label }),
    selectAction: t('medicineSelector.add'),
    currentTag: t('medicineSelector.added'),
    selectedAction: t('medicineSelector.added'),
    done: t('medicineSelector.done'),
    selectionCount: (count) => t('medicineSelector.selectionCount', { count }),
    cancel: t('medicineSelector.cancel'),
    close: t('medicineSelector.close'),
    loading: t('medicineSelector.loading'),
    refetching: t('medicineSelector.refetching'),
    error: t('medicineSelector.error'),
    retry: t('medicineSelector.retry'),
    empty: t('medicineSelector.empty'),
    filteredEmpty: t('medicineSelector.emptyFiltered'),
    clearFilters: t('medicineSelector.clearFilters'),
    previousPage: t('medicineSelector.previousPage'),
    nextPage: t('medicineSelector.nextPage'),
  }

  return (
    <EntitySelectorDialog<MedicineSummary>
      useEntities={useEntities}
      columns={columns}
      filters={filters}
      labels={labels}
      rowId={(medicine) => String(medicine.id)}
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

function buildMedicineQuery(
  state: EntitySelectorState,
  forcedVisibility: HealthVisibility | null,
): MedicineListQuery {
  const category = state.filters.category
  const prescription = state.filters.prescription
  const filterVisibility = state.filters.visibility
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: category ? Number(category) : null,
    requiresPrescription: prescription === '' ? null : prescription === 'true',
    visibility:
      forcedVisibility ?? ((filterVisibility || null) as HealthVisibility | null),
    page: state.page,
    pageSize: state.pageSize,
    sort: (state.sort?.field ?? defaultSort.field) as MedicineSortField,
    sortDirection: state.sort?.direction ?? defaultSort.direction,
  }
}
