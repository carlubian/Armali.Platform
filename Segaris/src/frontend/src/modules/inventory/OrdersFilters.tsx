import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { InventoryOrderStatus, InventoryVisibility } from '@/app/api/inventory'
import { Button, Input, Select } from '@/components/ui'

import {
  activeOrderFilterCount,
  type OrdersFilterPatch,
  type OrdersState,
} from './ordersState'
import { useCurrencies, useSuppliers } from './queries'

interface OrdersFiltersProps {
  state: OrdersState
  onChange: (patch: OrdersFilterPatch) => void
  onClear: () => void
}

interface Chip {
  key: string
  label: string
  clear: OrdersFilterPatch
}

const statuses: InventoryOrderStatus[] = ['Planning', 'Active', 'Received', 'Cancelled']

export function OrdersFilters({ state, onChange, onClear }: OrdersFiltersProps) {
  const { t } = useTranslation('inventory')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()

  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)
  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const suppliers = useSuppliers()
  const currencies = useCurrencies()

  const anyOption = { value: '', label: t('orders.filters.anyOption') }

  const count = activeOrderFilterCount(state)
  const chips = buildChips(state, {
    supplierName: nameById(suppliers.data, state.supplier),
    currencyCode: codeById(currencies.data, state.currency),
    status: state.status === '' ? '' : t(`orders.status.${state.status}`),
    visibility:
      state.visibility === '' ? '' : t(`orders.visibility.${state.visibility}`),
    labels: {
      search: (value: string) => t('orders.filters.chip.search', { value }),
      mine: t('orders.filters.chip.mine'),
    },
  })

  return (
    <div className="seg-inv__filters">
      <div className="seg-inv__filters-primary">
        <Input
          className="seg-inv__search"
          label={t('orders.filters.searchLabel')}
          placeholder={t('orders.filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <label className="seg-inv__field">
          <span>{t('orders.filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as InventoryOrderStatus | '' })
            }
            options={[
              anyOption,
              ...statuses.map((value) => ({
                value,
                label: t(`orders.status.${value}`),
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
          {expanded ? t('orders.filters.fewer') : t('orders.filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-inv__filters-secondary" id={secondaryId}>
          <label className="seg-inv__field">
            <span>{t('orders.filters.supplier')}</span>
            <Select
              value={state.supplier == null ? '' : String(state.supplier)}
              onChange={(event) =>
                onChange({
                  supplier:
                    event.target.value === '' ? null : Number(event.target.value),
                })
              }
              options={[
                anyOption,
                ...(suppliers.data ?? []).map((supplier) => ({
                  value: String(supplier.id),
                  label: supplier.name,
                })),
              ]}
            />
          </label>
          <label className="seg-inv__field">
            <span>{t('orders.filters.currency')}</span>
            <Select
              value={state.currency == null ? '' : String(state.currency)}
              onChange={(event) =>
                onChange({
                  currency:
                    event.target.value === '' ? null : Number(event.target.value),
                })
              }
              options={[
                anyOption,
                ...(currencies.data ?? []).map((currency) => ({
                  value: String(currency.id),
                  label: currency.code,
                })),
              ]}
            />
          </label>
          <label className="seg-inv__field">
            <span>{t('orders.filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({ visibility: event.target.value as InventoryVisibility | '' })
              }
              options={[
                anyOption,
                { value: 'Public', label: t('orders.visibility.Public') },
                { value: 'Private', label: t('orders.visibility.Private') },
              ]}
            />
          </label>
          <label className="seg-inv__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('orders.filters.mine')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-inv__chips"
          role="group"
          aria-label={t('orders.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-inv__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('orders.filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-inv__chip-clear" onClick={onClear}>
            {t('orders.filters.clearAll')}
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
  supplierName: string
  currencyCode: string
  status: string
  visibility: string
  labels: {
    search: (value: string) => string
    mine: string
  }
}

function buildChips(state: OrdersState, resolved: ChipLabels): Chip[] {
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
  if (state.supplier != null) {
    chips.push({
      key: 'supplier',
      label: resolved.supplierName,
      clear: { supplier: null },
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
