import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { assetsApi, type AssetSummary } from '@/app/api/assets'
import type {
  MaintenancePriority,
  MaintenanceStatus,
  MaintenanceVisibility,
} from '@/app/api/maintenance'
import { Button, Input, Select } from '@/components/ui'
import { useQuery } from '@tanstack/react-query'

import {
  activeMaintenanceFilterCount,
  type MaintenanceFilterPatch,
  type MaintenanceState,
} from './maintenanceState'
import { useMaintenanceTypes } from './queries'

interface MaintenanceFiltersProps {
  state: MaintenanceState
  onChange: (patch: MaintenanceFilterPatch) => void
  onClear: () => void
}

interface Chip {
  key: string
  label: string
  clear: MaintenanceFilterPatch
}

const statuses: MaintenanceStatus[] = [
  'Pending',
  'InProgress',
  'Completed',
  'Cancelled',
]
const priorities: MaintenancePriority[] = ['Low', 'Medium', 'High']

export function MaintenanceFilters({
  state,
  onChange,
  onClear,
}: MaintenanceFiltersProps) {
  const { t } = useTranslation('maintenance')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()
  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)

  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const types = useMaintenanceTypes()
  const assets = useQuery({
    queryKey: ['maintenance', 'asset-filter-options'],
    queryFn: ({ signal }) =>
      assetsApi.listAssets(
        { page: 1, pageSize: 100, sort: 'name', sortDirection: 'asc' },
        signal,
      ),
  })
  const anyOption = { value: '', label: t('tasks.filters.anyOption') }
  const typeOptions = [
    anyOption,
    ...(types.data ?? []).map((type) => ({
      value: String(type.id),
      label: type.name,
    })),
  ]
  const assetOptions = [
    anyOption,
    ...(assets.data?.items ?? []).map((asset) => ({
      value: String(asset.id),
      label: asset.name,
    })),
  ]
  const count = activeMaintenanceFilterCount(state)
  const chips = buildChips(state, {
    typeName: nameById(types.data, state.type),
    assetName: assetNameById(assets.data?.items, state.asset),
    status: state.status === '' ? '' : t(`tasks.status.${state.status}`),
    priority: state.priority === '' ? '' : t(`tasks.priority.${state.priority}`),
    visibility:
      state.visibility === '' ? '' : t(`tasks.visibility.${state.visibility}`),
    labels: {
      search: (value: string) => t('tasks.filters.chip.search', { value }),
      mine: t('tasks.filters.chip.mine'),
      asset: (value: string) => t('tasks.filters.chip.asset', { value }),
    },
  })

  return (
    <div className="seg-maint__filters">
      <div className="seg-maint__filters-primary">
        <Input
          className="seg-maint__search"
          label={t('tasks.filters.searchLabel')}
          placeholder={t('tasks.filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <label className="seg-maint__field">
          <span>{t('tasks.filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as MaintenanceStatus | '' })
            }
            options={[
              anyOption,
              ...statuses.map((value) => ({
                value,
                label: t(`tasks.status.${value}`),
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
          {expanded ? t('tasks.filters.fewer') : t('tasks.filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-maint__filters-secondary" id={secondaryId}>
          <label className="seg-maint__field">
            <span>{t('tasks.filters.type')}</span>
            <Select
              value={state.type == null ? '' : String(state.type)}
              onChange={(event) =>
                onChange({
                  type: event.target.value === '' ? null : Number(event.target.value),
                })
              }
              options={typeOptions}
            />
          </label>
          <label className="seg-maint__field">
            <span>{t('tasks.filters.priority')}</span>
            <Select
              value={state.priority}
              onChange={(event) =>
                onChange({
                  priority: event.target.value as MaintenancePriority | '',
                })
              }
              options={[
                anyOption,
                ...priorities.map((value) => ({
                  value,
                  label: t(`tasks.priority.${value}`),
                })),
              ]}
            />
          </label>
          <label className="seg-maint__field">
            <span>{t('tasks.filters.asset')}</span>
            <Select
              value={state.asset == null ? '' : String(state.asset)}
              onChange={(event) =>
                onChange({
                  asset: event.target.value === '' ? null : Number(event.target.value),
                })
              }
              options={assetOptions}
            />
          </label>
          <label className="seg-maint__field">
            <span>{t('tasks.filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({
                  visibility: event.target.value as MaintenanceVisibility | '',
                })
              }
              options={[
                anyOption,
                { value: 'Public', label: t('tasks.visibility.Public') },
                { value: 'Private', label: t('tasks.visibility.Private') },
              ]}
            />
          </label>
          <label className="seg-maint__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('tasks.filters.mine')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-maint__chips"
          role="group"
          aria-label={t('tasks.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-maint__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('tasks.filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-maint__chip-clear" onClick={onClear}>
            {t('tasks.filters.clearAll')}
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

function assetNameById(
  items: ReadonlyArray<AssetSummary> | undefined,
  id: number | null,
): string {
  if (id == null) return ''
  return items?.find((asset) => asset.id === id)?.name ?? String(id)
}

function buildChips(
  state: MaintenanceState,
  resolved: {
    typeName: string
    assetName: string
    status: string
    priority: string
    visibility: string
    labels: {
      search: (value: string) => string
      mine: string
      asset: (value: string) => string
    }
  },
): Chip[] {
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
  if (state.type != null) {
    chips.push({ key: 'type', label: resolved.typeName, clear: { type: null } })
  }
  if (state.priority !== '') {
    chips.push({
      key: 'priority',
      label: resolved.priority,
      clear: { priority: '' },
    })
  }
  if (state.asset != null) {
    chips.push({
      key: 'asset',
      label: resolved.labels.asset(resolved.assetName),
      clear: { asset: null },
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
