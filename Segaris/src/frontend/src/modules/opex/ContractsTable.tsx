import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  OpexContractSortField,
  OpexContractStatus,
  OpexContractSummary,
  OpexMovementType,
} from '@/app/api/opex'
import { formatCurrency } from '@/app/i18n/formatters'
import { Badge, type BadgeTone } from '@/components/ui'

import type { ContractsState } from './contractsState'

interface Column {
  field: OpexContractSortField
  key: keyof typeof columnLabels
  numeric?: boolean
}

const columnLabels = {
  name: 'contracts.columns.name',
  type: 'contracts.columns.type',
  status: 'contracts.columns.status',
  category: 'contracts.columns.category',
  supplier: 'contracts.columns.supplier',
  frequency: 'contracts.columns.frequency',
  estimatedAnnual: 'contracts.columns.estimatedAnnual',
  realizedThisYear: 'contracts.columns.realizedThisYear',
  currency: 'contracts.columns.currency',
} as const

const columns: Column[] = [
  { field: 'name', key: 'name' },
  { field: 'type', key: 'type' },
  { field: 'status', key: 'status' },
  { field: 'category', key: 'category' },
  { field: 'supplier', key: 'supplier' },
  { field: 'frequency', key: 'frequency' },
  { field: 'estimatedAnnualAmount', key: 'estimatedAnnual', numeric: true },
  { field: 'realizedCurrentYearAmount', key: 'realizedThisYear', numeric: true },
  { field: 'currency', key: 'currency' },
]

const typeTone: Record<OpexMovementType, BadgeTone> = {
  Income: 'success',
  Expense: 'neutral',
}

const statusTone: Record<OpexContractStatus, BadgeTone> = {
  Planning: 'gold',
  Active: 'success',
  OnHold: 'neutral',
  Closed: 'neutral',
}

interface ContractsTableProps {
  contracts: OpexContractSummary[]
  state: ContractsState
  language: string
  onSort: (field: OpexContractSortField) => void
  onOpen: (contractId: number) => void
  busy: boolean
}

export function ContractsTable({
  contracts,
  state,
  language,
  onSort,
  onOpen,
  busy,
}: ContractsTableProps) {
  const { t } = useTranslation('opex')

  return (
    <div className="seg-opex__table-wrap" aria-busy={busy}>
      <table className="seg-opex__table">
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
                  className={column.numeric ? 'seg-opex__num' : undefined}
                >
                  <button
                    type="button"
                    className={'seg-opex__sort' + (active ? ' is-active' : '')}
                    onClick={() => onSort(column.field)}
                    aria-label={t('contracts.sort.label', {
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
          {contracts.map((contract) => (
            <tr
              key={contract.id}
              className="seg-opex__row"
              onClick={() => onOpen(contract.id)}
            >
              <td className="seg-opex__name">
                <button
                  type="button"
                  className="seg-opex__row-open"
                  onClick={(event) => {
                    event.stopPropagation()
                    onOpen(contract.id)
                  }}
                  aria-label={t('contracts.openRow', { name: contract.name })}
                >
                  {contract.name}
                </button>
              </td>
              <td>
                <Badge tone={typeTone[contract.movementType]}>
                  {t(`contracts.type.${contract.movementType}`)}
                </Badge>
              </td>
              <td>
                <Badge tone={statusTone[contract.status]} dot>
                  {t(`contracts.status.${contract.status}`)}
                </Badge>
              </td>
              <td>{contract.categoryName}</td>
              <td>{contract.supplierName ?? t('contracts.none')}</td>
              <td>{t(`contracts.frequency.${contract.expectedFrequency}`)}</td>
              <td className="seg-opex__num">
                {contract.estimatedAnnualAmount == null
                  ? t('contracts.none')
                  : formatCurrency(
                      contract.estimatedAnnualAmount,
                      contract.currencyCode,
                      language,
                    )}
              </td>
              <td className="seg-opex__num">
                {formatCurrency(
                  contract.realizedCurrentYearAmount,
                  contract.currencyCode,
                  language,
                )}
              </td>
              <td>{contract.currencyCode}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
