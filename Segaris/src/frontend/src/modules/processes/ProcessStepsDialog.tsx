import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { TFunction } from 'i18next'
import {
  ArrowDown,
  ArrowUp,
  Calendar,
  Check,
  ChevronsRight,
  Flag,
  Info,
  ListRestart,
  Minus,
  PartyPopper,
  Plus,
  RotateCcw,
  Save,
  StickyNote,
  Trash2,
} from 'lucide-react'
import { Fragment, useEffect, useRef, useState, type ReactNode } from 'react'
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
import { daysUntil, dueUrgency } from './processDates'

export interface ProcessStepsDialogProps {
  processId: number
  language: string
  mode: StepDialogMode
  onClose: () => void
  onRestructure: () => void
  onBackToTimeline: () => void
}

type DraftStep = StepListItemRequest & {
  clientId: string
  state: StepExecutionState
}

type ActionKind = 'complete' | 'skip' | 'undo'
type StepDialogMode = 'timeline' | 'restructure'
type StepField = 'description' | 'dueDate' | 'notes'
type RowErrors = Partial<Record<StepField, string>>

interface DraftValidation {
  banner: string | null
  rows: Record<string, RowErrors>
}

interface RestructureErrors extends DraftValidation {
  server: string | null
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

export function ProcessStepsDialog({
  processId,
  language,
  mode,
  onClose,
  onRestructure,
  onBackToTimeline,
}: ProcessStepsDialogProps) {
  const { t } = useTranslation('processes')
  const queryClient = useQueryClient()
  const [draftOverride, setDraftOverride] = useState<DraftStep[] | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [structureErrors, setStructureErrors] =
    useState<RestructureErrors>(emptyRestructureErrors)
  const restructureButtonRef = useRef<HTMLButtonElement>(null)
  const previousMode = useRef<StepDialogMode>(mode)

  useEffect(() => {
    if (previousMode.current === 'restructure' && mode === 'timeline') {
      restructureButtonRef.current?.focus()
    }
    previousMode.current = mode
  }, [mode])

  const query = useQuery({
    queryKey: processesKeys.process(processId),
    queryFn: ({ signal }) => processesApi.getProcess(processId, signal),
  })

  const process = query.data

  const publishProcess = (saved: Process) => {
    queryClient.setQueryData(processesKeys.process(saved.id), saved)
    setDraftOverride(null)
    void queryClient.invalidateQueries({ queryKey: processesKeys.process(saved.id) })
    void queryClient.invalidateQueries({ queryKey: processesKeys.steps(saved.id) })
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
    onSuccess: (saved) => {
      publishProcess(saved)
      onBackToTimeline()
    },
    onError: (error) => setStructureErrors(mapRestructureError(error, draft, t)),
  })

  const saving = restructureMutation.isPending
  const acting = frontierMutation.isPending

