import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  MaintenancePriority,
  MaintenanceSortField,
  MaintenanceStatus,
  MaintenanceTaskSummary,
  MaintenanceVisibility,
} from '@/app/api/maintenance'
import { formatDate } from '@/app/i18n/formatters'
import { Badge, type BadgeTone } from '@/components/ui'

import type { MaintenanceState } from './maintenanceState'

interface Column {
  field: MaintenanceSortField
  key: keyof typeof columnLabels
}

const columnLabels = {
  title: 'tasks.columns.title',
  type: 'tasks.columns.type',
  status: 'tasks.columns.status',
  priority: 'tasks.columns.priority',
  dueDate: 'tasks.columns.dueDate',
  visibility: 'tasks.columns.visibility',
} as const

const columns: Column[] = [
  { field: 'title', key: 'title' },
  { field: 'type', key: 'type' },
  { field: 'status', key: 'status' },
  { field: 'priority', key: 'priority' },
  { field: 'dueDate', key: 'dueDate' },
  { field: 'visibility', key: 'visibility' },
]

const statusTone: Record<MaintenanceStatus, BadgeTone> = {
  Pending: 'gold',
  InProgress: 'azure',
  Completed: 'success',
  Cancelled: 'neutral',
}

const priorityTone: Record<MaintenancePriority, BadgeTone> = {
  Low: 'neutral',
  Medium: 'gold',
  High: 'danger',
}

const visibilityTone: Record<MaintenanceVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

interface MaintenanceTableProps {
  tasks: MaintenanceTaskSummary[]
  state: MaintenanceState
  language: string
  onSort: (field: MaintenanceSortField) => void
  onOpen: (taskId: number) => void
  busy: boolean
}

export function MaintenanceTable({
  tasks,
  state,
  language,
  onSort,
  onOpen,
  busy,
}: MaintenanceTableProps) {
  const { t } = useTranslation('maintenance')

  return (
    <div className="seg-maint__table-wrap" aria-busy={busy}>
      <table className="seg-maint__table">
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
                    className={'seg-maint__sort' + (active ? ' is-active' : '')}
                    onClick={() => onSort(column.field)}
                    aria-label={t('tasks.sort.label', {
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
            <th>{t('tasks.columns.asset')}</th>
          </tr>
        </thead>
        <tbody>
          {tasks.map((task) => (
            <tr
              key={task.id}
              className="seg-maint__row"
              onClick={() => onOpen(task.id)}
            >
              <td className="seg-maint__name">
                <button
                  type="button"
                  className="seg-maint__row-open"
                  onClick={(event) => {
                    event.stopPropagation()
                    onOpen(task.id)
                  }}
                  aria-label={t('tasks.openRow', { title: task.title })}
                >
                  {task.title}
                </button>
              </td>
              <td>{task.maintenanceTypeName}</td>
              <td>
                <Badge tone={statusTone[task.status]} dot>
                  {t(`tasks.status.${task.status}`)}
                </Badge>
              </td>
              <td>
                <Badge tone={priorityTone[task.priority]}>
                  {t(`tasks.priority.${task.priority}`)}
                </Badge>
              </td>
              <td>
                {task.dueDate == null
                  ? t('common.none')
                  : formatDate(task.dueDate, language)}
              </td>
              <td>
                <Badge tone={visibilityTone[task.visibility]}>
                  {t(`tasks.visibility.${task.visibility}`)}
                </Badge>
              </td>
              <td>
                {task.assetId == null
                  ? t('common.none')
                  : (task.assetName ?? t('common.unknownAsset'))}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
