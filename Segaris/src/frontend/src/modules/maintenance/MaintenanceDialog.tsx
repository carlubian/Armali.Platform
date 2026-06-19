import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Globe, Lock, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { assetsApi, type AssetSummary } from '@/app/api/assets'
import { isApiError } from '@/app/api/errors'
import {
  maintenanceApi,
  type MaintenancePriority,
  type MaintenanceStatus,
  type MaintenanceTask,
  type MaintenanceVisibility,
  type CreateMaintenanceTaskRequest,
} from '@/app/api/maintenance'
import { formatDate } from '@/app/i18n/formatters'
import {
  Button,
  Dialog,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  type SegmentTone,
} from '@/components/ui'

import { MaintenanceAttachments } from './MaintenanceAttachments'
import { StagedMaintenanceAttachments } from './StagedMaintenanceAttachments'
import {
  buildDefaults,
  createMaintenanceSchema,
  fromTask,
  toRequest,
  type MaintenanceFormValues,
} from './maintenanceForm'
import { maintenanceKeys, useMaintenanceTypes } from './queries'

import './MaintenanceDialog.css'

export interface MaintenanceDialogProps {
  mode: 'create' | 'edit'
  taskId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (task: MaintenanceTask, mode: 'create' | 'edit') => void
  onDeleted: (task: MaintenanceTask) => void
}

