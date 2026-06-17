import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  TravelTripSortField,
  TravelTripStatus,
  TravelTripSummary,
  TravelVisibility,
} from '@/app/api/travel'
import { formatDate } from '@/app/i18n/formatters'
import { Badge, type BadgeTone } from '@/components/ui'

import type { TripsState } from './tripsState'

interface Column {
  field: TravelTripSortField
  key: keyof typeof columnLabels
}

const columnLabels = {
  name: 'trips.columns.name',
  tripType: 'trips.columns.tripType',
  destination: 'trips.columns.destination',
  startDate: 'trips.columns.startDate',
  endDate: 'trips.columns.endDate',
  status: 'trips.columns.status',
  visibility: 'trips.columns.visibility',
} as const

const columns: Column[] = [
  { field: 'name', key: 'name' },
  { field: 'tripType', key: 'tripType' },
  { field: 'destination', key: 'destination' },
  { field: 'startDate', key: 'startDate' },
  { field: 'endDate', key: 'endDate' },
  { field: 'status', key: 'status' },
  { field: 'visibility', key: 'visibility' },
]

const statusTone: Record<TravelTripStatus, BadgeTone> = {
  Planned: 'gold',
  Ongoing: 'success',
  Completed: 'neutral',
  Cancelled: 'neutral',
}

const visibilityTone: Record<TravelVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

interface TripsTableProps {
  trips: TravelTripSummary[]
  state: TripsState
  language: string
  onSort: (field: TravelTripSortField) => void
  onOpen: (tripId: number) => void
  busy: boolean
}

export function TripsTable({
  trips,
  state,
  language,
  onSort,
  onOpen,
  busy,
}: TripsTableProps) {
  const { t } = useTranslation('travel')

  return (
    <div className="seg-trv__table-wrap" aria-busy={busy}>
      <table className="seg-trv__table">
        <thead>
          <tr>
            {columns.map((column) => {
              const active = state.sort === column.field
              const direction = active ? state.sortDirection : undefined
              const ariaSort = !active
                ? 'none'
                : state.sortDirection === 'asc'
                  ? 'ascending'
                  : 'descending'
              return (
                <th key={column.field} aria-sort={ariaSort}>
                  <button
                    type="button"
                    className={'seg-trv__sort' + (active ? ' is-active' : '')}
                    onClick={() => onSort(column.field)}
                    aria-label={t('trips.sort.label', {
                      column: t(columnLabels[column.key]),
                    })}
                  >
                    <span>{t(columnLabels[column.key])}</span>
                    {direction === 'asc' ? (
                      <ArrowUp size={14} aria-hidden="true" />
                    ) : direction === 'desc' ? (
                      <ArrowDown size={14} aria-hidden="true" />
                    ) : (
                      <ChevronsUpDown size={14} aria-hidden="true" />
                    )}
                  </button>
                </th>
              )
            })}
          </tr>
        </thead>
        <tbody>
          {trips.map((trip) => (
            <tr key={trip.id} className="seg-trv__row" onClick={() => onOpen(trip.id)}>
              <td className="seg-trv__name">
                <button
                  type="button"
                  className="seg-trv__row-open"
                  onClick={(event) => {
                    event.stopPropagation()
                    onOpen(trip.id)
                  }}
                  aria-label={t('trips.openRow', { name: trip.name })}
                >
                  {trip.name}
                </button>
              </td>
              <td>{trip.tripTypeName}</td>
              <td>{trip.destination ?? '—'}</td>
              <td>{formatDate(trip.startDate, language)}</td>
              <td>{formatDate(trip.endDate, language)}</td>
              <td>
                <Badge tone={statusTone[trip.status]} dot>
                  {t(`trips.status.${trip.status}`)}
                </Badge>
              </td>
              <td>
                <Badge tone={visibilityTone[trip.visibility]}>
                  {t(`trips.visibility.${trip.visibility}`)}
                </Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
