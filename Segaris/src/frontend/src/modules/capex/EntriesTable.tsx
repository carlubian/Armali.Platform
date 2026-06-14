import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  CapexEntrySortField,
  CapexEntryStatus,
  CapexEntrySummary,
  CapexMovementType,
} from '@/app/api/capex'
import { formatCurrency, formatDate } from '@/app/i18n/formatters'
import { Badge, type BadgeTone } from '@/components/ui'

import type { EntriesState } from './entriesState'

interface Column {
  field: CapexEntrySortField
  key: keyof typeof columnLabels
  numeric?: boolean
}

const columnLabels = {
  title: 'entries.columns.title',
  type: 'entries.columns.type',
  status: 'entries.columns.status',
  dueDate: 'entries.columns.dueDate',
  category: 'entries.columns.category',
  supplier: 'entries.columns.supplier',
  costCenter: 'entries.columns.costCenter',
  total: 'entries.columns.total',
  currency: 'entries.columns.currency',
} as const

const columns: Column[] = [
  { field: 'title', key: 'title' },
  { field: 'type', key: 'type' },
  { field: 'status', key: 'status' },
  { field: 'dueDate', key: 'dueDate' },
  { field: 'category', key: 'category' },
  { field: 'supplier', key: 'supplier' },
  { field: 'costCenter', key: 'costCenter' },
  { field: 'total', key: 'total', numeric: true },
  { field: 'currency', key: 'currency' },
]

const typeTone: Record<CapexMovementType, BadgeTone> = {
  Income: 'success',
  Expense: 'neutral',
}

const statusTone: Record<CapexEntryStatus, BadgeTone> = {
  Planning: 'gold',
  Completed: 'success',
  Canceled: 'neutral',
}

interface EntriesTableProps {
  entries: CapexEntrySummary[]
  state: EntriesState
  language: string
  onSort: (field: CapexEntrySortField) => void
  busy: boolean
}

export function EntriesTable({
  entries,
  state,
  language,
  onSort,
  busy,
}: EntriesTableProps) {
  const { t } = useTranslation('capex')

  return (
    <div className="seg-capex__table-wrap" aria-busy={busy}>
      <table className="seg-capex__table">
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
                <th
                  key={column.field}
                  aria-sort={ariaSort}
                  className={column.numeric ? 'seg-capex__num' : undefined}
                >
                  <button
                    type="button"
                    className={'seg-capex__sort' + (active ? ' is-active' : '')}
                    onClick={() => onSort(column.field)}
                    aria-label={t('entries.sort.label', {
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
          {entries.map((entry) => (
            <tr key={entry.id}>
              <td className="seg-capex__title">{entry.title}</td>
              <td>
                <Badge tone={typeTone[entry.movementType]}>
                  {t(`entries.type.${entry.movementType}`)}
                </Badge>
              </td>
              <td>
                <Badge tone={statusTone[entry.status]} dot>
                  {t(`entries.status.${entry.status}`)}
                </Badge>
              </td>
              <td>{formatDate(entry.dueDate, language)}</td>
              <td>{entry.categoryName}</td>
              <td>{entry.supplierName ?? t('entries.none')}</td>
              <td>{entry.costCenterName ?? t('entries.none')}</td>
              <td className="seg-capex__num">
                {formatCurrency(entry.totalAmount, entry.currencyCode, language)}
              </td>
              <td>{entry.currencyCode}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