export function MaintenanceDialog({
  mode,
  taskId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: MaintenanceDialogProps) {
  const { t } = useTranslation('maintenance')
  const types = useMaintenanceTypes()

  const taskQuery = useQuery({
    queryKey: maintenanceKeys.task(taskId as number),
    queryFn: ({ signal }) => maintenanceApi.getTask(taskId as number, signal),
    enabled: mode === 'edit' && taskId != null,
  })

  const title =
    mode === 'create' ? t('taskEditor.createTitle') : t('taskEditor.editTitle')
  const description =
    mode === 'create'
      ? t('taskEditor.createDescription')
      : t('taskEditor.editDescription')

  if (types.data == null || (mode === 'edit' && taskQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-maint-editor__status">
          <Spinner />
          <span>{t('taskEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && taskQuery.isError) {
    const notFound =
      isApiError(taskQuery.error) && taskQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-maint-editor__error" role="alert">
          {notFound ? t('taskEditor.notFound') : t('taskEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const task = mode === 'edit' ? (taskQuery.data as MaintenanceTask) : undefined
  const initialValues =
    task != null ? fromTask(task) : buildDefaults(firstCatalogId(types.data))
  const canChangeVisibility =
    task == null || (currentUserId != null && task.createdById === currentUserId)

  return (
    <MaintenanceEditorForm
      mode={mode}
      taskId={taskId}
      task={task}
      title={title}
      description={description}
      initialValues={initialValues}
      types={types.data}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
    />
  )
}

function firstCatalogId(items: ReadonlyArray<{ id: number }> | undefined): string {
  const first = items?.[0]
  return first != null ? String(first.id) : ''
}

interface MaintenanceEditorFormProps {
  mode: 'create' | 'edit'
  taskId?: number
  task?: MaintenanceTask
  title: string
  description: string
  initialValues: MaintenanceFormValues
  types: ReadonlyArray<{ id: number; name: string }>
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (task: MaintenanceTask, mode: 'create' | 'edit') => void
  onDeleted: (task: MaintenanceTask) => void
}

const statuses: MaintenanceStatus[] = [
  'Pending',
  'InProgress',
  'Completed',
  'Cancelled',
]
const priorities: MaintenancePriority[] = ['Low', 'Medium', 'High']
const visibilities: MaintenanceVisibility[] = ['Public', 'Private']

const visibilityMeta: Record<
  MaintenanceVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

function MaintenanceEditorForm({
  mode,
  taskId,
  task,
  title,
  description,
  initialValues,
  types,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: MaintenanceEditorFormProps) {
  const { t, i18n } = useTranslation('maintenance')
  const schema = useMemo(
    () =>
      createMaintenanceSchema({
        titleRequired: t('taskEditor.validation.titleRequired'),
        titleTooLong: t('taskEditor.validation.titleTooLong'),
        typeRequired: t('taskEditor.validation.typeRequired'),
        dateInvalid: t('taskEditor.validation.dateInvalid'),
        notesTooLong: t('taskEditor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<MaintenanceFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState, control, setValue } = form
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdTask, setCreatedTask] = useState<MaintenanceTask | null>(null)
  const editedRef = useRef(false)
  const visibility = useWatch({ control, name: 'visibility' })
  const assetId = useWatch({ control, name: 'assetId' })

  const assetOptionsQuery = useQuery({
    queryKey: ['maintenance', 'asset-picker-options', visibility],
    queryFn: ({ signal }) =>
      assetsApi.listAssets(
        {
          page: 1,
          pageSize: 100,
          sort: 'name',
          sortDirection: 'asc',
          visibility: visibility === 'Public' ? 'Public' : null,
        },
        signal,
      ),
  })

  const assets = useMemo(
    () => assetOptionsQuery.data?.items ?? [],
    [assetOptionsQuery.data?.items],
  )
  useEffect(() => {
    if (visibility !== 'Public' || assetId === '') return
    if (!assets.some((asset) => String(asset.id) === assetId)) {
      setValue('assetId', '', { shouldDirty: true, shouldValidate: true })
      editedRef.current = true
    }
  }, [assetId, assets, setValue, visibility])

  const mutation = useMutation({
    mutationFn: (request: CreateMaintenanceTaskRequest) =>
      mode === 'create'
        ? maintenanceApi.createTask(request)
        : maintenanceApi.updateTask(taskId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedTask(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => maintenanceApi.deleteTask(taskId as number),
    onSuccess: () => {
      if (task != null) onDeleted(task)
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
  const assetOptions = buildAssetOptions(assets, task, t('taskEditor.fields.assetPlaceholder'))

  if (createdTask != null) {
    const finish = () => onSaved(createdTask, 'create')
    return (
      <Dialog
        scrollable
        width={760}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdTask.title,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-maint-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <MaintenanceAttachments taskId={createdTask.id} autoUpload={stagedFiles} />
        </section>
      </Dialog>
    )
  }

  return (
    <>
      <Dialog
        scrollable
        width={760}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-maint-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('taskEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-maint-form" disabled={submitting}>
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
          id="seg-maint-form"
          className="seg-maint-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-maint-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-maint-editor__section">
            <h3>{t('taskEditor.sections.general')}</h3>
            <Input
              label={t('taskEditor.fields.title')}
              placeholder={t('taskEditor.fields.titlePlaceholder')}
              required
              error={formState.errors.title?.message}
              {...register('title')}
            />
            <div className="seg-maint-editor__grid">
              <Field
                label={t('taskEditor.fields.type')}
                error={formState.errors.maintenanceTypeId?.message}
              >
                <Select
                  {...register('maintenanceTypeId')}
                  aria-invalid={formState.errors.maintenanceTypeId != null}
                  options={types.map((type) => ({
                    value: String(type.id),
                    label: type.name,
                  }))}
                />
              </Field>
              <Field label={t('taskEditor.fields.status')}>
                <Select
                  {...register('status')}
                  options={statuses.map((value) => ({
                    value,
                    label: t(`tasks.status.${value}`),
                  }))}
                />
              </Field>
              <Field label={t('taskEditor.fields.priority')}>
                <Select
                  {...register('priority')}
                  options={priorities.map((value) => ({
                    value,
                    label: t(`tasks.priority.${value}`),
                  }))}
                />
              </Field>
              <Input
                label={t('taskEditor.fields.dueDate')}
                type="date"
                error={formState.errors.dueDate?.message}
                {...register('dueDate')}
              />
              {task?.completedDate != null && (
                <Input
                  label={t('taskEditor.fields.completedDate')}
                  value={formatDate(task.completedDate, i18n.language)}
                  readOnly
                />
              )}
              <Field
                label={t('taskEditor.fields.asset')}
                hint={t('editor.visibilityHint.publicAssetOnly')}
              >
                <Select
                  {...register('assetId')}
                  aria-invalid={formState.errors.assetId != null}
                  disabled={assetOptionsQuery.isPending}
                  options={assetOptions}
                />
              </Field>
              <ToggleField
                id="maintenance-field-visibility"
                label={t('taskEditor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="maintenance-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`tasks.visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-maint-editor__section">
            <h3>{t('taskEditor.sections.notes')}</h3>
            <label className="seg-maint-editor__notes">
              <span className="seg-maint-editor__notes-label">
                {t('taskEditor.fields.notes')}
              </span>
              <textarea
                className="seg-maint-editor__textarea"
                rows={4}
                placeholder={t('taskEditor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-maint-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-maint-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-maint-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && taskId != null ? (
              <MaintenanceAttachments taskId={taskId} />
            ) : (
              <StagedMaintenanceAttachments
                files={stagedFiles}
                onChange={(files) => {
                  editedRef.current = true
                  setStagedFiles(files)
                }}
              />
            )}
          </section>
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
          title={t('taskEditor.delete.title')}
          description={t('taskEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('taskEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('taskEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('taskEditor.delete.deleting')
                  : t('taskEditor.delete.confirm')}
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
    <div className="seg-maint-editor__field">
      <label className="seg-maint-editor__field-control">
        <span className="seg-maint-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-maint-editor__field-hint' +
            (error != null ? ' seg-maint-editor__field-hint--error' : '')
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
    <div className="seg-maint-editor__field">
      <span className="seg-maint-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-maint-editor__field-hint">{hint}</span>}
    </div>
  )
}

function buildAssetOptions(
  assets: ReadonlyArray<AssetSummary>,
  task: MaintenanceTask | undefined,
  emptyLabel: string,
) {
  const options = [
    { value: '', label: emptyLabel },
    ...assets.map((asset) => ({ value: String(asset.id), label: asset.name })),
  ]
  if (
    task?.assetId != null &&
    task.assetName != null &&
    !options.some((option) => option.value === String(task.assetId))
  ) {
    options.push({ value: String(task.assetId), label: task.assetName })
  }
  return options
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'maintenance.task.validation':
        return t('taskEditor.errors.validation')
      case 'maintenance.task.unknown_type':
        return t('taskEditor.errors.unknownType')
      case 'maintenance.task.asset_invalid':
        return t('taskEditor.errors.assetInvalid')
      case 'maintenance.task.asset_visibility_forbidden':
        return t('taskEditor.errors.assetVisibility')
      case 'maintenance.task.visibility_forbidden':
        return t('taskEditor.errors.visibilityForbidden')
    }
    if (error.kind === 'not-found') return t('taskEditor.notFound')
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('taskEditor.errors.conflict')
    }
  }
  return t('taskEditor.errors.generic')
}
