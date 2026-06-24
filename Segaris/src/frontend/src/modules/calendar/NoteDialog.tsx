import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { CalendarDays, Check, Lock, Trash2, Users } from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  calendarApi,
  type CalendarDailyNote,
  type CalendarDailyNoteRequest,
  type CalendarVisibility,
} from '@/app/api/calendar'
import { isApiError } from '@/app/api/errors'
import { Button, Dialog, Input, SegmentedControl, Spinner } from '@/components/ui'
import type { SegmentTone } from '@/components/ui'

import {
  buildNoteDefaults,
  calendarNoteBodyMaxLength,
  calendarNoteTitleMaxLength,
  createCalendarNoteSchema,
  fromNote,
  type CalendarNoteFormValues,
} from './noteForm'
import { useCalendarNote } from './queries'

import './NoteDialog.css'

export type CalendarNoteMutationKind = 'created' | 'updated' | 'deleted'

export interface CalendarNoteDialogProps {
  mode: 'create' | 'edit'
  noteId?: number
  /** Civil date a new note is pinned to (the selected calendar day). */
  defaultDate: string
  /** Viewer id, used to decide whether visibility may be changed. */
  currentUserId: number | null
  onClose: () => void
  onMutated: (kind: CalendarNoteMutationKind, note: CalendarDailyNote) => void
}

const visibilities: CalendarVisibility[] = ['Private', 'Public']
const visibilityMeta: Record<
  CalendarVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
  Public: { icon: <Users size={15} />, tone: 'accent' },
}

export function CalendarNoteDialog({
  mode,
  noteId,
  defaultDate,
  currentUserId,
  onClose,
  onMutated,
}: CalendarNoteDialogProps) {
  const { t } = useTranslation('calendar')

  const noteQuery = useCalendarNote(noteId ?? 0, mode === 'edit' && noteId != null)

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  if (mode === 'edit' && noteQuery.isPending) {
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.close')}
      >
        <div className="seg-cal-note__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && noteQuery.isError) {
    const notFound = isApiError(noteQuery.error) && noteQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.close')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-cal-note__error" role="alert">
          {notFound ? t('editor.errors.notFound') : t('editor.errors.loadError')}
        </p>
      </Dialog>
    )
  }

  const note = mode === 'edit' ? (noteQuery.data as CalendarDailyNote) : undefined
  const initialValues = note != null ? fromNote(note) : buildNoteDefaults(defaultDate)
  const canChangeVisibility =
    note == null || (currentUserId != null && note.createdById === currentUserId)

  return (
    <CalendarNoteForm
      mode={mode}
      noteId={noteId}
      note={note}
      title={title}
      description={description}
      initialValues={initialValues}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onMutated={onMutated}
    />
  )
}

interface CalendarNoteFormProps {
  mode: 'create' | 'edit'
  noteId?: number
  note?: CalendarDailyNote
  title: string
  description: string
  initialValues: CalendarNoteFormValues
  canChangeVisibility: boolean
  onClose: () => void
  onMutated: (kind: CalendarNoteMutationKind, note: CalendarDailyNote) => void
}

