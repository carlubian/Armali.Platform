import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { InventoryItemStatus, InventoryVisibility } from '@/app/api/inventory'
import { Button, Input, Select } from '@/components/ui'

import {
  activeItemFilterCount,
  type ItemsFilterPatch,
  type ItemsState,
} from './itemsState'
import { useInventoryCategories, useInventoryLocations, useSuppliers } from './queries'

interface ItemsFiltersProps {
  state: ItemsState
  onChange: (patch: ItemsFilterPatch) => void
  onClear: () => void
}

interface Chip {
  key: string
  label: string
  clear: ItemsFilterPatch
}

const statuses: InventoryItemStatus[] = ['Candidate', 'Active', 'Deprecated']

export function ItemsFilters({ state, onChange, onClear }: ItemsFiltersProps) {
  const { t } = useTranslation('inventory')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()

  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)
  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const categories = useInventoryCategories()
  const locations = useInventoryLocations()
  const suppliers = useSuppliers()

  const anyOption = { value: '', label: t('items.filters.anyOption') }
  const idOptions = (
    items: ReadonlyArray<{ id: number; name: string }> | undefined,
  ) => [
    anyOption,
    ...(items ?? []).map((item) => ({ value: String(item.id), label: item.name })),
  ]

  const numberPatch = (key: 'category' | 'location' | 'supplier', value: string) =>
    onChange({ [key]: value === '' ? null : Number(value) })

  const count = activeItemFilterCount(state)
  const chips = buildChips(state, {
    categoryName: nameById(categories.data, state.category),
    locationName: nameById(locations.data, state.location),
    supplierName: nameById(suppliers.data, state.supplier),
    status: state.status === '' ? '' : t(`items.status.${state.status}`),
    visibility:
      state.visibility === '' ? '' : t(`items.visibility.${state.visibility}`),
    labels: {
      search: (value: string) => t('items.filters.chip.search', { value }),
      mine: t('items.filters.chip.mine'),
    },
  })

  return (
    <div className="seg-inv__filters">
      <div className="seg-inv__filters-primary">
        <Input
          className="seg-inv__search"
          label={t('items.filters.searchLabel')}
          placeholder={t('items.filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <label className="seg-inv__field">
          <span>{t('items.filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as InventoryItemStatus | '' })
            }
            options={[
              anyOption,
              ...statuses.map((value) => ({
                value,
                label: t(`items.status.${value}`),
              })),
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
          {expanded ? t('items.filters.fewer') : t('items.filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-inv__filters-secondary" id={secondaryId}>
          <label className="seg-inv__field">
            <span>{t('items.filters.category')}</span>
            <Select
              value={state.category == null ? '' : String(state.category)}
              onChange={(event) => numberPatch('category', event.target.value)}
              options={idOptions(categories.data)}
            />
          </label>
          <label className="seg-inv__field">
            <span>{t('items.filters.location')}</span>
            <Select
              value={state.location == null ? '' : String(state.location)}
              onChange={(event) => numberPatch('location', event.target.value)}
              options={idOptions(locations.data)}
            />
          </label>
          <label className="seg-inv__field">
            <span>{t('items.filters.supplier')}</span>
            <Select
              value={state.supplier == null ? '' : String(state.supplier)}
              onChange={(event) => numberPatch('supplier', event.target.value)}
              options={idOptions(suppliers.data)}
            />
          </label>
          <label className="seg-inv__field">
            <span>{t('items.filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({ visibility: event.target.value as InventoryVisibility | '' })
              }
              options={[
                anyOption,
                { value: 'Public', label: t('items.visibility.Public') },
                { value: 'Private', label: t('items.visibility.Private') },
              ]}
            />
          </label>
          <label className="seg-inv__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('items.filters.mine')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-inv__chips"
          role="group"
          aria-label={t('items.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-inv__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('items.filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-inv__chip-clear" onClick={onClear}>
            {t('items.filters.clearAll')}
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

interface ChipLabels {
  categoryName: string
  locationName: string
  supplierName: string
  status: string
  visibility: string
  labels: {
    search: (value: string) => string
    mine: string
  }
}

function buildChips(state: ItemsState, resolved: ChipLabels): Chip[] {
  const chips: Chip[] = []
  if (state.search.trim() !== '') {
    chips.push({
      key: 'search',
      label: resolved.labels.search(state.search.trim()),
      clear: { search: '' },
    })
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
  if (state.location != null) {
    chips.push({
      key: 'location',
      label: resolved.locationName,
      clear: { location: null },
    })
  }
  if (state.supplier != null) {
    chips.push({
      key: 'supplier',
      label: resolved.supplierName,
      clear: { supplier: null },
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
