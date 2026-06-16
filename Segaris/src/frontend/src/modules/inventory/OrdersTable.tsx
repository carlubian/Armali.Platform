import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  InventoryOrderSortField,
  InventoryOrderStatus,
  InventoryOrderSummary,
  InventoryVisibility,
} from '@/app/api/inventory'
import { formatDate } from '@/app/i18n/formatters'
import { Badge, type BadgeTone } from '@/components/ui'

import type { OrdersState } from './ordersState'

interface Column {
  field: InventoryOrderSortField
  key: keyof typeof columnLabels
}

const columnLabels = {
  supplier: 'orders.columns.supplier',
  status: 'orders.columns.status',
  orderDate: 'orders.columns.orderDate',
  expectedReceiptDate: 'orders.columns.expectedReceiptDate',
  currency: 'orders.columns.currency',
  visibility: 'orders.columns.visibility',
} as const

const columns: Column[] = [
  { field: 'supplier', key: 'supplier' },
  { field: 'status', key: 'status' },
  { field: 'orderDate', key: 'orderDate' },
  { field: 'expectedReceiptDate', key: 'expectedReceiptDate' },
  { field: 'currency', key: 'currency' },
  { field: 'visibility', key: 'visibility' },
]

const statusTone: Record<InventoryOrderStatus, BadgeTone> = {
  Planning: 'gold',
  Active: 'success',
  Received: 'azure',
  Cancelled: 'neutral',
}

const visibilityTone: Record<InventoryVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

interface OrdersTableProps {
  orders: InventoryOrderSummary[]
  state: OrdersState
  language: string
  onSort: (field: InventoryOrderSortField) => void
  onOpen: (orderId: number) => void
  busy: boolean
}

export function OrdersTable({
  orders,
  state,
  language,
  onSort,
  onOpen,
  busy,
}: OrdersTableProps) {
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
                <th key={column.field} aria-sort={ariaSort}>
                  <button
                    type="button"
                    className={'seg-inv__sort' + (active ? ' is-active' : '')}
                    onClick={() => onSort(column.field)}
                    aria-label={t('orders.sort.label', {
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
          {orders.map((order) => (
            <tr
              key={order.id}
              className="seg-inv__row"
              onClick={() => onOpen(order.id)}
            >
              <td className="seg-inv__name">
                <button
                  type="button"
                  className="seg-inv__row-open"
                  onClick={(event) => {
                    event.stopPropagation()
                    onOpen(order.id)
                  }}
                  aria-label={t('orders.openRow', { supplier: order.supplierName })}
                >
                  {order.supplierName}
                </button>
              </td>
              <td>
                <Badge tone={statusTone[order.status]} dot>
                  {t(`orders.status.${order.status}`)}
                </Badge>
              </td>
              <td>
                {order.orderDate == null
                  ? t('common.none')
                  : formatDate(order.orderDate, language)}
              </td>
              <td>
                {order.expectedReceiptDate == null
                  ? t('common.none')
                  : formatDate(order.expectedReceiptDate, language)}
              </td>
              <td>{order.currencyCode}</td>
              <td>
                <Badge tone={visibilityTone[order.visibility]}>
                  {t(`orders.visibility.${order.visibility}`)}
                </Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
