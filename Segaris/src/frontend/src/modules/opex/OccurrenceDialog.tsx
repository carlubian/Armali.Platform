import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FileText, Paperclip, Trash2, X } from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { opexApi, type OpexOccurrence } from '@/app/api/opex'
import { isApiError } from '@/app/api/errors'
import { Button, Dialog, Input, Spinner } from '@/components/ui'

import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
} from './attachments'
import { OccurrenceAttachments } from './OccurrenceAttachments'
import {
  buildOccurrenceDefaults,
  createOccurrenceSchema,
  fromOccurrence,
  parseOccurrenceAmount,
  toOccurrenceRequest,
  type OccurrenceFormValues,
} from './occurrenceForm'
import { opexKeys } from './queries'

export interface OccurrenceDialogProps {
  mode: 'create' | 'edit'
  contractId: number
  occurrenceId?: number
  currencyCode: string
  onClose: () => void
  onSaved: () => void
  onDeleted: () => void
}

export function OccurrenceDialog({
  mode,
  contractId,
  occurrenceId,
  currencyCode,
  onClose,
  onSaved,
  onDeleted,
}: OccurrenceDialogProps) {
  const { t } = useTranslation('opex')
  const queryClient = useQueryClient()

  const occurrenceQuery = useQuery({
    queryKey: opexKeys.occurrence(contractId, occurrenceId as number),
    queryFn: ({ signal }) =>
      opexApi.getOccurrence(contractId, occurrenceId as number, signal),
    enabled: mode === 'edit' && occurrenceId != null,
  })

  const title =
    mode === 'create' ? t('occurrenceEditor.createTitle') : t('occurrenceEditor.editTitle')

  if (mode === 'edit' && occurrenceQuery.isPending) {
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-opex-editor__status">
          <Spinner />
          <span>{t('occurrenceEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && occurrenceQuery.isError) {
    const notFound =
      isApiError(occurrenceQuery.error) && occurrenceQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-opex-editor__error" role="alert">
          {notFound ? t('occurrenceEditor.notFound') : t('occurrenceEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const occurrence = mode === 'edit' ? (occurrenceQuery.data as OpexOccurrence) : undefined
  const initialValues =
    occurrence != null ? fromOccurrence(occurrence) : buildOccurrenceDefaults()

  const handleSaved = () => {
    void queryClient.invalidateQueries({ queryKey: opexKeys.occurrences(contractId) })
    void queryClient.invalidateQueries({ queryKey: opexKeys.contract(contractId) })
    onSaved()
  }

  const handleDeleted = () => {
    void queryClient.invalidateQueries({ queryKey: opexKeys.occurrences(contractId) })
    void queryClient.invalidateQueries({ queryKey: opexKeys.contract(contractId) })
    onDeleted()
  }

  return (
    <OccurrenceEditorForm
      mode={mode}
      contractId={contractId}
      occurrenceId={occurrenceId}
      title={title}
      currencyCode={currencyCode}
      initialValues={initialValues}
      onClose={onClose}
      onSaved={handleSaved}
      onDeleted={handleDeleted}
    />
  )
}

interface OccurrenceEditorFormProps {
  mode: 'create' | 'edit'
  contractId: number
  occurrenceId?: number
  title: string
  currencyCode: string
  initialValues: OccurrenceFormValues
  onClose: () => void
  onSaved: () => void
  onDeleted: () => void
}

function OccurrenceEditorForm({
  mode,
  contractId,
  occurrenceId,
  title,
  currencyCode,
  initialValues,
  onClose,
  onSaved,
  onDeleted,
}: OccurrenceEditorFormProps) {
  const { t } = useTranslation('opex')

  const schema = useMemo(
    () =>
      createOccurrenceSchema({
        dateRequired: t('occurrenceEditor.validation.dateRequired'),
        amountInvalid: t('occurrenceEditor.validation.amountInvalid'),
        descriptionTooLong: t('occurrenceEditor.validation.descriptionTooLong'),
        notesTooLong: t('occurrenceEditor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<OccurrenceFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState } = form

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdOccurrence, setCreatedOccurrence] = useState<OpexOccurrence | null>(null)
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: ReturnType<typeof toOccurrenceRequest>) =>
      mode === 'create'
        ? opexApi.createOccurrence(contractId, request)
        : opexApi.updateOccurrence(contractId, occurrenceId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedOccurrence(saved)
        return
      }
      onSaved()
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => opexApi.deleteOccurrence(contractId, occurrenceId as number),
    onSuccess: () => {
      onDeleted()
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapServerError(error, t))
    },
  })

  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    void handleSubmit((values) => {
      setServerError(null)
      mutation.mutate(toOccurrenceRequest(values))
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

  if (createdOccurrence != null) {
    const finish = () => onSaved()
    return (
      <Dialog
        scrollable
        width={560}
        title={t('occurrenceEditor.attachments.uploadTitle')}
        description={t('occurrenceEditor.attachments.uploadDescription')}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-opex-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <OccurrenceAttachments
            contractId={contractId}
            occurrenceId={createdOccurrence.id}
            autoUpload={stagedFiles}
          />
        </section>
      </Dialog>
    )
  }

  return (
    <>
      <Dialog
        scrollable
        width={560}
        title={title}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-opex-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('occurrenceEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-opex-occurrence-form" disabled={submitting}>
              {mode === 'create'
                ? submitting
                  ? t('editor.actions.creating')
                  : t('occurrenceEditor.actions.create')
                : submitting
                  ? t('editor.actions.saving')
                  : t('editor.actions.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-opex-occurrence-form"
          className="seg-opex-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-opex-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-opex-editor__section">
            <h3>{t('occurrenceEditor.sections.general')}</h3>
            <div className="seg-opex-editor__grid">
              <Input
                type="date"
                label={t('occurrenceEditor.fields.effectiveDate')}
                required
                error={formState.errors.effectiveDate?.message}
                {...register('effectiveDate')}
              />
              <Input
                label={t('occurrenceEditor.fields.actualAmount')}
                inputMode="decimal"
                placeholder="0.00"
                required
                error={formState.errors.actualAmount?.message}
                hint={currencyCode}
                {...register('actualAmount')}
              />
            </div>
            <Input
              label={t('occurrenceEditor.fields.description')}
              placeholder={t('occurrenceEditor.fields.descriptionPlaceholder')}
              error={formState.errors.description?.message}
              {...register('description')}
            />
          </section>

          <section className="seg-opex-editor__section">
            <h3>{t('occurrenceEditor.sections.notes')}</h3>
            <label className="seg-opex-editor__notes">
              <span className="seg-opex-editor__notes-label">
                {t('occurrenceEditor.fields.notes')}
              </span>
              <textarea
                className="seg-opex-editor__textarea"
                rows={3}
                placeholder={t('occurrenceEditor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-opex-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-opex-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-opex-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && occurrenceId != null ? (
              <OccurrenceAttachments contractId={contractId} occurrenceId={occurrenceId} />
            ) : (
              <StagedAttachments
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
          title={t('occurrenceEditor.delete.title')}
          description={t('occurrenceEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('occurrenceEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('occurrenceEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('occurrenceEditor.delete.deleting')
                  : t('occurrenceEditor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

interface StagedAttachmentsProps {
  files: File[]
  onChange: (files: File[]) => void
}

function StagedAttachments({ files, onChange }: StagedAttachmentsProps) {
  const { t } = useTranslation('opex')
  const input = useRef<HTMLInputElement>(null)

  const removeAt = (index: number) =>
    onChange(files.filter((_, position) => position !== index))

  return (
    <div className="seg-opex-attach">
      <div className="seg-opex-attach__head">
        <Button
          variant="outline"
          size="sm"
          iconLeft={<Paperclip size={15} />}
          onClick={() => input.current?.click()}
        >
          {t('editor.attachments.add')}
        </Button>
        <input
          ref={input}
          type="file"
          multiple
          accept={attachmentAccept}
          className="seg-opex-attach__input"
          tabIndex={-1}
          aria-label={t('editor.attachments.add')}
          onChange={(event) => {
            const chosen = event.target.files
            if (chosen != null) onChange([...files, ...Array.from(chosen)])
            event.target.value = ''
          }}
        />
      </div>
      {files.length === 0 ? (
        <p className="seg-opex-attach__empty">{t('editor.attachments.empty')}</p>
      ) : (
        <>
          <p className="seg-opex-editor__hint">{t('editor.attachments.stagedHint')}</p>
          <ul className="seg-opex-attach__list">
            {files.map((file, index) => {
              const rejection = rejectionFor(file)
              return (
                <li
                  key={`${file.name}-${index}`}
                  className={
                    'seg-opex-attach__item' +
                    (rejection != null ? ' seg-opex-attach__item--error' : '')
                  }
                >
                  <FileText
                    size={18}
                    aria-hidden="true"
                    className="seg-opex-attach__icon"
                  />
                  <span className="seg-opex-attach__meta">
                    <span className="seg-opex-attach__name">{file.name}</span>
                    <span className="seg-opex-attach__size">
                      {rejection === 'tooLarge'
                        ? t('editor.attachments.errors.tooLarge')
                        : rejection === 'type'
                          ? t('editor.attachments.errors.type')
                          : formatFileSize(file.size)}
                    </span>
                  </span>
                  <button
                    type="button"
                    className="seg-opex-attach__action"
                    onClick={() => removeAt(index)}
                    aria-label={t('editor.attachments.removeStaged')}
                  >
                    <X size={16} aria-hidden="true" />
                  </button>
                </li>
              )
            })}
          </ul>
        </>
      )}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'opex.occurrence.validation':
        return t('occurrenceEditor.errors.validation')
    }
    if (error.kind === 'not-found') {
      return t('occurrenceEditor.errors.notFound')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('occurrenceEditor.errors.conflict')
    }
  }
  return t('occurrenceEditor.errors.generic')
}

// Re-export parseOccurrenceAmount for use in request construction
export { parseOccurrenceAmount }
