import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { ProcessStatus, ProcessVisibility } from '@/app/api/processes'
import { Button, Input, Select } from '@/components/ui'

import { useProcessCategories } from './queries'
import {
  activeProcessesFilterCount,
  type ProcessesFilterPatch,
  type ProcessesState,
} from './processesState'

interface ProcessesFiltersProps {
  state: ProcessesState
  onChange: (patch: ProcessesFilterPatch) => void
  onClear: () => void
}

interface Chip {
  key: string
  label: string
  clear: ProcessesFilterPatch
}

const statuses: ProcessStatus[] = ['NotStarted', 'InProgress', 'Completed', 'Cancelled']
const visibilities: ProcessVisibility[] = ['Public', 'Private']

export function ProcessesFilters({ state, onChange, onClear }: ProcessesFiltersProps) {
  const { t } = useTranslation('processes')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()
  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)

  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const categories = useProcessCategories()
  const anyOption = { value: '', label: t('filters.anyOption') }
  const categoryOptions = [
    anyOption,
    ...(categories.data ?? []).map((category) => ({
      value: String(category.id),
      label: category.name,
    })),
  ]
  const count = activeProcessesFilterCount(state)
  const chips = buildChips(state, {
    categoryName: nameById(categories.data, state.category),
    status: state.status === '' ? '' : t(`list.status.${state.status}`),
    visibility: state.visibility === '' ? '' : t(`list.visibility.${state.visibility}`),
    labels: {
      search: (value: string) => t('filters.chip.search', { value }),
      mine: t('filters.chip.mine'),
      category: (value: string) => t('filters.chip.category', { value }),
    },
  })

  return (
    <div className="seg-proc__filters">
      <div className="seg-proc__filters-primary">
        <Input
          className="seg-proc__search"
          label={t('filters.searchLabel')}
          placeholder={t('filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <label className="seg-proc__field">
          <span>{t('filters.category')}</span>
          <Select
            value={state.category == null ? '' : String(state.category)}
            onChange={(event) =>
              onChange({
                category: event.target.value === '' ? null : Number(event.target.value),
              })
            }
            options={categoryOptions}
          />
        </label>
        <label className="seg-proc__field">
          <span>{t('filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as ProcessStatus | '' })
            }
            options={[
              anyOption,
              ...statuses.map((value) => ({
                value,
                label: t(`list.status.${value}`),
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
          {expanded ? t('filters.fewer') : t('filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-proc__filters-secondary" id={secondaryId}>
          <label className="seg-proc__field">
            <span>{t('filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({ visibility: event.target.value as ProcessVisibility | '' })
              }
              options={[
                anyOption,
                ...visibilities.map((value) => ({
                  value,
                  label: t(`list.visibility.${value}`),
                })),
              ]}
            />
          </label>
          <label className="seg-proc__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('filters.mine')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-proc__chips"
          role="group"
          aria-label={t('filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-proc__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-proc__chip-clear" onClick={onClear}>
            {t('filters.clearAll')}
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
  state: ProcessesState,
  resolved: {
    categoryName: string
    status: string
    visibility: string
    labels: {
      search: (value: string) => string
      mine: string
      category: (value: string) => string
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
  if (state.category != null) {
    chips.push({
      key: 'category',
      label: resolved.labels.category(resolved.categoryName),
      clear: { category: null },
    })
  }
  if (state.status !== '') {
    chips.push({ key: 'status', label: resolved.status, clear: { status: '' } })
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
