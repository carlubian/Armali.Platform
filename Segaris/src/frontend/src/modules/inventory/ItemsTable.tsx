import { ArrowDown, ArrowUp, ChevronsUpDown, Plus, Minus } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  InventoryItemSortField,
  InventoryItemStatus,
  InventoryItemSummary,
  InventoryVisibility,
} from '@/app/api/inventory'
import { formatNumber } from '@/app/i18n/formatters'
import { Badge, type BadgeTone } from '@/components/ui'

import type { ItemsState } from './itemsState'

interface Column {
  field: InventoryItemSortField
  key: keyof typeof columnLabels
  numeric?: boolean
}

const columnLabels = {
  name: 'items.columns.name',
  status: 'items.columns.status',
  category: 'items.columns.category',
  location: 'items.columns.location',
  currentStock: 'items.columns.currentStock',
  minimumStock: 'items.columns.minimumStock',
  visibility: 'items.columns.visibility',
} as const

const columns: Column[] = [
  { field: 'name', key: 'name' },
  { field: 'status', key: 'status' },
  { field: 'category', key: 'category' },
  { field: 'location', key: 'location' },
  { field: 'currentStock', key: 'currentStock', numeric: true },
  { field: 'minimumStock', key: 'minimumStock', numeric: true },
  { field: 'visibility', key: 'visibility' },
]

const statusTone: Record<InventoryItemStatus, BadgeTone> = {
  Candidate: 'gold',
  Active: 'success',
  Deprecated: 'neutral',
}

const visibilityTone: Record<InventoryVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

/** An active item at or below its minimum threshold needs attention. */
function isLowStock(item: InventoryItemSummary): boolean {
  return item.status === 'Active' && item.currentStock <= item.minimumStock
}

interface ItemsTableProps {
  items: InventoryItemSummary[]
  state: ItemsState
  language: string
  onSort: (field: InventoryItemSortField) => void
  onOpen: (itemId: number) => void
  onAdjust: (item: InventoryItemSummary) => void
  busy: boolean
}

export function ItemsTable({
  items,
  state,
  language,
  onSort,
  onOpen,
  onAdjust,
  busy,
}: ItemsTableProps) {
  const { t } = useTranslation('inventory')

  return (
    <div className="seg-inv__table-wrap" aria-busy={busy}>
      <table className="seg-inv__table">
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
                  className={column.numeric ? 'seg-inv__num' : undefined}
                >
                  <button
                    type="button"
                    className={'seg-inv__sort' + (active ? ' is-active' : '')}
                    onClick={() => onSort(column.field)}
                    aria-label={t('items.sort.label', {
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
            <th className="seg-inv__actions-col">
              <span className="seg-inv__sr">{t('items.columns.actions')}</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => {
            const lowStock = isLowStock(item)
            return (
              <tr
                key={item.id}
                className="seg-inv__row"
                onClick={() => onOpen(item.id)}
              >
                <td className="seg-inv__name">
                  <button
                    type="button"
                    className="seg-inv__row-open"
                    onClick={(event) => {
                      event.stopPropagation()
                      onOpen(item.id)
                    }}
                    aria-label={t('items.openRow', { name: item.name })}
                  >
                    {item.name}
                  </button>
                </td>
                <td>
                  <Badge tone={statusTone[item.status]} dot>
                    {t(`items.status.${item.status}`)}
                  </Badge>
                </td>
                <td>{item.categoryName}</td>
                <td>{item.locationName}</td>
                <td className="seg-inv__num">
                  <span className="seg-inv__stock">
                    {formatNumber(item.currentStock, language)}
                    {lowStock && <Badge tone="gold">{t('items.lowStock')}</Badge>}
                  </span>
                </td>
                <td className="seg-inv__num">
                  {formatNumber(item.minimumStock, language)}
                </td>
                <td>
                  <Badge tone={visibilityTone[item.visibility]}>
                    {t(`items.visibility.${item.visibility}`)}
                  </Badge>
                </td>
                <td className="seg-inv__actions-col">
                  <button
                    type="button"
                    className="seg-inv__adjust"
                    onClick={(event) => {
                      event.stopPropagation()
                      onAdjust(item)
                    }}
                    aria-label={t('items.adjust.open', { name: item.name })}
                  >
                    <Plus size={13} aria-hidden="true" />
                    <Minus size={13} aria-hidden="true" />
                  </button>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
