import {
  ArrowDown,
  ArrowUp,
  ChevronsUpDown,
  Globe,
  ListChecks,
  Lock,
  Pencil,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  ProcessSortField,
  ProcessStatus,
  ProcessSummary,
  ProcessVisibility,
} from '@/app/api/processes'
import { formatDate } from '@/app/i18n/formatters'
import { Badge, IconButton, Tooltip, type BadgeTone } from '@/components/ui'

import { CategoryGlyph } from './categoryIcons'
import { daysUntil, dueUrgency } from './processDates'
import type { ProcessesState } from './processesState'

const columnLabels: Record<ProcessSortField, string> = {
  name: 'list.columns.name',
  category: 'list.columns.category',
  status: 'list.columns.status',
  dueDate: 'list.columns.dueDate',
  visibility: 'list.columns.visibility',
}

const statusTone: Record<ProcessStatus, BadgeTone> = {
  NotStarted: 'neutral',
  InProgress: 'aqua',
  Completed: 'success',
  Cancelled: 'danger',
}

const visibilityMeta: Record<ProcessVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

interface ProcessesTableProps {
  processes: ProcessSummary[]
  state: ProcessesState
  language: string
  busy: boolean
  onSort: (field: ProcessSortField) => void
  onOpen: (processId: number) => void
  onSteps: (processId: number) => void
}

export function ProcessesTable({
  processes,
  state,
  language,
  busy,
  onSort,
  onOpen,
  onSteps,
}: ProcessesTableProps) {
  const { t } = useTranslation('processes')

  const sortableHeader = (field: ProcessSortField) => {
    const active = state.sort === field
    const direction = active ? state.sortDirection : undefined
    const ariaSort = !active
      ? 'none'
      : state.sortDirection === 'asc'
        ? 'ascending'
        : 'descending'
    return (
      <th key={field} aria-sort={ariaSort}>
        <button
          type="button"
          className={'seg-proc__sort' + (active ? ' is-active' : '')}
          onClick={() => onSort(field)}
          aria-label={t('list.sort.label', { column: t(columnLabels[field]) })}
        >
          <span>{t(columnLabels[field])}</span>
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
  }

  return (
    <div className="seg-proc__table-wrap" aria-busy={busy}>
      <table className="seg-proc__table">
        <thead>
          <tr>
            {sortableHeader('name')}
            {sortableHeader('category')}
            {sortableHeader('status')}
            <th className="seg-proc__col-static">{t('list.columns.progress')}</th>
            {sortableHeader('dueDate')}
            {sortableHeader('visibility')}
            <th className="seg-proc__col-manage">{t('list.manage')}</th>
          </tr>
        </thead>
        <tbody>
          {processes.map((process) => {
            const closed =
              process.status === 'Completed' || process.status === 'Cancelled'
            return (
              <tr
                key={process.id}
                className={'seg-proc__row' + (closed ? ' is-closed' : '')}
              >
                <td className="seg-proc__name">
                  <span className="seg-proc__name-icon" aria-hidden="true">
                    <ListChecks size={17} />
                  </span>
                  <span className="seg-proc__name-txt">
                    <button
                      type="button"
                      className="seg-proc__row-open"
                      onClick={() => onOpen(process.id)}
                      aria-label={t('list.openRow', { name: process.name })}
                    >
                      {process.name}
                    </button>
                    <span className="seg-proc__name-owner">
                      {t('list.by', { name: process.creatorName })}
                    </span>
                  </span>
                </td>
                <td>
                  <span className="seg-proc__cat">
                    <CategoryGlyph name={process.categoryName} size={14} />
                    {process.categoryName}
                  </span>
                </td>
                <td>
                  <Badge
                    tone={statusTone[process.status]}
                    dot
                    pulse={process.status === 'InProgress'}
                  >
                    {t(`list.status.${process.status}`)}
                  </Badge>
                </td>
                <td>
                  <Progress
                    resolved={process.resolvedStepCount}
                    total={process.totalStepCount}
                  />
                </td>
                <td>
                  <DueCell
                    dueDate={process.effectiveDueDate}
                    closed={closed}
                    language={language}
                  />
                </td>
                <td>
                  <span
                    className={
                      'seg-proc__vis' +
                      (process.visibility === 'Private' ? ' is-private' : '')
                    }
                  >
                    {process.visibility === 'Private' ? (
                      <Lock size={13} aria-hidden="true" />
                    ) : (
                      <Globe size={13} aria-hidden="true" />
                    )}
                    <Badge tone={visibilityMeta[process.visibility]}>
                      {t(`list.visibility.${process.visibility}`)}
                    </Badge>
                  </span>
                </td>
                <td>
                  <div className="seg-proc__row-act">
                    <Tooltip label={t('list.timeline')} side="top">
                      <IconButton
                        size="sm"
                        variant="bare"
                        label={t('list.timeline')}
                        icon={<ListChecks size={15} />}
                        onClick={() => onSteps(process.id)}
                      />
                    </Tooltip>
                    <Tooltip label={t('list.edit')} side="top">
                      <IconButton
                        size="sm"
                        variant="bare"
                        label={t('list.edit')}
                        icon={<Pencil size={15} />}
                        onClick={() => onOpen(process.id)}
                      />
                    </Tooltip>
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

function Progress({ resolved, total }: { resolved: number; total: number }) {
  const { t } = useTranslation('processes')
  if (total === 0) {
    return <span className="seg-proc__prog-empty">{t('list.progress.empty')}</span>
  }
  const shown = Math.min(total, 9)
  const dots = Array.from({ length: shown }, (_, index) => index < resolved)
  return (
    <div
      className="seg-proc__prog"
      title={t('list.progress.label', { resolved, total })}
    >
      <span className="seg-proc__prog-dots" aria-hidden="true">
        {dots.map((done, index) => (
          <span
            key={index}
            className={'seg-proc__prog-dot' + (done ? ' is-done' : ' is-pending')}
          />
        ))}
        {total > 9 && <span className="seg-proc__prog-more">+{total - 9}</span>}
      </span>
      <span className="seg-proc__prog-frac">
        {t('list.progress.fraction', { resolved, total })}
      </span>
    </div>
  )
}

interface DueCellProps {
  dueDate: string | null
  closed: boolean
  language: string
}

function DueCell({ dueDate, closed, language }: DueCellProps) {
  const { t } = useTranslation('processes')
  if (dueDate == null) {
    return <span className="seg-proc__due-none">{t('list.due.none')}</span>
  }
  const urgency = closed ? 'later' : dueUrgency(dueDate)
  return (
    <div className="seg-proc__due">
      <span className="seg-proc__due-date">{formatDate(dueDate, language)}</span>
      {!closed && (
        <span className={`seg-proc__due-rel is-${urgency}`}>
          {relativePhrase(dueDate, t)}
        </span>
      )}
    </div>
  )
}

function relativePhrase(
  dueDate: string,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  const days = daysUntil(dueDate)
  if (days == null) return ''
  if (days === 0) return t('list.due.today')
  if (days === 1) return t('list.due.tomorrow')
  if (days < 0) return t('list.due.overdue', { count: -days })
  return t('list.due.inDays', { count: days })
}
