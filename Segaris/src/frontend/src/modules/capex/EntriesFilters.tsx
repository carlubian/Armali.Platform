import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type {
  CapexEntryStatus,
  CapexMovementType,
  CapexVisibility,
} from '@/app/api/capex'
import { formatDate } from '@/app/i18n/formatters'
import { Button, Input, Select } from '@/components/ui'

import {
  activeFilterCount,
  type EntriesFilterPatch,
  type EntriesState,
} from './entriesState'
import {
  useCapexCategories,
  useCostCenters,
  useCurrencies,
  useSuppliers,
} from './queries'

interface EntriesFiltersProps {
  state: EntriesState
  onChange: (patch: EntriesFilterPatch) => void
  onClear: () => void
  language: string
}

interface Chip {
  key: string
  label: string
  clear: EntriesFilterPatch
}

export function EntriesFilters({
  state,
  onChange,
  onClear,
  language,
}: EntriesFiltersProps) {
  const { t } = useTranslation('capex')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()

  // The search box keeps a local value so fast typing is never dropped by the
  // URL round-trip; it re-syncs when the URL search changes from elsewhere
  // (for example the "Clear all" action) using the previous-value render
  // adjustment recommended by React instead of an effect.
  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)
  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const categories = useCapexCategories()
  const suppliers = useSuppliers()
  const costCenters = useCostCenters()
  const currencies = useCurrencies()

  const anyOption = { value: '', label: t('entries.filters.anyOption') }
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
  const chips = buildChips(state, language, {
    categoryName: nameById(categories.data, state.category),
    supplierName: nameById(suppliers.data, state.supplier),
    costCenterName: nameById(costCenters.data, state.costCenter),
    currencyCode: codeById(currencies.data, state.currency),
    type: state.type === '' ? '' : t(`entries.type.${state.type}`),
    status: state.status === '' ? '' : t(`entries.status.${state.status}`),
    visibility:
      state.visibility === '' ? '' : t(`entries.visibility.${state.visibility}`),
    labels: {
      search: (value) => t('entries.filters.chip.search', { value }),
      from: (value) => t('entries.filters.chip.from', { value }),
      to: (value) => t('entries.filters.chip.to', { value }),
      mine: t('entries.filters.chip.mine'),
    },
  })

  return (
    <div className="seg-capex__filters">
      <div className="seg-capex__filters-primary">
        <Input
          className="seg-capex__search"
          label={t('entries.filters.searchLabel')}
          placeholder={t('entries.filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <Input
          type="date"
          label={t('entries.filters.from')}
          value={state.from}
          onChange={(event) => onChange({ from: event.target.value })}
        />
        <Input
          type="date"
          label={t('entries.filters.to')}
          value={state.to}
          onChange={(event) => onChange({ to: event.target.value })}
        />
        <label className="seg-capex__field">
          <span>{t('entries.filters.type')}</span>
          <Select
            value={state.type}
            onChange={(event) =>
              onChange({ type: event.target.value as CapexMovementType | '' })
            }
            options={[
              anyOption,
              { value: 'Income', label: t('entries.type.Income') },
              { value: 'Expense', label: t('entries.type.Expense') },
            ]}
          />
        </label>
        <label className="seg-capex__field">
          <span>{t('entries.filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as CapexEntryStatus | '' })
            }
            options={[
              anyOption,
              { value: 'Planning', label: t('entries.status.Planning') },
              { value: 'Completed', label: t('entries.status.Completed') },
              { value: 'Canceled', label: t('entries.status.Canceled') },
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
          {expanded ? t('entries.filters.fewer') : t('entries.filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-capex__filters-secondary" id={secondaryId}>
          <label className="seg-capex__field">
            <span>{t('entries.filters.category')}</span>
            <Select
              value={state.category == null ? '' : String(state.category)}
              onChange={(event) => numberPatch('category', event.target.value)}
              options={idOptions(categories.data)}
            />
          </label>
          <label className="seg-capex__field">
            <span>{t('entries.filters.supplier')}</span>
            <Select
              value={state.supplier == null ? '' : String(state.supplier)}
              onChange={(event) => numberPatch('supplier', event.target.value)}
              options={idOptions(suppliers.data)}
            />
          </label>
          <label className="seg-capex__field">
            <span>{t('entries.filters.costCenter')}</span>
            <Select
              value={state.costCenter == null ? '' : String(state.costCenter)}
              onChange={(event) => numberPatch('costCenter', event.target.value)}
              options={idOptions(costCenters.data)}
            />
          </label>
          <label className="seg-capex__field">
            <span>{t('entries.filters.currency')}</span>
            <Select
              value={state.currency == null ? '' : String(state.currency)}
              onChange={(event) => numberPatch('currency', event.target.value)}
              options={idOptions(currencies.data, true)}
            />
          </label>
          <label className="seg-capex__field">
            <span>{t('entries.filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({ visibility: event.target.value as CapexVisibility | '' })
              }
              options={[
                anyOption,
                { value: 'Public', label: t('entries.visibility.Public') },
                { value: 'Private', label: t('entries.visibility.Private') },
              ]}
            />
          </label>
          <label className="seg-capex__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('entries.filters.myEntries')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-capex__chips"
          role="group"
          aria-label={t('entries.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-capex__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('entries.filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-capex__chip-clear" onClick={onClear}>
            {t('entries.filters.clearAll')}
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
  visibility: string
  labels: {
    search: (value: string) => string
    from: (value: string) => string
    to: (value: string) => string
    mine: string
  }
}

function buildChips(
  state: EntriesState,
  language: string,
  resolved: ChipLabels,
): Chip[] {
  const chips: Chip[] = []
  if (state.search.trim() !== '') {
    chips.push({
      key: 'search',
      label: resolved.labels.search(state.search.trim()),
      clear: { search: '' },
    })
  }
  if (state.from !== '') {
    chips.push({
      key: 'from',
      label: resolved.labels.from(formatDate(state.from, language)),
      clear: { from: '' },
    })
  }
  if (state.to !== '') {
    chips.push({
      key: 'to',
      label: resolved.labels.to(formatDate(state.to, language)),
      clear: { to: '' },
    })
  }
  if (state.type !== '') {
    chips.push({ key: 'type', label: resolved.type, clear: { type: '' } })
  }
  if (state.status !== '') {
    chips.push({ key: 'status', label: resolved.status, clear: { status: '' } })
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
