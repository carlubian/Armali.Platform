import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowDown,
  ArrowUp,
  Calendar,
  Check,
  Minus,
  Plus,
  RotateCcw,
  Save,
  StickyNote,
  Trash2,
} from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { isApiError } from '@/app/api/errors'
import { launcherKeys } from '@/app/api/launcher'
import {
  processesApi,
  type Process,
  type ProcessStep,
  type StepExecutionState,
  type StepListItemRequest,
} from '@/app/api/processes'
import { formatDate } from '@/app/i18n/formatters'
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  Input,
  Spinner,
  type BadgeTone,
} from '@/components/ui'

import { CategoryGlyph } from './categoryIcons'
import { processesKeys } from './contracts'
import { dueUrgency } from './processDates'

export interface ProcessStepsDialogProps {
  processId: number
  language: string
  onClose: () => void
}

type DraftStep = StepListItemRequest & {
  clientId: string
  state: StepExecutionState
}

type ActionKind = 'complete' | 'skip' | 'undo'

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

export function ProcessStepsDialog({
  processId,
  language,
  onClose,
}: ProcessStepsDialogProps) {
  const { t } = useTranslation('processes')
  const queryClient = useQueryClient()
  const [draftOverride, setDraftOverride] = useState<DraftStep[] | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [structureError, setStructureError] = useState<string | null>(null)

  const query = useQuery({
    queryKey: processesKeys.process(processId),
    queryFn: ({ signal }) => processesApi.getProcess(processId, signal),
  })

  const process = query.data

  const publishProcess = (saved: Process) => {
    queryClient.setQueryData(processesKeys.process(saved.id), saved)
    setDraftOverride(null)
    void queryClient.invalidateQueries({ queryKey: processesKeys.all })
    void queryClient.invalidateQueries({ queryKey: launcherKeys.attention() })
  }

  const frontierMutation = useMutation({
    mutationFn: ({ kind, stepId }: { kind: ActionKind; stepId: number }) => {
      switch (kind) {
        case 'complete':
          return processesApi.completeStep(processId, stepId)
        case 'skip':
          return processesApi.skipStep(processId, stepId)
        case 'undo':
          return processesApi.undoStep(processId, stepId)
      }
    },
    onSuccess: publishProcess,
    onError: (error) => setActionError(mapStepError(error, t)),
  })

  const restructureMutation = useMutation({
    mutationFn: (steps: StepListItemRequest[]) =>
      processesApi.updateSteps(processId, { steps }),
    onSuccess: publishProcess,
    onError: (error) => setStructureError(mapStepError(error, t)),
  })

  const saving = restructureMutation.isPending
  const acting = frontierMutation.isPending

  if (query.isPending) {
    return (
      <Dialog
        scrollable
        width={860}
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

  const baseDraft = toDraft(process.steps)
  const draft = draftOverride ?? baseDraft
  const dirty = draftOverride != null
  const validationError = validateDraft(draft, t)
  const latestResolvedId = latestResolvedStepId(process.steps)
  const canSave = dirty && validationError == null && !saving

  const mutateDraft = (mapper: (current: DraftStep[]) => DraftStep[]) => {
    setDraftOverride((current) => mapper(current ?? baseDraft))
    setStructureError(null)
  }

  const submitRestructure = () => {
    const error = validateDraft(draft, t)
    if (error != null) {
      setStructureError(error)
      return
    }
    setStructureError(null)
    restructureMutation.mutate(
      draft.map(({ id, description, dueDate, notes, isOptional }) => ({
        id,
        description: description.trim(),
        dueDate,
        notes,
        isOptional,
      })),
    )
  }

  return (
    <Dialog
      scrollable
      width={860}
      title={t('steps.title')}
      onClose={onClose}
      closeLabel={t('steps.close')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={saving}>
            {t('steps.close')}
          </Button>
          <Button
            iconLeft={<Save size={15} />}
            disabled={!canSave}
            onClick={submitRestructure}
          >
            {saving ? t('steps.restructure.saving') : t('steps.restructure.save')}
          </Button>
        </>
      }
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
          <div className="seg-proc-steps__sub" aria-live="polite">
            <Badge
              tone={statusTone[process.status]}
              dot
              pulse={process.status === 'InProgress'}
            >
              {t(`list.status.${process.status}`)}
            </Badge>
            <span className="seg-proc-steps__metng">
              {process.steps.length === 0
                ? t('steps.noSteps')
                : t('steps.statusLabel', {
                    resolved: process.resolvedStepCount,
                    total: process.totalStepCount,
                  })}
            </span>
          </div>
        </div>
      </div>

      {actionError != null && (
        <p className="seg-proc-editor__error" role="alert">
          {actionError}
        </p>
      )}
      {(structureError ?? validationError) != null && (
        <p className="seg-proc-editor__error" role="alert">
          {structureError ?? validationError}
        </p>
      )}

      {draft.length === 0 ? (
        <p className="seg-proc-steps__empty">{t('steps.empty')}</p>
      ) : (
        <ol className="seg-proc-steps__list">
          {draft.map((step, index) => (
            <StepRow
              key={step.clientId}
              step={step}
              index={index}
              total={draft.length}
              frontier={step.id === process.nextPendingStepId}
              latestResolved={step.id != null && step.id === latestResolvedId}
              language={language}
              busy={acting || saving}
              onAction={(kind) => {
                if (step.id == null) return
                setActionError(null)
                frontierMutation.mutate({ kind, stepId: step.id })
              }}
              onPatch={(patch) =>
                mutateDraft((current) =>
                  current.map((candidate) =>
                    candidate.clientId === step.clientId
                      ? { ...candidate, ...patch }
                      : candidate,
                  ),
                )
              }
              onMove={(direction) =>
                mutateDraft((current) => moveStep(current, index, direction))
              }
              onRemove={() =>
                mutateDraft((current) =>
                  current.filter((candidate) => candidate.clientId !== step.clientId),
                )
              }
            />
          ))}
        </ol>
      )}

      <div className="seg-proc-steps__toolbar">
        <Button
          variant="outline"
          iconLeft={<Plus size={15} />}
          onClick={() => mutateDraft((current) => [...current, newDraftStep()])}
          disabled={saving}
        >
          {t('steps.restructure.add')}
        </Button>
        <p className="seg-proc-steps__note">{t('steps.note')}</p>
      </div>
    </Dialog>
  )
}

interface StepRowProps {
  step: DraftStep
  index: number
  total: number
  frontier: boolean
  latestResolved: boolean
  language: string
  busy: boolean
  onAction: (kind: ActionKind) => void
  onPatch: (patch: Partial<DraftStep>) => void
  onMove: (direction: 'up' | 'down') => void
  onRemove: () => void
}

function StepRow({
  step,
  index,
  total,
  frontier,
  latestResolved,
  language,
  busy,
  onAction,
  onPatch,
  onMove,
  onRemove,
}: StepRowProps) {
  const { t } = useTranslation('processes')
  const urgency = dueUrgency(step.dueDate)
  const completeDisabled =
    busy || step.id == null || !frontier || step.state !== 'Pending'
  const skipDisabled = completeDisabled || !step.isOptional
  const undoDisabled = busy || step.id == null || !latestResolved

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
        <Input
          label={t('steps.restructure.description')}
          value={step.description}
          onChange={(event) => onPatch({ description: event.target.value })}
          required
        />
        <div className="seg-proc-step__meta">
          {step.dueDate != null && (
            <span className={`seg-proc-step__due is-${urgency}`}>
              <Calendar size={12} aria-hidden="true" />
              {formatDate(step.dueDate, language)}
            </span>
          )}
          {step.notes != null && step.notes.length > 0 && (
            <span className="seg-proc-step__note-text">
              <StickyNote size={12} aria-hidden="true" />
              {step.notes}
            </span>
          )}
        </div>
        <div className="seg-proc-step__edit-grid">
          <Input
            label={t('steps.restructure.dueDate')}
            type="date"
            value={step.dueDate ?? ''}
            onChange={(event) => onPatch({ dueDate: emptyToNull(event.target.value) })}
          />
          <Input
            label={t('steps.restructure.notes')}
            value={step.notes ?? ''}
            onChange={(event) => onPatch({ notes: emptyToNull(event.target.value) })}
          />
          <Checkbox
            className="seg-proc-step__optional"
            label={t('steps.optional')}
            checked={step.isOptional}
            onChange={(event) => onPatch({ isOptional: event.target.checked })}
          />
        </div>
      </div>
      <div className="seg-proc-step__side">
        <span className={'seg-proc-state-pill ' + statePill[step.state]}>
          {frontier && step.state === 'Pending'
            ? t('steps.current')
            : t(`steps.state.${step.state}`)}
        </span>
        <div className="seg-proc-step__actions">
          <Button
            size="sm"
            variant="outline"
            disabled={completeDisabled}
            onClick={() => onAction('complete')}
          >
            {t('steps.actions.complete')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            disabled={skipDisabled}
            onClick={() => onAction('skip')}
          >
            {t('steps.actions.skip')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            iconLeft={<RotateCcw size={13} />}
            disabled={undoDisabled}
            onClick={() => onAction('undo')}
          >
            {t('steps.actions.undo')}
          </Button>
        </div>
        <div className="seg-proc-step__actions">
          <Button
            size="sm"
            variant="ghost"
            iconLeft={<ArrowUp size={13} />}
            disabled={busy || index === 0}
            onClick={() => onMove('up')}
          >
            {t('steps.restructure.up')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            iconLeft={<ArrowDown size={13} />}
            disabled={busy || index === total - 1}
            onClick={() => onMove('down')}
          >
            {t('steps.restructure.down')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            className="seg-proc-step__remove"
            iconLeft={<Trash2 size={13} />}
            disabled={busy}
            onClick={onRemove}
          >
            {t('steps.restructure.remove')}
          </Button>
        </div>
      </div>
    </li>
  )
}

function toDraft(steps: ProcessStep[]): DraftStep[] {
  return steps.map((step) => ({
    id: step.id,
    clientId: String(step.id),
    description: step.description,
    dueDate: step.dueDate,
    notes: step.notes,
    isOptional: step.isOptional,
    state: step.state,
  }))
}

function newDraftStep(): DraftStep {
  return {
    id: null,
    clientId: `new-${crypto.randomUUID()}`,
    description: '',
    dueDate: null,
    notes: null,
    isOptional: false,
    state: 'Pending',
  }
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim()
  return trimmed.length === 0 ? null : trimmed
}

function moveStep(
  steps: DraftStep[],
  index: number,
  direction: 'up' | 'down',
): DraftStep[] {
  const target = direction === 'up' ? index - 1 : index + 1
  if (target < 0 || target >= steps.length) return steps
  const copy = [...steps]
  const [removed] = copy.splice(index, 1)
  copy.splice(target, 0, removed)
  return copy
}

function latestResolvedStepId(steps: ProcessStep[]): number | null {
  for (let index = steps.length - 1; index >= 0; index -= 1) {
    if (steps[index].state !== 'Pending') return steps[index].id
  }
  return null
}

function validateDraft(draft: DraftStep[], t: (key: string) => string): string | null {
  let pendingSeen = false
  for (const step of draft) {
    if (step.description.trim().length === 0) {
      return t('steps.restructure.errors.description')
    }
    if (step.description.trim().length > 500) {
      return t('steps.restructure.errors.descriptionLong')
    }
    if (step.notes != null && step.notes.length > 1000) {
      return t('steps.restructure.errors.notesLong')
    }
    if (step.dueDate != null && !/^\d{4}-\d{2}-\d{2}$/.test(step.dueDate)) {
      return t('steps.restructure.errors.date')
    }
    if (step.state === 'Pending') {
      pendingSeen = true
    } else if (pendingSeen) {
      return t('steps.restructure.errors.contiguity')
    }
  }
  return null
}

function mapStepError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    const code = error.problem?.code ?? ''
    if (code.includes('contiguity')) return t('steps.restructure.errors.contiguity')
    if (code.includes('frontier')) return t('steps.actions.errors.frontier')
    if (code.includes('optional')) return t('steps.actions.errors.optional')
    if (error.kind === 'not-found') return t('editor.notFound')
  }
  return t('steps.restructure.errors.generic')
}
