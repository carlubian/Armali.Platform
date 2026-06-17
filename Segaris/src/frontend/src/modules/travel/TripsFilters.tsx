import { SlidersHorizontal, X } from 'lucide-react'
import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { TravelTripStatus, TravelVisibility } from '@/app/api/travel'
import { Button, Input, Select } from '@/components/ui'

import {
  activeTripFilterCount,
  type TripsFilterPatch,
  type TripsState,
} from './tripsState'
import { useTravelTripTypes } from './queries'

interface TripsFiltersProps {
  state: TripsState
  onChange: (patch: TripsFilterPatch) => void
  onClear: () => void
}

interface Chip {
  key: string
  label: string
  clear: TripsFilterPatch
}

const statuses: TravelTripStatus[] = ['Planned', 'Ongoing', 'Completed', 'Cancelled']

export function TripsFilters({ state, onChange, onClear }: TripsFiltersProps) {
  const { t } = useTranslation('travel')
  const [expanded, setExpanded] = useState(false)
  const secondaryId = useId()

  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)
  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const tripTypes = useTravelTripTypes()

  const anyOption = { value: '', label: t('trips.filters.anyOption') }

  const count = activeTripFilterCount(state)
  const chips = buildChips(state, {
    tripTypeName: nameById(tripTypes.data, state.tripType),
    status: state.status === '' ? '' : t(`trips.status.${state.status}`),
    visibility:
      state.visibility === '' ? '' : t(`trips.visibility.${state.visibility}`),
    labels: {
      search: (value: string) => t('trips.filters.chip.search', { value }),
      mine: t('trips.filters.chip.mine'),
    },
  })

  return (
    <div className="seg-trv__filters">
      <div className="seg-trv__filters-primary">
        <Input
          className="seg-trv__search"
          label={t('trips.filters.searchLabel')}
          placeholder={t('trips.filters.searchPlaceholder')}
          value={searchText}
          onChange={(event) => {
            setSearchText(event.target.value)
            onChange({ search: event.target.value })
          }}
        />
        <label className="seg-trv__field">
          <span>{t('trips.filters.status')}</span>
          <Select
            value={state.status}
            onChange={(event) =>
              onChange({ status: event.target.value as TravelTripStatus | '' })
            }
            options={[
              anyOption,
              ...statuses.map((value) => ({
                value,
                label: t(`trips.status.${value}`),
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
          {expanded ? t('trips.filters.fewer') : t('trips.filters.more')}
        </Button>
      </div>

      {expanded && (
        <div className="seg-trv__filters-secondary" id={secondaryId}>
          <label className="seg-trv__field">
            <span>{t('trips.filters.tripType')}</span>
            <Select
              value={state.tripType == null ? '' : String(state.tripType)}
              onChange={(event) =>
                onChange({
                  tripType:
                    event.target.value === '' ? null : Number(event.target.value),
                })
              }
              options={[
                anyOption,
                ...(tripTypes.data ?? []).map((type) => ({
                  value: String(type.id),
                  label: type.name,
                })),
              ]}
            />
          </label>
          <label className="seg-trv__field">
            <span>{t('trips.filters.visibility')}</span>
            <Select
              value={state.visibility}
              onChange={(event) =>
                onChange({ visibility: event.target.value as TravelVisibility | '' })
              }
              options={[
                anyOption,
                { value: 'Public', label: t('trips.visibility.Public') },
                { value: 'Private', label: t('trips.visibility.Private') },
              ]}
            />
          </label>
          <label className="seg-trv__check">
            <input
              type="checkbox"
              checked={state.mine}
              onChange={(event) => onChange({ mine: event.target.checked })}
            />
            <span>{t('trips.filters.mine')}</span>
          </label>
        </div>
      )}

      {count > 0 && (
        <div
          className="seg-trv__chips"
          role="group"
          aria-label={t('trips.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-trv__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('trips.filters.remove', { label: chip.label })}
            >
              <span>{chip.label}</span>
              <X size={13} aria-hidden="true" />
            </button>
          ))}
          <button type="button" className="seg-trv__chip-clear" onClick={onClear}>
            {t('trips.filters.clearAll')}
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
  tripTypeName: string
  status: string
  visibility: string
  labels: {
    search: (value: string) => string
    mine: string
  }
}

function buildChips(state: TripsState, resolved: ChipLabels): Chip[] {
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
  if (state.tripType != null) {
    chips.push({
      key: 'tripType',
      label: resolved.tripTypeName,
      clear: { tripType: null },
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