function CalendarNoteForm({
  mode,
  noteId,
  note,
  title,
  description,
  initialValues,
  canChangeVisibility,
  onClose,
  onMutated,
}: CalendarNoteFormProps) {
  const { t } = useTranslation('calendar')

  const schema = useMemo(
    () =>
      createCalendarNoteSchema({
        dateRequired: t('editor.validation.dateRequired'),
        titleTooLong: t('editor.validation.titleTooLong'),
        bodyRequired: t('editor.validation.bodyRequired'),
        bodyTooLong: t('editor.validation.bodyTooLong'),
      }),
    [t],
  )

  const form = useForm<CalendarNoteFormValues, unknown, CalendarDailyNoteRequest>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { control, register, handleSubmit, formState } = form

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const savedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CalendarDailyNoteRequest) =>
      mode === 'create'
        ? calendarApi.createNote(request)
        : calendarApi.updateNote(noteId as number, request),
    onSuccess: (saved) => {
      savedRef.current = true
      onMutated(mode === 'create' ? 'created' : 'updated', saved)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => calendarApi.deleteNote(noteId as number),
    onSuccess: () => {
      savedRef.current = true
      if (note != null) onMutated('deleted', note)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapServerError(error, t))
    },
  })

  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    void handleSubmit((request) => {
      setServerError(null)
      mutation.mutate(request)
    })(event)
  }

  // Read `isDirty` during render so RHF's formState proxy subscribes to it; a
  // bare access inside the close handler would otherwise see a stale value.
  const isDirty = formState.isDirty

  const requestClose = () => {
    if (isDirty && !savedRef.current) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const submitting = mutation.isPending
  const busy = submitting || deleteMutation.isPending
  const visibility = useWatch({ control, name: 'visibility' })
  const body = useWatch({ control, name: 'body' })

  return (
    <>
      <Dialog
        scrollable
        width={560}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.close')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-cal-note__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={busy}
              >
                {t('editor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button
              type="submit"
              form="calendar-note-form"
              iconLeft={<Check size={16} />}
              disabled={busy}
            >
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
          id="calendar-note-form"
          className="seg-cal-note__form"
          onSubmit={submit}
          noValidate
        >
          {serverError != null && (
            <p className="seg-cal-note__error" role="alert">
              {serverError}
            </p>
          )}

          <div className="seg-cal-note__grid2">
            <Input
              type="date"
              label={t('editor.fields.date')}
              required
              iconLeft={<CalendarDays size={16} />}
              error={formState.errors.date?.message}
              {...register('date')}
            />
            <div className="seg-cal-note__field">
              <span className="seg-cal-note__label" id="calendar-note-visibility">
                {t('editor.fields.visibility')}
              </span>
              <SegmentedControl
                aria-labelledby="calendar-note-visibility"
                disabled={!canChangeVisibility}
                {...register('visibility')}
                options={visibilities.map((value) => ({
                  value,
                  label: t(`editor.visibility.${value}`),
                  icon: visibilityMeta[value].icon,
                  tone: visibilityMeta[value].tone,
                }))}
              />
              <span className="seg-cal-note__hint">
                {!canChangeVisibility
                  ? t('editor.visibilityHint.locked')
                  : visibility === 'Public'
                    ? t('editor.visibilityHint.public')
                    : t('editor.visibilityHint.private')}
              </span>
            </div>
          </div>

          <Input
            label={
              <>
                {t('editor.fields.title')}{' '}
                <span className="seg-cal-note__optional">
                  · {t('editor.fields.titleOptional')}
                </span>
              </>
            }
            placeholder={t('editor.fields.titlePlaceholder')}
            maxLength={calendarNoteTitleMaxLength}
            error={formState.errors.title?.message}
            {...register('title')}
          />

          <div className="seg-cal-note__field">
            <label
              className="seg-cal-note__label seg-cal-note__label--req"
              htmlFor="calendar-note-body"
            >
              {t('editor.fields.body')}
            </label>
            <textarea
              id="calendar-note-body"
              className="seg-cal-note__textarea"
              rows={5}
              maxLength={calendarNoteBodyMaxLength}
              placeholder={t('editor.fields.bodyPlaceholder')}
              aria-invalid={formState.errors.body != null}
              aria-required
              {...register('body')}
            />
            <span className="seg-cal-note__count">
              {t('editor.fields.bodyCount', {
                count: body?.length ?? 0,
                max: calendarNoteBodyMaxLength,
              })}
            </span>
            {formState.errors.body?.message != null && (
              <span className="seg-cal-note__field-error" role="alert">
                {formState.errors.body.message}
              </span>
            )}
          </div>
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
          width={440}
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

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'calendar.note.validation':
        return t('editor.errors.validation')
      case 'calendar.note.not_found':
        return t('editor.errors.notFound')
      case 'calendar.note.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}
