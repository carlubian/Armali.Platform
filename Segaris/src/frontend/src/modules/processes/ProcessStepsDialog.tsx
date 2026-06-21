import { useQuery } from '@tanstack/react-query'
import { Calendar, Check, Minus, StickyNote } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { isApiError } from '@/app/api/errors'
import {
  processesApi,
  type Process,
  type ProcessStep,
  type StepExecutionState,
} from '@/app/api/processes'
import { formatDate } from '@/app/i18n/formatters'
import { Badge, Dialog, Spinner, type BadgeTone } from '@/components/ui'

import { CategoryGlyph } from './categoryIcons'
import { dueUrgency } from './processDates'

export interface ProcessStepsDialogProps {
  processId: number
  language: string
  onClose: () => void
}

const statusTone: Record<Process['status'], BadgeTone> = {
  NotStarted: 'neutral',
  InProgress: 'aqua',
  Completed: 'success',
  Cancelled: 'danger',
}

const statePill: Record<StepExecutionState, string> = {
  Pending: 'is-pending',
  Completed: 'is-done',
  Skipped: 'is-skipped',
}

/**
 * Read-only step timeline preview. The interactive frontier actions
 * (complete/skip/undo) and the restructure controls arrive in a later Wave; this
 * surface establishes the layout and reflects the live derived status.
 */
export function ProcessStepsDialog({
  processId,
  language,
  onClose,
}: ProcessStepsDialogProps) {
  const { t } = useTranslation('processes')
  const query = useQuery({
    queryKey: ['processes', processId, 'steps'],
    queryFn: ({ signal }) => processesApi.getProcess(processId, signal),
  })

  const process = query.data

  if (query.isPending) {
    return (
      <Dialog
        scrollable
        width={760}
        title={t('steps.title')}
        onClose={onClose}
        closeLabel={t('steps.close')}
      >
        <div className="seg-proc-editor__status">
          <Spinner />
        </div>
      </Dialog>
    )
  }

  if (query.isError || process == null) {
    const notFound = isApiError(query.error) && query.error.kind === 'not-found'
    return (
      <Dialog
        width={620}
        title={t('steps.title')}
        onClose={onClose}
        closeLabel={t('steps.close')}
      >
        <p className="seg-proc-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('steps.loadError')}
        </p>
      </Dialog>
    )
  }

  const steps = process.steps

  return (
    <Dialog
      scrollable
      width={760}
      title={t('steps.title')}
      onClose={onClose}
      closeLabel={t('steps.close')}
    >
      <div className="seg-proc-steps__head">
        <span className="seg-proc-steps__head-icon">
          <CategoryGlyph name={process.categoryName} size={20} />
        </span>
        <div className="seg-proc-steps__head-txt">
          <div className="armali-eyebrow">
            {t('steps.eyebrow', { category: process.categoryName })}
          </div>
          <h2 className="seg-proc-steps__name">{process.name}</h2>
          <div className="seg-proc-steps__sub">
            <Badge
              tone={statusTone[process.status]}
              dot
              pulse={process.status === 'InProgress'}
            >
              {t(`list.status.${process.status}`)}
            </Badge>
            <span className="seg-proc-steps__metng">
              {steps.length === 0
                ? t('steps.noSteps')
                : t('steps.statusLabel', {
                    resolved: process.resolvedStepCount,
                    total: process.totalStepCount,
                  })}
            </span>
          </div>
        </div>
      </div>

      {steps.length === 0 ? (
        <p className="seg-proc-steps__empty">{t('steps.empty')}</p>
      ) : (
        <ol className="seg-proc-steps__list">
          {steps.map((step) => (
            <StepRow
              key={step.id}
              step={step}
              frontier={step.id === process.nextPendingStepId}
              language={language}
            />
          ))}
        </ol>
      )}

      <p className="seg-proc-steps__note">{t('steps.note')}</p>
    </Dialog>
  )
}

interface StepRowProps {
  step: ProcessStep
  frontier: boolean
  language: string
}

function StepRow({ step, frontier, language }: StepRowProps) {
  const { t } = useTranslation('processes')
  const urgency = dueUrgency(step.dueDate)
  return (
    <li
      className={
        'seg-proc-step ' + statePill[step.state] + (frontier ? ' is-frontier' : '')
      }
    >
      <span className="seg-proc-step__orb" aria-hidden="true">
        {step.state === 'Completed' ? (
          <Check size={15} />
        ) : step.state === 'Skipped' ? (
          <Minus size={15} />
        ) : null}
      </span>
      <div className="seg-proc-step__main">
        <div className="seg-proc-step__desc">{step.description}</div>
        <div className="seg-proc-step__meta">
          {step.dueDate != null && (
            <span className={`seg-proc-step__due is-${urgency}`}>
              <Calendar size={12} aria-hidden="true" />
              {formatDate(step.dueDate, language)}
            </span>
          )}
          {step.isOptional && step.state !== 'Skipped' && (
            <span className="seg-proc-state-pill is-skipped">
              {t('steps.optional')}
            </span>
          )}
          {step.notes != null && (
            <span className="seg-proc-step__note-text">
              <StickyNote size={12} aria-hidden="true" />
              {step.notes}
            </span>
          )}
        </div>
      </div>
      <span className={'seg-proc-state-pill ' + statePill[step.state]}>
        {frontier && step.state === 'Pending'
          ? t('steps.current')
          : t(`steps.state.${step.state}`)}
      </span>
    </li>
  )
}
