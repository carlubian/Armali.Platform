import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Ban,
  CircleDashed,
  Globe,
  ListChecks,
  Lock,
  RotateCcw,
  Sparkles,
  Trash2,
} from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { isApiError } from '@/app/api/errors'
import {
  processesApi,
  type CreateProcessRequest,
  type Process,
  type ProcessCategory,
  type ProcessStatus,
  type ProcessVisibility,
} from '@/app/api/processes'
import {
  Badge,
  Button,
  Dialog,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  type BadgeTone,
  type SegmentTone,
} from '@/components/ui'

import { ProcessAttachments } from './ProcessAttachments'
import { StagedProcessAttachments } from './StagedProcessAttachments'
import {
  buildDefaults,
  createProcessSchema,
  fromProcess,
  toRequest,
  type ProcessFormValues,
} from './processForm'
import { useProcessCategories } from './queries'

export interface ProcessDialogProps {
  mode: 'create' | 'edit'
  processId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (process: Process, mode: 'create' | 'edit') => void
  onDeleted: (process: Process) => void
  onLifecycle: (process: Process, kind: 'cancelled' | 'reopened') => void
  onManageSteps: (processId: number) => void
}

const visibilities: ProcessVisibility[] = ['Public', 'Private']

const visibilityMeta: Record<
  ProcessVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

const statusTone: Record<ProcessStatus, BadgeTone> = {
  NotStarted: 'neutral',
  InProgress: 'aqua',
  Completed: 'success',
  Cancelled: 'danger',
}

