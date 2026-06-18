import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Check, Sparkles, Trash2 } from 'lucide-react'
import { useMemo, useRef, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { isApiError } from '@/app/api/errors'
import {
  moodAlignments,
  moodApi,
  moodDirections,
  moodEnergies,
  moodNotesMaxLength,
  moodSources,
  type CreateMoodEntryRequest,
  type MoodEntry,
} from '@/app/api/mood'
import { Button, Dialog, Input, Spinner } from '@/components/ui'

import {
  alignmentTone,
  directionTone,
  energyTone,
  moodToneVars,
  scoreTone,
  sourceTone,
  type MoodTone,
} from './criteria'
import { useEmotionLabel } from './MoodPrimitives'
import {
  buildDefaults,
  createMoodEntrySchema,
  fromEntry,
  type MoodEntryFormValues,
} from './entryForm'
import { moodKeys } from './queries'

import './MoodDialog.css'

export interface MoodEntryDialogProps {
  mode: 'create' | 'edit'
  entryId?: number
  today: string
  onClose: () => void
  onCreated: (entry: MoodEntry) => void
  onSaved: (entry: MoodEntry) => void
  onDeleted: (entry: MoodEntry) => void
}

export function MoodEntryDialog({
  mode,
  entryId,
  today,
  onClose,
  onCreated,
  onSaved,
  onDeleted,
}: MoodEntryDialogProps) {
  const { t } = useTranslation('mood')

  const entryQuery = useQuery({
    queryKey: moodKeys.entry(entryId ?? 0),
    queryFn: ({ signal }) => moodApi.getEntry(entryId as number, signal),
    enabled: mode === 'edit' && entryId != null,
  })

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  if (mode === 'edit' && entryQuery.isPending) {
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.close')}
      >
        <div className="mood-dialog__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && entryQuery.isError) {
    const notFound =
      isApiError(entryQuery.error) && entryQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.close')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="mood-dialog__error" role="alert">
          {notFound ? t('editor.errors.notFound') : t('editor.errors.generic')}
        </p>
      </Dialog>
    )
  }

  const entry = mode === 'edit' ? (entryQuery.data as MoodEntry) : undefined
  const initialValues = entry != null ? fromEntry(entry) : buildDefaults(today)

  return (
    <MoodEntryForm
      mode={mode}
      entryId={entryId}
      entry={entry}
      title={title}
      description={description}
      initialValues={initialValues}
      onClose={onClose}
      onCreated={onCreated}
      onSaved={onSaved}
      onDeleted={onDeleted}
    />
  )
}

interface MoodEntryFormProps {
  mode: 'create' | 'edit'
  entryId?: number
  entry?: MoodEntry
  title: string
  description: string
  initialValues: MoodEntryFormValues
  onClose: () => void
  onCreated: (entry: MoodEntry) => void
  onSaved: (entry: MoodEntry) => void
  onDeleted: (entry: MoodEntry) => void
}