  if (query.isPending) {
    return (
      <Dialog
        scrollable
        width={980}
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
  const validation = validateDraft(draft, t)
  const activeErrors = mergeRestructureErrors(validation, structureErrors)
  const canSave = dirty && !hasDraftErrors(activeErrors) && !saving

  const mutateDraft = (mapper: (current: DraftStep[]) => DraftStep[]) => {
    setDraftOverride((current) => mapper(current ?? baseDraft))
    setStructureErrors(emptyRestructureErrors())
  }

  const submitRestructure = () => {
    const errors = validateDraft(draft, t)
    if (hasDraftErrors(errors)) {
      setStructureErrors({ ...errors, server: null })
      return
    }
    setStructureErrors(emptyRestructureErrors())
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

  const closeRestructure = () => {
    setDraftOverride(null)
    setStructureErrors(emptyRestructureErrors())
    onBackToTimeline()
  }

  const footer =
    mode === 'timeline' ? (
      <>
        <span className="seg-proc-steps__foot-note">
          <Info size={14} aria-hidden="true" />
          {t('steps.frontier.rules')}
        </span>
        <Button
          ref={restructureButtonRef}
          variant="outline"
          iconLeft={<ListRestart size={16} />}
          onClick={() => {
            setStructureErrors(emptyRestructureErrors())
            onRestructure()
          }}
        >
          {t('steps.restructure.open')}
        </Button>
        <Button variant="ghost" onClick={onClose} disabled={acting}>
          {t('steps.close')}
        </Button>
      </>
    ) : (
      <>
        <Button variant="ghost" onClick={closeRestructure} disabled={saving}>
          {t('steps.restructure.back')}
        </Button>
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
    )

  return (
    <Dialog
      scrollable
      width={980}
      title={mode === 'timeline' ? t('steps.title') : t('steps.restructure.title')}
      description={
        mode === 'timeline'
          ? t('steps.timelineDescription')
          : t('steps.restructure.descriptionText')
      }
      onClose={mode === 'timeline' ? onClose : closeRestructure}
      closeLabel={t('steps.close')}
      footer={footer}
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
          <TimelineMetadata process={process} language={language} />
        </div>
      </div>

      {actionError != null && (
        <p className="seg-proc-editor__error" role="alert">
          {actionError}
        </p>
      )}

      {mode === 'timeline' ? (
        <TimelineView
          process={process}
          language={language}
          busy={acting}
          onAction={(kind, stepId) => {
            setActionError(null)
            frontierMutation.mutate({ kind, stepId })
          }}
          onRestructure={onRestructure}
        />
      ) : (
        <RestructureView
          draft={draft}
          process={process}
          language={language}
          saving={saving}
          errors={activeErrors}
          onPatch={(clientId, patch) =>
            mutateDraft((current) =>
              current.map((candidate) =>
                candidate.clientId === clientId
                  ? { ...candidate, ...patch }
                  : candidate,
              ),
            )
          }
          onMove={(index, direction) =>
            mutateDraft((current) => moveStep(current, index, direction))
          }
          onRemove={(clientId) =>
            mutateDraft((current) =>
              current.filter((candidate) => candidate.clientId !== clientId),
            )
          }
          onAdd={() => mutateDraft((current) => [...current, newDraftStep()])}
        />
      )}
    </Dialog>
  )
}

interface TimelineMetadataProps {
  process: Process
  language: string
}

function TimelineMetadata({ process, language }: TimelineMetadataProps) {
  const { t } = useTranslation('processes')
  const due = process.effectiveDueDate
  const urgency = dueUrgency(due)
  return (
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
      <span className="seg-proc-steps__metng">
        {t('steps.metadata.category', { category: process.categoryName })}
      </span>
      <span className={`seg-proc-steps__metng is-${urgency}`}>
        {due == null
          ? t('steps.metadata.noDue')
          : t('steps.metadata.due', { date: formatDate(due, language) })}
      </span>
    </div>
  )
}

interface TimelineViewProps {
  process: Process
  language: string
  busy: boolean
  onAction: (kind: ActionKind, stepId: number) => void
  onRestructure: () => void
}

function TimelineView({
  process,
  language,
  busy,
  onAction,
  onRestructure,
}: TimelineViewProps) {
  const { t } = useTranslation('processes')
  const steps = process.steps
  const frontier = frontierStep(process)
  const latestResolved = latestResolvedStep(process.steps)
  const completed = steps.length > 0 && frontier == null
  const cancelled = process.status === 'Cancelled' || process.isCancelled
  const executionLocked = busy || cancelled

  return (
    <div className="seg-proc-timeline">
      {steps.length > 0 && (
        <div className="seg-proc-trackwrap">
          <ol className="seg-proc-track" aria-label={t('steps.trackLabel')}>
            {steps.map((step, index) => (
              <TrackNode
                key={step.id}
                step={step}
                index={index}
                total={steps.length}
                frontier={frontier?.id === step.id && !cancelled}
                previousResolved={index > 0 && steps[index - 1].state !== 'Pending'}
                language={language}
              />
            ))}
          </ol>
        </div>
      )}

      {completed ? (
        <FrontierBar
          tone="done"
          icon={<PartyPopper size={20} />}
          eyebrow={t('steps.frontier.completeEyebrow')}
          title={t('steps.frontier.completeTitle')}
          body={t('steps.frontier.completeBody')}
          actions={
            latestResolved == null ? null : (
              <Button
                variant="outline"
                size="sm"
                iconLeft={<RotateCcw size={15} />}
                disabled={executionLocked}
                onClick={() => onAction('undo', latestResolved.id)}
                title={t('steps.actions.undoHint')}
              >
                {busy ? t('steps.actions.working') : t('steps.actions.undoLast')}
              </Button>
            )
          }
        />
      ) : steps.length === 0 ? (
        <FrontierBar
          icon={<ListRestart size={20} />}
          eyebrow={t('steps.frontier.emptyEyebrow')}
          title={t('steps.frontier.emptyTitle')}
          body={t('steps.frontier.emptyBody')}
          actions={
            <Button
              variant="outline"
              size="sm"
              iconLeft={<ListRestart size={15} />}
              onClick={onRestructure}
            >
              {t('steps.restructure.open')}
            </Button>
          }
        />
      ) : frontier != null ? (
        <FrontierBar
          icon={<Flag size={20} />}
          eyebrow={
            cancelled
              ? t('steps.frontier.cancelledEyebrow')
              : t('steps.frontier.pendingEyebrow')
          }
          title={frontier.description}
          body={<FrontierBody step={frontier} language={language} />}
          actions={
            <>
              {latestResolved != null && (
                <Button
                  variant="ghost"
                  size="sm"
                  iconLeft={<RotateCcw size={15} />}
                  disabled={executionLocked}
                  onClick={() => onAction('undo', latestResolved.id)}
                  title={t('steps.actions.undoHint')}
                >
                  {t('steps.actions.undo')}
                </Button>
              )}
              <Button
                variant="outline"
                size="sm"
                iconLeft={<ChevronsRight size={15} />}
                disabled={executionLocked || !frontier.isOptional}
                onClick={() => onAction('skip', frontier.id)}
                title={
                  frontier.isOptional
                    ? t('steps.actions.skipHint')
                    : t('steps.actions.skipDisabled')
                }
              >
                {t('steps.actions.skip')}
              </Button>
              <Button
                size="sm"
                iconLeft={<Check size={15} />}
                disabled={executionLocked}
                onClick={() => onAction('complete', frontier.id)}
                title={t('steps.actions.completeHint')}
              >
                {busy ? t('steps.actions.working') : t('steps.actions.completeStep')}
              </Button>
            </>
          }
        />
      ) : null}
    </div>
  )
}

interface TrackNodeProps {
  step: ProcessStep
  index: number
  total: number
  frontier: boolean
  previousResolved: boolean
  language: string
}

function TrackNode({
  step,
  index,
  total,
  frontier,
  previousResolved,
  language,
}: TrackNodeProps) {
  const { t } = useTranslation('processes')
  const urgency = dueUrgency(step.dueDate)
  const stateClass = frontier ? 'is-frontier' : statePill[step.state]
  const stateText = frontier ? t('steps.current') : t(`steps.state.${step.state}`)
  return (
    <>
      {index > 0 && (
        <li
          className={`seg-proc-link ${previousResolved ? 'is-done' : 'is-pending'}`}
          aria-hidden="true"
        />
      )}
      <li
        className={`seg-proc-node ${stateClass}`}
        aria-label={t('steps.nodeLabel', {
          index: index + 1,
          total,
          description: step.description,
          state: stateText,
        })}
      >
        <span className={`seg-proc-node__orb ${stateClass}`} aria-hidden="true">
          {step.state === 'Completed' ? (
            <Check size={22} />
          ) : step.state === 'Skipped' ? (
            <Minus size={20} />
          ) : (
            index + 1
          )}
        </span>
        <span className="seg-proc-node__num">{stateText}</span>
        <span className="seg-proc-node__desc">{step.description}</span>
        {step.dueDate != null && (
          <span className={`seg-proc-node__due is-${urgency}`}>
            <Calendar size={11} aria-hidden="true" />
            {formatDate(step.dueDate, language)}
          </span>
        )}
        {step.isOptional && step.state !== 'Skipped' && (
          <span className="seg-proc-node__flag">{t('steps.optional')}</span>
        )}
      </li>
    </>
  )
}

interface FrontierBarProps {
  tone?: 'frontier' | 'done'
  icon: ReactNode
  eyebrow: string
  title: string
  body: ReactNode
  actions: ReactNode
}

function FrontierBar({
  tone = 'frontier',
  icon,
  eyebrow,
  title,
  body,
  actions,
}: FrontierBarProps) {
  return (
    <section
      className={`seg-proc-frontier ${tone === 'done' ? 'seg-proc-frontier--done' : ''}`}
    >
      <span className="seg-proc-frontier__icon" aria-hidden="true">
        {icon}
      </span>
      <div className="seg-proc-frontier__txt">
        <div className="armali-eyebrow">{eyebrow}</div>
        <h3>{title}</h3>
        <div className="seg-proc-frontier__body">{body}</div>
      </div>
      <div className="seg-proc-frontier__act">{actions}</div>
    </section>
  )
}

interface FrontierBodyProps {
  step: ProcessStep
  language: string
}

function FrontierBody({ step, language }: FrontierBodyProps) {
  const { t } = useTranslation('processes')
  return (
    <>
      {step.dueDate == null
        ? t('steps.frontier.noDue')
        : t('steps.frontier.due', {
            date: formatDate(step.dueDate, language),
            relative: dueRelative(step.dueDate, t),
          })}
      {step.notes != null && step.notes.trim().length > 0 ? ` ${step.notes}` : ''}
    </>
  )
}

interface RestructureViewProps {
  draft: DraftStep[]
  process: Process
  language: string
  saving: boolean
  errors: RestructureErrors
  onPatch: (clientId: string, patch: Partial<DraftStep>) => void
  onMove: (index: number, direction: 'up' | 'down') => void
  onRemove: (clientId: string) => void
  onAdd: () => void
}

function RestructureView({
  draft,
  process,
  language,
  saving,
  errors,
  onPatch,
  onMove,
  onRemove,
  onAdd,
}: RestructureViewProps) {
  const { t } = useTranslation('processes')
  const lockedCount = resolvedPrefixCount(process.steps)
  const latestResolvedId = latestResolvedStepId(process.steps)
  const divider = (
    <li
      className="seg-proc-frontier-divider"
      aria-label={t('steps.restructure.frontierDivider')}
    >
      <span>{t('steps.restructure.lockedPrefix')}</span>
      <strong>{t('steps.restructure.editableTail')}</strong>
    </li>
  )

  return (
    <div className="seg-proc-restructure">
      <div
        className={[
          'seg-proc-restructure__banner',
          errors.banner != null || errors.server != null ? 'is-error' : '',
        ]
          .filter(Boolean)
          .join(' ')}
        role={errors.banner != null || errors.server != null ? 'alert' : undefined}
      >
        <Info size={16} aria-hidden="true" />
        <p>{errors.server ?? errors.banner ?? t('steps.restructure.invariant')}</p>
      </div>
      {draft.length === 0 ? (
        <p className="seg-proc-steps__empty">{t('steps.empty')}</p>
      ) : (
        <ol className="seg-proc-steps__list">
          {draft.map((step, index) => {
            const locked = index < lockedCount
            return (
              <Fragment key={step.clientId}>
                {index === lockedCount && divider}
                <StepRow
                  step={step}
                  index={index}
                  lockedCount={lockedCount}
                  total={draft.length}
                  locked={locked}
                  frontier={step.id === process.nextPendingStepId}
                  latestResolved={step.id != null && step.id === latestResolvedId}
                  language={language}
                  busy={saving}
                  errors={errors.rows[step.clientId] ?? {}}
                  onPatch={(patch) => onPatch(step.clientId, patch)}
                  onMove={(direction) => onMove(index, direction)}
                  onRemove={() => onRemove(step.clientId)}
                />
              </Fragment>
            )
          })}
          {lockedCount === draft.length && divider}
        </ol>
      )}

      <div className="seg-proc-steps__toolbar">
        <Button
          variant="outline"
          iconLeft={<Plus size={15} />}
          onClick={onAdd}
          disabled={saving}
        >
          {t('steps.restructure.add')}
        </Button>
        <p className="seg-proc-steps__note">{t('steps.note')}</p>
      </div>
    </div>
  )
}

interface StepRowProps {
  step: DraftStep
  index: number
  lockedCount: number
  total: number
  locked: boolean
  frontier: boolean
  latestResolved: boolean
  language: string
  busy: boolean
  errors: RowErrors
  onPatch: (patch: Partial<DraftStep>) => void
  onMove: (direction: 'up' | 'down') => void
  onRemove: () => void
}

function StepRow({
  step,
  index,
  lockedCount,
  total,
  locked,
  frontier,
  latestResolved,
  language,
  busy,
  errors,
  onPatch,
  onMove,
  onRemove,
}: StepRowProps) {
  const { t } = useTranslation('processes')
  const urgency = dueUrgency(step.dueDate)

  return (
    <li
      className={
        'seg-proc-step ' +
        statePill[step.state] +
        (frontier ? ' is-frontier' : '') +
        (locked ? ' is-locked' : '')
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
          label={t('steps.restructure.stepDescription')}
          value={step.description}
          onChange={(event) => onPatch({ description: event.target.value })}
          disabled={busy || locked}
          error={errors.description}
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
            disabled={busy || locked}
            error={errors.dueDate}
          />
          <Input
            label={t('steps.restructure.notes')}
            value={step.notes ?? ''}
            onChange={(event) => onPatch({ notes: emptyToNull(event.target.value) })}
            disabled={busy || locked}
            error={errors.notes}
          />
          <Checkbox
            className="seg-proc-step__optional"
            label={t('steps.optional')}
            checked={step.isOptional}
            onChange={(event) => onPatch({ isOptional: event.target.checked })}
            disabled={busy || locked}
          />
        </div>
      </div>
      <div className="seg-proc-step__side">
        <span className={'seg-proc-state-pill ' + statePill[step.state]}>
          {frontier && step.state === 'Pending'
            ? t('steps.current')
            : latestResolved
              ? t('steps.latestResolved')
              : t(`steps.state.${step.state}`)}
        </span>
        <div className="seg-proc-step__actions">
          <Button
            size="sm"
            variant="ghost"
            iconLeft={<ArrowUp size={13} />}
            disabled={busy || locked || index <= lockedCount}
            onClick={() => onMove('up')}
          >
            {t('steps.restructure.up')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            iconLeft={<ArrowDown size={13} />}
            disabled={busy || locked || index === total - 1}
            onClick={() => onMove('down')}
          >
            {t('steps.restructure.down')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            className="seg-proc-step__remove"
            iconLeft={<Trash2 size={13} />}
            disabled={busy || locked}
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
    clientId: `new-${randomClientId()}`,
    description: '',
    dueDate: null,
    notes: null,
    isOptional: false,
    state: 'Pending',
  }
}

// crypto.randomUUID is only defined in secure contexts (HTTPS or localhost).
// Segaris is served over plain HTTP on the household network, so fall back to a
// locally-unique id when it is unavailable to keep "add step" from throwing.
function randomClientId(): string {
  const globalCrypto = globalThis.crypto
  if (typeof globalCrypto?.randomUUID === 'function') {
    return globalCrypto.randomUUID()
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`
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

function resolvedPrefixCount(steps: ProcessStep[]): number {
  const firstPending = steps.findIndex((step) => step.state === 'Pending')
  return firstPending === -1 ? steps.length : firstPending
}

function frontierStep(process: Process): ProcessStep | null {
  if (process.nextPendingStepId != null) {
    return process.steps.find((step) => step.id === process.nextPendingStepId) ?? null
  }
  return process.steps.find((step) => step.state === 'Pending') ?? null
}

function latestResolvedStep(steps: ProcessStep[]): ProcessStep | null {
  for (let index = steps.length - 1; index >= 0; index -= 1) {
    if (steps[index].state !== 'Pending') return steps[index]
  }
  return null
}

function latestResolvedStepId(steps: ProcessStep[]): number | null {
  return latestResolvedStep(steps)?.id ?? null
}

function dueRelative(civil: string, t: TFunction<'processes'>): string {
  const days = daysUntil(civil)
  if (days == null) return t('steps.frontier.relativeUnknown')
  if (days < 0) return t('list.due.overdue', { count: Math.abs(days) })
  if (days === 0) return t('list.due.today')
  if (days === 1) return t('list.due.tomorrow')
  return t('list.due.inDays', { count: days })
}

function emptyRestructureErrors(): RestructureErrors {
  return { banner: null, rows: {}, server: null }
}

function validateDraft(
  draft: DraftStep[],
  t: (key: string) => string,
): DraftValidation {
  const rows: Record<string, RowErrors> = {}
  let banner: string | null = null
  let pendingSeen = false
  for (const step of draft) {
    const stepErrors: RowErrors = {}
    if (step.description.trim().length === 0) {
      stepErrors.description = t('steps.restructure.errors.description')
    }
    if (step.description.trim().length > 500) {
      stepErrors.description = t('steps.restructure.errors.descriptionLong')
    }
    if (step.notes != null && step.notes.length > 1000) {
      stepErrors.notes = t('steps.restructure.errors.notesLong')
    }
    if (step.dueDate != null && !/^\d{4}-\d{2}-\d{2}$/.test(step.dueDate)) {
      stepErrors.dueDate = t('steps.restructure.errors.date')
    }
    if (Object.keys(stepErrors).length > 0) {
      rows[step.clientId] = stepErrors
    }
    if (step.state === 'Pending') {
      pendingSeen = true
    } else if (pendingSeen) {
      banner = t('steps.restructure.errors.contiguity')
    }
  }
  return { banner, rows }
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

function mergeRestructureErrors(
  validation: DraftValidation,
  operation: RestructureErrors,
): RestructureErrors {
  return {
    banner: operation.banner ?? validation.banner,
    rows: { ...validation.rows, ...operation.rows },
    server: operation.server,
  }
}

function hasDraftErrors(errors: DraftValidation): boolean {
  return (
    errors.banner != null ||
    Object.values(errors.rows).some((row) => Object.keys(row).length > 0)
  )
}

function mapRestructureError(
  error: unknown,
  draft: DraftStep[],
  t: (key: string) => string,
): RestructureErrors {
  const mapped = emptyRestructureErrors()
  if (isApiError(error)) {
    const code = error.problem?.code ?? ''
    if (code.includes('contiguity')) {
      mapped.banner = t('steps.restructure.errors.contiguity')
      return mapped
    }
    if (error.problem?.errors != null) {
      for (const [path, messages] of Object.entries(error.problem.errors)) {
        const rowMatch = path.match(/steps\[(\d+)]\.(description|dueDate|notes)/i)
        if (rowMatch == null) continue
        const index = Number(rowMatch[1])
        const field = rowMatch[2] as StepField
        const clientId = draft[index]?.clientId
        if (clientId == null) continue
        mapped.rows[clientId] = {
          ...mapped.rows[clientId],
          [field]: messages[0] ?? t('steps.restructure.errors.generic'),
        }
      }
      if (hasDraftErrors(mapped)) return mapped
    }
    if (error.kind === 'not-found') {
      mapped.server = t('editor.notFound')
      return mapped
    }
  }
  mapped.server = t('steps.restructure.errors.generic')
  return mapped
}
