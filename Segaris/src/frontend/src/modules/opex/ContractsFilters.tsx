import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type {
  OpexContractStatus,
  OpexExpectedFrequency,
  OpexMovementType,
  OpexVisibility,
} from '@/app/api/opex'
import { Button, Input, Select } from '@/components/ui'

import {
  activeFilterCount,
  type ContractsFilterPatch,
  type ContractsState,
} from './contractsState'
import {
  useOpexCategories,
  useCostCenters,
  useCurrencies,
  useSuppliers,
} from './queries'

interface ContractsFiltersProps {
  state: ContractsState
  onChange: (patch: ContractsFilterPatch) => void
  onClear: () => void
}

interface Chip {
  key: string
  label: string
  clear: ContractsFilterPatch
}

export function ContractsFilters({ state, onChange, onClear }: ContractsFiltersProps) {
  const { t } = useTranslation('opex')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()

  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)
  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const categories = useOpexCategories()
  const suppliers = useSuppliers()
  const costCenters = useCostCenters()
  const currencies = useCurrencies()

  const anyOption = { value: '', label: t('contracts.filters.anyOption') }
  const idOptions = (
    items: ReadonlyArray<{ id: number; name?: string; code?: string }> | undefined,
    useCode = false,
  ) => [
    anyOption,
    ...(items ?? []).map((item) => ({
      value: String(item.id),
      label: useCode ? (item.code ?? '') : (item.name ?? ''),
    })),
  ]

  const numberPatch = (
    key: 'category' | 'supplier' | 'costCenter' | 'currency',
    value: string,
  ) => onChange({ [key]: value === '' ? null : Number(value) })

  const count = activeFilterCount(state)
  const chips = buildChips(state, {
    categoryName: nameById(categories.data, state.category),
    supplierName: nameById(suppliers.data, state.supplier),
    costCenterName: nameById(costCenters.data, state.costCenter),
    currencyCode: codeById(currencies.data, state.currency),
    type: state.type === '' ? '' : t(`contracts.type.${state.type}`),
    status: state.status === '' ? '' : t(`contracts.status.${state.status}`),
    frequency:
      state.frequency === '' ? '' : t(`contracts.frequency.${state.frequency}`),
    visibility:
      state.visibility === '' ? '' : t(`contracts.visibility.${state.visibility}`),
    labels: {
      search: (value: string) => t('contracts.filters.chip.search', { value }),
      mine: t('contracts.filters.chip.mine'),
    },
  })

  return (
    <div className="seg-opex__filters">
      <div className="seg-opex__filters-primary">
        <Input
          className="seg-opex__search"
          label={t('contracts.filters.searchLabel')}
          placeholder={t('contracts.filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <label className="seg-opex__field">
          <span>{t('contracts.filters.type')}</span>
          <Select
            value={state.type}
            onChange={(event) =>
              onChange({ type: event.target.value as OpexMovementType | '' })
            }
            options={[
              anyOption,
              { value: 'Income', label: t('contracts.type.Income') },
              { value: 'Expense', label: t('contracts.type.Expense') },
            ]}
          />
        </label>
        <label className="seg-opex__field">
          <span>{t('contracts.filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as OpexContractStatus | '' })
            }
            options={[
              anyOption,
              { value: 'Planning', label: t('contracts.status.Planning') },
              { value: 'Active', label: t('contracts.status.Active') },
              { value: 'OnHold', label: t('contracts.status.OnHold') },
              { value: 'Closed', label: t('contracts.status.Closed') },
            ]}
          />
        </label>
        <label className="seg-opex__field">
          <span>{t('contracts.filters.frequency')}</span>
          <Select
            value={state.frequency}
            onChange={(event) =>
              onChange({ frequency: event.target.value as OpexExpectedFrequency | '' })
            }
            options={[
              anyOption,
              { value: 'None', label: t('contracts.frequency.None') },
              { value: 'Weekly', label: t('contracts.frequency.Weekly') },
              { value: 'Monthly', label: t('contracts.frequency.Monthly') },
              { value: 'Quarterly', label: t('contracts.frequency.Quarterly') },
              { value: 'SemiAnnual', label: t('contracts.frequency.SemiAnnual') },
              { value: 'Annual', label: t('contracts.frequency.Annual') },
              { value: 'Irregular', label: t('contracts.frequency.Irregular') },
            ]}
          />
        </label>
        <Button
          variant="outline"
          size="sm"
          iconLeft={<SlidersHorizontal size={15} />}
          aria-expanded={expanded}
          aria-controls={secondaryId}
          onClick={() => setExpanded((current) => !current)}
        >
          {expanded ? t('contracts.filters.fewer') : t('contracts.filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-opex__filters-secondary" id={secondaryId}>
          <label className="seg-opex__field">
            <span>{t('contracts.filters.category')}</span>
            <Select
              value={state.category == null ? '' : String(state.category)}
              onChange={(event) => numberPatch('category', event.target.value)}
              options={idOptions(categories.data)}
            />
          </label>
          <label className="seg-opex__field">
            <span>{t('contracts.filters.supplier')}</span>
            <Select
              value={state.supplier == null ? '' : String(state.supplier)}
              onChange={(event) => numberPatch('supplier', event.target.value)}
              options={idOptions(suppliers.data)}
            />
          </label>
          <label className="seg-opex__field">
            <span>{t('contracts.filters.costCenter')}</span>
            <Select
              value={state.costCenter == null ? '' : String(state.costCenter)}
              onChange={(event) => numberPatch('costCenter', event.target.value)}
              options={idOptions(costCenters.data)}
            />
          </label>
          <label className="seg-opex__field">
            <span>{t('contracts.filters.currency')}</span>
            <Select
              value={state.currency == null ? '' : String(state.currency)}
              onChange={(event) => numberPatch('currency', event.target.value)}
              options={idOptions(currencies.data, true)}
            />
          </label>
          <label className="seg-opex__field">
            <span>{t('contracts.filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({ visibility: event.target.value as OpexVisibility | '' })
              }
              options={[
                anyOption,
                { value: 'Public', label: t('contracts.visibility.Public') },
                { value: 'Private', label: t('contracts.visibility.Private') },
              ]}
            />
          </label>
          <label className="seg-opex__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('contracts.filters.myContracts')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-opex__chips"
          role="group"
          aria-label={t('contracts.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-opex__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('contracts.filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-opex__chip-clear" onClick={onClear}>
            {t('contracts.filters.clearAll')}
          </button>
        </div>
      )}
    </div>
  )
}

function nameById(
  items: ReadonlyArray<{ id: number; name: string }> | undefined,
  id: number | null,
): string {
  if (id == null) return ''
  return items?.find((item) => item.id === id)?.name ?? String(id)
}

function codeById(
  items: ReadonlyArray<{ id: number; code: string }> | undefined,
  id: number | null,
): string {
  if (id == null) return ''
  return items?.find((item) => item.id === id)?.code ?? String(id)
}

interface ChipLabels {
  categoryName: string
  supplierName: string
  costCenterName: string
  currencyCode: string
  type: string
  status: string
  frequency: string
  visibility: string
  labels: {
    search: (value: string) => string
    mine: string
  }
}

function buildChips(state: ContractsState, resolved: ChipLabels): Chip[] {
  const chips: Chip[] = []
  if (state.search.trim() !== '') {
    chips.push({
      key: 'search',
      label: resolved.labels.search(state.search.trim()),
      clear: { search: '' },
    })
  }
  if (state.type !== '') {
    chips.push({ key: 'type', label: resolved.type, clear: { type: '' } })
  }
  if (state.status !== '') {
    chips.push({ key: 'status', label: resolved.status, clear: { status: '' } })
  }
  if (state.frequency !== '') {
    chips.push({
      key: 'frequency',
      label: resolved.frequency,
      clear: { frequency: '' },
    })
  }
  if (state.category != null) {
    chips.push({
      key: 'category',
      label: resolved.categoryName,
      clear: { category: null },
    })
  }
  if (state.supplier != null) {
    chips.push({
      key: 'supplier',
      label: resolved.supplierName,
      clear: { supplier: null },
    })
  }
  if (state.costCenter != null) {
    chips.push({
      key: 'costCenter',
      label: resolved.costCenterName,
      clear: { costCenter: null },
    })
  }
  if (state.currency != null) {
    chips.push({
      key: 'currency',
      label: resolved.currencyCode,
      clear: { currency: null },
    })
  }
  if (state.visibility !== '') {
    chips.push({
      key: 'visibility',
      label: resolved.visibility,
      clear: { visibility: '' },
    })
  }
  if (state.mine) {
    chips.push({ key: 'mine', label: resolved.labels.mine, clear: { mine: false } })
  }
  return chips
}