function MoodEntryForm({
  mode,
  entryId,
  entry,
  title,
  description,
  initialValues,
  onClose,
  onCreated,
  onSaved,
  onDeleted,
}: MoodEntryFormProps) {
  const { t } = useTranslation('mood')
  const emotionLabel = useEmotionLabel()

  const schema = useMemo(
    () =>
      createMoodEntrySchema({
        dateRequired: t('editor.validation.dateRequired'),
        scoreRequired: t('editor.validation.scoreRequired'),
        energyRequired: t('editor.validation.energyRequired'),
        alignmentRequired: t('editor.validation.alignmentRequired'),
        directionRequired: t('editor.validation.directionRequired'),
        sourceRequired: t('editor.validation.sourceRequired'),
        notesTooLong: t('editor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<MoodEntryFormValues, unknown, CreateMoodEntryRequest>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { control, register, handleSubmit, formState } = form

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const savedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CreateMoodEntryRequest) =>
      mode === 'create'
        ? moodApi.createEntry(request)
        : moodApi.updateEntry(entryId as number, request),
    onSuccess: (saved) => {
      savedRef.current = true
      if (mode === 'create') onCreated(saved)
      else onSaved(saved)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => moodApi.deleteEntry(entryId as number),
    onSuccess: () => {
      savedRef.current = true
      if (entry != null) onDeleted(entry)
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
  const notes = useWatch({ control, name: 'notes' })

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
                className="mood-dialog__delete"
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
              form="mood-entry-form"
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
        <form id="mood-entry-form" className="mood-form" onSubmit={submit} noValidate>
          {serverError != null && (
            <p className="mood-dialog__error" role="alert">
              {serverError}
            </p>
          )}

          <div className="mood-form__grid2">
            <Input
              type="date"
              label={t('editor.fields.entryDate')}
              required
              error={formState.errors.entryDate?.message}
              {...register('entryDate')}
            />
            <Controller
              control={control}
              name="score"
              render={({ field }) => (
                <ScoreField
                  value={field.value}
                  onChange={field.onChange}
                  error={formState.errors.score?.message}
                />
              )}
            />
          </div>

          <Controller
            control={control}
            name="energy"
            render={({ field }) => (
              <ChoiceField
                label={t('criteria.energy.label')}
                options={moodEnergies}
                value={field.value}
                onChange={field.onChange}
                toneOf={(value) => energyTone[value]}
                labelOf={(value) => t(`criteria.energy.${value}`)}
                error={formState.errors.energy?.message}
              />
            )}
          />
          <Controller
            control={control}
            name="alignment"
            render={({ field }) => (
              <ChoiceField
                label={t('criteria.alignment.label')}
                options={moodAlignments}
                value={field.value}
                onChange={field.onChange}
                toneOf={(value) => alignmentTone[value]}
                labelOf={(value) => t(`criteria.alignment.${value}`)}
                error={formState.errors.alignment?.message}
              />
            )}
          />
          <div className="mood-form__grid2">
            <Controller
              control={control}
              name="direction"
              render={({ field }) => (
                <ChoiceField
                  label={t('criteria.direction.label')}
                  options={moodDirections}
                  value={field.value}
                  onChange={field.onChange}
                  toneOf={(value) => directionTone[value]}
                  labelOf={(value) => t(`criteria.direction.${value}`)}
                  error={formState.errors.direction?.message}
                />
              )}
            />
            <Controller
              control={control}
              name="source"
              render={({ field }) => (
                <ChoiceField
                  label={t('criteria.source.label')}
                  options={moodSources}
                  value={field.value}
                  onChange={field.onChange}
                  toneOf={(value) => sourceTone[value]}
                  labelOf={(value) => t(`criteria.source.${value}`)}
                  error={formState.errors.source?.message}
                />
              )}
            />
          </div>

          <div className="mood-derived">
            <span className="mood-derived__icon">
              <Sparkles size={18} aria-hidden="true" />
            </span>
            <div className="mood-derived__text">
              <span className="mood-derived__label">{t('editor.derived.label')}</span>
              <span className="mood-derived__value">
                {entry != null
                  ? emotionLabel(entry.derivedEmotion)
                  : t('editor.derived.placeholder')}
              </span>
            </div>
            <span className="mood-derived__hint">{t('editor.derived.hint')}</span>
          </div>

          <label className="mood-form__notes">
            <span className="mood-form__notes-label">
              {t('editor.fields.notes')}{' '}
              <span className="mood-form__notes-optional">
                · {t('editor.fields.notesOptional')}
              </span>
            </span>
            <textarea
              className="mood-form__textarea"
              rows={3}
              maxLength={moodNotesMaxLength}
              placeholder={t('editor.fields.notesPlaceholder')}
              aria-invalid={formState.errors.notes != null}
              {...register('notes')}
            />
            <span className="mood-form__notes-count">
              {t('editor.fields.notesCount', {
                count: notes?.length ?? 0,
                max: moodNotesMaxLength,
              })}
            </span>
            {formState.errors.notes?.message != null && (
              <span className="mood-dialog__field-error" role="alert">
                {formState.errors.notes.message}
              </span>
            )}
          </label>
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

interface ScoreFieldProps {
  value: number | null
  onChange: (value: number) => void
  error?: string
}

function ScoreField({ value, onChange, error }: ScoreFieldProps) {
  const { t } = useTranslation('mood')
  return (
    <div className="mood-field">
      <span className="mood-field__label mood-field__label--req">
        {t('editor.fields.score')}
      </span>
      <div className="mood-scoresel" role="group" aria-label={t('editor.fields.score')}>
        {[1, 2, 3, 4, 5].map((option) => {
          const active = value === option
          const [bg, fg] = moodToneVars[scoreTone(option)]
          return (
            <button
              key={option}
              type="button"
              className={['mood-scoresel__btn', active ? 'is-active' : '']
                .filter(Boolean)
                .join(' ')}
              aria-pressed={active}
              style={
                active ? { background: bg, color: fg, borderColor: fg } : undefined
              }
              onClick={() => onChange(option)}
            >
              {option}
            </button>
          )
        })}
      </div>
      {error != null && (
        <span className="mood-dialog__field-error" role="alert">
          {error}
        </span>
      )}
    </div>
  )
}

interface ChoiceFieldProps<T extends string> {
  label: string
  options: readonly T[]
  value: T | null
  onChange: (value: T) => void
  toneOf: (value: T) => MoodTone
  labelOf: (value: T) => string
  error?: string
}

function ChoiceField<T extends string>({
  label,
  options,
  value,
  onChange,
  toneOf,
  labelOf,
  error,
}: ChoiceFieldProps<T>) {
  return (
    <div className="mood-field">
      <span className="mood-field__label mood-field__label--req">{label}</span>
      <div className="mood-choice" role="group" aria-label={label}>
        {options.map((option) => {
          const active = value === option
          const [bg, fg] = moodToneVars[toneOf(option)]
          return (
            <button
              key={option}
              type="button"
              className={['mood-choice__btn', active ? 'is-active' : '']
                .filter(Boolean)
                .join(' ')}
              aria-pressed={active}
              style={
                active ? { background: bg, color: fg, borderColor: fg } : undefined
              }
              onClick={() => onChange(option)}
            >
              {labelOf(option)}
            </button>
          )
        })}
      </div>
      {error != null && (
        <span className="mood-dialog__field-error" role="alert">
          {error}
        </span>
      )}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'mood.entry.validation':
        return t('editor.errors.validation')
      case 'mood.entry.not_found':
        return t('editor.errors.notFound')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}
