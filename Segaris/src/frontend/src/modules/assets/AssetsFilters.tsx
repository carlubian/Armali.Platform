import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { AssetStatus, AssetVisibility } from '@/app/api/assets'
import { Button, Input, Select } from '@/components/ui'

import {
  activeAssetFilterCount,
  type AssetsFilterPatch,
  type AssetsState,
} from './assetsState'
import { useAssetCategories, useAssetLocations } from './queries'

interface AssetsFiltersProps {
  state: AssetsState
  onChange: (patch: AssetsFilterPatch) => void
  onClear: () => void
}

interface Chip {
  key: string
  label: string
  clear: AssetsFilterPatch
}

const statuses: AssetStatus[] = ['Active', 'Stored', 'Retired']

export function AssetsFilters({ state, onChange, onClear }: AssetsFiltersProps) {
  const { t } = useTranslation('assets')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()
  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)

  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const categories = useAssetCategories()
  const locations = useAssetLocations()
  const anyOption = { value: '', label: t('assets.filters.anyOption') }
  const idOptions = (items: ReadonlyArray<{ id: number; name: string }> | undefined) => [
    anyOption,
    ...(items ?? []).map((item) => ({ value: String(item.id), label: item.name })),
  ]
  const numberPatch = (key: 'category' | 'location', value: string) =>
    onChange({ [key]: value === '' ? null : Number(value) })
  const count = activeAssetFilterCount(state)
  const chips = buildChips(state, {
    categoryName: nameById(categories.data, state.category),
    locationName: nameById(locations.data, state.location),
    status: state.status === '' ? '' : t(`assets.status.${state.status}`),
    visibility:
      state.visibility === '' ? '' : t(`assets.visibility.${state.visibility}`),
    labels: {
      search: (value: string) => t('assets.filters.chip.search', { value }),
      mine: t('assets.filters.chip.mine'),
    },
  })

  return (
    <div className="seg-assets__filters">
      <div className="seg-assets__filters-primary">
        <Input
          className="seg-assets__search"
          label={t('assets.filters.searchLabel')}
          placeholder={t('assets.filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <label className="seg-assets__field">
          <span>{t('assets.filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as AssetStatus | '' })
            }
            options={[
              anyOption,
              ...statuses.map((value) => ({
                value,
                label: t(`assets.status.${value}`),
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
          {expanded ? t('assets.filters.fewer') : t('assets.filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-assets__filters-secondary" id={secondaryId}>
          <label className="seg-assets__field">
            <span>{t('assets.filters.category')}</span>
            <Select
              value={state.category == null ? '' : String(state.category)}
              onChange={(event) => numberPatch('category', event.target.value)}
              options={idOptions(categories.data)}
            />
          </label>
          <label className="seg-assets__field">
            <span>{t('assets.filters.location')}</span>
            <Select
              value={state.location == null ? '' : String(state.location)}
              onChange={(event) => numberPatch('location', event.target.value)}
              options={idOptions(locations.data)}
            />
          </label>
          <label className="seg-assets__field">
            <span>{t('assets.filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({ visibility: event.target.value as AssetVisibility | '' })
              }
              options={[
                anyOption,
                { value: 'Public', label: t('assets.visibility.Public') },
                { value: 'Private', label: t('assets.visibility.Private') },
              ]}
            />
          </label>
          <label className="seg-assets__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('assets.filters.mine')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-assets__chips"
          role="group"
          aria-label={t('assets.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-assets__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('assets.filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-assets__chip-clear" onClick={onClear}>
            {t('assets.filters.clearAll')}
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

function buildChips(
  state: AssetsState,
  resolved: {
    categoryName: string
    locationName: string
    status: string
    visibility: string
    labels: { search: (value: string) => string; mine: string }
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