export function ProcessDialog({
  mode,
  processId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
  onLifecycle,
  onManageSteps,
}: ProcessDialogProps) {
  const { t } = useTranslation('processes')
  const categories = useProcessCategories()

  const processQuery = useQuery({
    queryKey: ['processes', processId as number],
    queryFn: ({ signal }) => processesApi.getProcess(processId as number, signal),
    enabled: mode === 'edit' && processId != null,
  })

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  if (categories.data == null || (mode === 'edit' && processQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={620}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-proc-editor__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && processQuery.isError) {
    const notFound =
      isApiError(processQuery.error) && processQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={620}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-proc-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const process = mode === 'edit' ? (processQuery.data as Process) : undefined
  const initialValues =
    process != null
      ? fromProcess(process)
      : buildDefaults(firstCatalogId(categories.data))
  const canChangeVisibility =
    process == null || (currentUserId != null && process.createdById === currentUserId)

  return (
    <ProcessEditorForm
      mode={mode}
      processId={processId}
      process={process}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
      onLifecycle={onLifecycle}
      onManageSteps={onManageSteps}
    />
  )
}

function firstCatalogId(items: ReadonlyArray<{ id: number }> | undefined): string {
  const first = items?.[0]
  return first != null ? String(first.id) : ''
}

interface ProcessEditorFormProps {
  mode: 'create' | 'edit'
  processId?: number
  process?: Process
  title: string
  description: string
  initialValues: ProcessFormValues
  categories: ProcessCategory[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (process: Process, mode: 'create' | 'edit') => void
  onDeleted: (process: Process) => void
  onLifecycle: (process: Process, kind: 'cancelled' | 'reopened') => void
  onManageSteps: (processId: number) => void
}

function ProcessEditorForm({
  mode,
  processId,
  process,
  title,
  description,
  initialValues,
  categories,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
  onLifecycle,
  onManageSteps,
}: ProcessEditorFormProps) {
  const { t } = useTranslation('processes')
  const schema = useMemo(
    () =>
      createProcessSchema({
        nameRequired: t('editor.validation.nameRequired'),
        nameTooLong: t('editor.validation.nameTooLong'),
        categoryRequired: t('editor.validation.categoryRequired'),
        dateInvalid: t('editor.validation.dateInvalid'),
        notesTooLong: t('editor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<ProcessFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState } = form

  const [live, setLive] = useState<Process | undefined>(process)
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdProcess, setCreatedProcess] = useState<Process | null>(null)
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CreateProcessRequest) =>
      mode === 'create'
        ? processesApi.createProcess(request)
        : processesApi.updateProcess(processId as number, request),
    onSuccess: (saved) => {
      setLive(saved)
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedProcess(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const lifecycleMutation = useMutation({
    mutationFn: (kind: 'cancel' | 'reopen') =>
      kind === 'cancel'
        ? processesApi.cancelProcess(processId as number)
        : processesApi.reopenProcess(processId as number),
    onSuccess: (saved, kind) => {
      setLive(saved)
      onLifecycle(saved, kind === 'cancel' ? 'cancelled' : 'reopened')
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => processesApi.deleteProcess(processId as number),
    onSuccess: () => {
      if (process != null) onDeleted(process)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapServerError(error, t))
    },
  })

  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    void handleSubmit((values) => {
      setServerError(null)
      mutation.mutate(toRequest(values))
    })(event)
  }

  const requestClose = () => {
    if (editedRef.current && !mutation.isSuccess) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const submitting = mutation.isPending
  const lifecycleBusy = lifecycleMutation.isPending
  const isCancelled = live?.isCancelled ?? false
  const liveStatus = live?.status

  if (createdProcess != null) {
    const finish = () => onSaved(createdProcess, 'create')
    return (
      <Dialog
        scrollable
        width={620}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdProcess.name,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-proc-editor__section">
          <h2>{t('editor.attachments.title')}</h2>
          <ProcessAttachments processId={createdProcess.id} autoUpload={stagedFiles} />
        </section>
      </Dialog>
    )
  }

  return (
    <>
      <Dialog
        scrollable
        width={620}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-proc-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('editor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-proc-form" disabled={submitting}>
              {mode === 'create'
                ? submitting
                  ? t('editor.actions.creating')
                  : t('editor.actions.create')
                : submitting
                  ? t('editor.actions.saving')
                  : t('editor.actions.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-proc-form"
          className="seg-proc-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-proc-editor__error" role="alert">
              {serverError}
            </p>
          )}

          {mode === 'edit' && liveStatus != null && (
            <div className="seg-proc-editor__statusrow">
              <Badge
                tone={statusTone[liveStatus]}
                dot
                pulse={liveStatus === 'InProgress'}
              >
                {t(`list.status.${liveStatus}`)}
              </Badge>
              <span className="seg-proc-editor__statushint">
                <CircleDashed size={13} aria-hidden="true" />
                {t('editor.derivedStatusHint')}
              </span>
            </div>
          )}

          <section className="seg-proc-editor__section">
            <h2>{t('editor.sections.general')}</h2>
            <Input
              label={t('editor.fields.name')}
              placeholder={t('editor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-proc-editor__grid">
              <Field
                label={t('editor.fields.category')}
                error={formState.errors.categoryId?.message}
              >
                <Select
                  {...register('categoryId')}
                  aria-invalid={formState.errors.categoryId != null}
                  options={categories.map((category) => ({
                    value: String(category.id),
                    label: category.name,
                  }))}
                />
              </Field>
              <Input
                label={t('editor.fields.dueDate')}
                type="date"
                hint={t('editor.fields.dueDateHint')}
                error={formState.errors.dueDate?.message}
                {...register('dueDate')}
              />
            </div>
            <ToggleField
              id="process-field-visibility"
              label={t('editor.fields.visibility')}
              hint={
                canChangeVisibility
                  ? t('editor.visibilityHint.default')
                  : t('editor.visibilityHint.locked')
              }
            >
              <SegmentedControl
                aria-labelledby="process-field-visibility"
                disabled={!canChangeVisibility}
                {...register('visibility')}
                options={visibilities.map((value) => ({
                  value,
                  label: t(`list.visibility.${value}`),
                  icon: visibilityMeta[value].icon,
                  tone: visibilityMeta[value].tone,
                }))}
              />
            </ToggleField>
          </section>

          <section className="seg-proc-editor__section">
            <h2>{t('editor.sections.notes')}</h2>
            <label className="seg-proc-editor__notes">
              <span className="seg-proc-editor__notes-label">
                {t('editor.fields.notes')}
              </span>
              <textarea
                className="seg-proc-editor__textarea"
                rows={4}
                placeholder={t('editor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null ? (
                <span className="seg-proc-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              ) : (
                <span className="seg-proc-editor__hint">
                  {t('editor.fields.notesHint')}
                </span>
              )}
            </label>
          </section>

          {mode === 'edit' && live != null && processId != null && (
            <section className="seg-proc-editor__section">
              <h2>{t('editor.sections.steps')}</h2>
              <div className="seg-proc-editor__steps">
                <p className="seg-proc-editor__steps-summary">
                  {live.totalStepCount === 0
                    ? t('editor.steps.summaryEmpty')
                    : t('editor.steps.summary', {
                        resolved: live.resolvedStepCount,
                        count: live.totalStepCount,
                      })}
                </p>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  iconLeft={<ListChecks size={15} />}
                  onClick={() => onManageSteps(processId)}
                >
                  {t('editor.steps.manage')}
                </Button>
              </div>
              <p className="seg-proc-editor__hint">{t('editor.steps.hint')}</p>
            </section>
          )}

          <section className="seg-proc-editor__section">
            <h2>{t('editor.attachments.title')}</h2>
            <p className="seg-proc-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && processId != null ? (
              <ProcessAttachments processId={processId} />
            ) : (
              <StagedProcessAttachments
                files={stagedFiles}
                onChange={(files) => {
                  editedRef.current = true
                  setStagedFiles(files)
                }}
              />
            )}
          </section>

          {mode === 'edit' && processId != null && (
            <section className="seg-proc-editor__section seg-proc-editor__lifecycle">
              {isCancelled ? (
                <>
                  <p className="seg-proc-editor__hint">
                    {t('editor.lifecycle.reopenHint')}
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    iconLeft={<RotateCcw size={15} />}
                    disabled={lifecycleBusy}
                    onClick={() => lifecycleMutation.mutate('reopen')}
                  >
                    {lifecycleBusy
                      ? t('editor.lifecycle.reopening')
                      : t('editor.lifecycle.reopen')}
                  </Button>
                </>
              ) : (
                <>
                  <p className="seg-proc-editor__hint">
                    {t('editor.lifecycle.cancelHint')}
                  </p>
                  <Button
                    type="button"
                    variant="ghost"
                    className="seg-proc-editor__cancel-process"
                    iconLeft={<Ban size={15} />}
                    disabled={lifecycleBusy}
                    onClick={() => lifecycleMutation.mutate('cancel')}
                  >
                    {lifecycleBusy
                      ? t('editor.lifecycle.cancelling')
                      : t('editor.lifecycle.cancel')}
                  </Button>
                </>
              )}
            </section>
          )}

          {mode === 'create' && (
            <p className="seg-proc-editor__foot-note">
              <Sparkles size={13} aria-hidden="true" />
              {t('editor.newDefaultsHint')}
            </p>
          )}
        </form>
      </Dialog>

      {confirmingClose && (
        <Dialog
          width={420}
          title={t('editor.unsaved.title')}
          description={t('editor.unsaved.description')}
          onClose={() => setConfirmingClose(false)}
          closeLabel={t('editor.unsaved.stay')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingClose(false)}>
                {t('editor.unsaved.stay')}
              </Button>
              <Button variant="danger" onClick={onClose}>
                {t('editor.unsaved.leave')}
              </Button>
            </>
          }
        />
      )}

      {confirmingDelete && (
        <Dialog
          width={460}
          title={t('editor.delete.title')}
          description={t('editor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('editor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('editor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('editor.delete.deleting')
                  : t('editor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

interface FieldProps {
  label: string
  hint?: string
  error?: string
  children: ReactNode
}

function Field({ label, hint, error, children }: FieldProps) {
  const message = error ?? hint
  return (
    <div className="seg-proc-editor__field">
      <label className="seg-proc-editor__field-control">
        <span className="seg-proc-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-proc-editor__field-hint' +
            (error != null ? ' seg-proc-editor__field-hint--error' : '')
          }
        >
          {message}
        </span>
      )}
    </div>
  )
}

interface ToggleFieldProps {
  id: string
  label: string
  hint?: string
  children: ReactNode
}

function ToggleField({ id, label, hint, children }: ToggleFieldProps) {
  return (
    <div className="seg-proc-editor__field">
      <span className="seg-proc-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-proc-editor__field-hint">{hint}</span>}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'processes.process.validation':
        return t('editor.errors.validation')
      case 'processes.process.unknown_category':
        return t('editor.errors.unknownCategory')
      case 'processes.process.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
    }
    if (error.kind === 'not-found') return t('editor.notFound')
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}
