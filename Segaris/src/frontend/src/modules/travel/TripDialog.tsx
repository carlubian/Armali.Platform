import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Globe, Link2, Lock, Plus, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { destinationsApi, type DestinationSummary } from '@/app/api/destinations'
import {
  travelApi,
  type CreateTravelTripRequest,
  type TravelTrip,
  type TravelTripStatus,
  type TravelVisibility,
} from '@/app/api/travel'
import { isApiError } from '@/app/api/errors'
import {
  EntityReferenceField,
  type EntityReference,
} from '@/components/entity-selection'
import {
  Button,
  Dialog,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  Tabs,
  type SegmentTone,
} from '@/components/ui'

import { TravelAttachments } from './TravelAttachments'
import {
  DestinationEntitySelector,
  destinationReference,
} from '@/modules/destinations/DestinationEntitySelector'
import { destinationsKeys } from '@/modules/destinations/contracts'
import { TripExpenses } from './TripExpenses'
import {
  blankItineraryEntry,
  buildDefaults,
  createTripSchema,
  fromTrip,
  maxItineraryEntries,
  toRequest,
  type TripFormValues,
} from './tripForm'
import { travelKeys, useTravelTripTypes } from './queries'

import './TravelDialog.css'

export interface TripDialogProps {
  mode: 'create' | 'edit'
  tripId?: number
  currentUserId: number | null
  language: string
  onClose: () => void
  onCreated: (trip: TravelTrip) => void
  onSaved: (trip: TravelTrip) => void
  onDeleted: (trip: TravelTrip) => void
}

export function TripDialog({
  mode,
  tripId,
  currentUserId,
  language,
  onClose,
  onCreated,
  onSaved,
  onDeleted,
}: TripDialogProps) {
  const { t } = useTranslation('travel')

  const tripTypes = useTravelTripTypes()

  const tripQuery = useQuery({
    queryKey: travelKeys.trip(tripId as number),
    queryFn: ({ signal }) => travelApi.getTrip(tripId as number, signal),
    enabled: mode === 'edit' && tripId != null,
  })

  const catalogsReady = tripTypes.data != null

  const title =
    mode === 'create' ? t('tripEditor.createTitle') : t('tripEditor.editTitle')
  const description =
    mode === 'create'
      ? t('tripEditor.createDescription')
      : t('tripEditor.editDescription')

  if (!catalogsReady || (mode === 'edit' && tripQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={820}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-trv-editor__status">
          <Spinner />
          <span>{t('tripEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && tripQuery.isError) {
    const notFound = isApiError(tripQuery.error) && tripQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={820}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-trv-editor__error" role="alert">
          {notFound ? t('tripEditor.notFound') : t('tripEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const trip = mode === 'edit' ? (tripQuery.data as TravelTrip) : undefined
  const initialValues =
    trip != null
      ? fromTrip(trip)
      : buildDefaults({ tripTypeId: firstCatalogId(tripTypes.data) })

  const canChangeVisibility =
    trip == null || (currentUserId != null && trip.createdById === currentUserId)

  return (
    <TripEditorForm
      mode={mode}
      tripId={tripId}
      trip={trip}
      title={title}
      description={description}
      language={language}
      initialValues={initialValues}
      tripTypes={tripTypes.data ?? []}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onCreated={onCreated}
      onSaved={onSaved}
      onDeleted={onDeleted}
    />
  )
}

function firstCatalogId(items: ReadonlyArray<{ id: number }> | undefined): string {
  const first = items?.[0]
  return first != null ? String(first.id) : ''
}

interface TripEditorFormProps {
  mode: 'create' | 'edit'
  tripId?: number
  trip?: TravelTrip
  title: string
  description: string
  language: string
  initialValues: TripFormValues
  tripTypes: ReadonlyArray<{ id: number; name: string }>
  canChangeVisibility: boolean
  onClose: () => void
  onCreated: (trip: TravelTrip) => void
  onSaved: (trip: TravelTrip) => void
  onDeleted: (trip: TravelTrip) => void
}

const statuses: TravelTripStatus[] = ['Planned', 'Ongoing', 'Completed', 'Cancelled']
const visibilities: TravelVisibility[] = ['Public', 'Private']

const visibilityMeta: Record<TravelVisibility, { icon: ReactNode; tone: SegmentTone }> =
  {
    Public: { icon: <Globe size={15} />, tone: 'accent' },
    Private: { icon: <Lock size={15} />, tone: 'neutral' },
  }

function TripEditorForm({
  mode,
  tripId,
  trip,
  title,
  description,
  language,
  initialValues,
  tripTypes,
  canChangeVisibility,
  onClose,
  onCreated,
  onSaved,
  onDeleted,
}: TripEditorFormProps) {
  const { t } = useTranslation('travel')

  const schema = useMemo(
    () =>
      createTripSchema({
        nameRequired: t('tripEditor.validation.nameRequired'),
        nameTooLong: t('tripEditor.validation.nameTooLong'),
        tripTypeRequired: t('tripEditor.validation.tripTypeRequired'),
        startDateRequired: t('tripEditor.validation.startDateRequired'),
        endDateRequired: t('tripEditor.validation.endDateRequired'),
        endBeforeStart: t('tripEditor.validation.endBeforeStart'),
        notesTooLong: t('tripEditor.validation.notesTooLong'),
        entryDateRequired: t('tripEditor.validation.entryDateRequired'),
        entryTitleRequired: t('tripEditor.validation.entryTitleRequired'),
        entryTitleTooLong: t('tripEditor.validation.entryTitleTooLong'),
        entryPlaceTooLong: t('tripEditor.validation.entryPlaceTooLong'),
        entryLocatorTooLong: t('tripEditor.validation.entryLocatorTooLong'),
        entryNoteTooLong: t('tripEditor.validation.entryNoteTooLong'),
      }),
    [t],
  )

  const form = useForm<TripFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, getValues, setValue } = form
  const itinerary = useFieldArray({ control, name: 'itinerary' })

  const [serverError, setServerError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<'details' | 'expenses'>('details')
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [selectorOpen, setSelectorOpen] = useState(false)
  const [pickedDestination, setPickedDestination] = useState<DestinationSummary | null>(
    null,
  )
  const editedRef = useRef(false)
  const visibility = useWatch({ control, name: 'visibility' })
  const destinationId = useWatch({ control, name: 'destinationId' })
  const destinationIdNum = destinationId.trim() === '' ? null : Number(destinationId)
  const initialDestinationId = trip?.destinationId ?? null

  const linkResolution = useQuery({
    queryKey: destinationsKeys.destination(initialDestinationId ?? 0),
    queryFn: ({ signal }) =>
      destinationsApi.getDestination(initialDestinationId as number, signal),
    enabled: initialDestinationId != null,
    retry: false,
  })

  const linkedDestination: DestinationSummary | undefined =
    pickedDestination?.id === destinationIdNum
      ? pickedDestination
      : initialDestinationId === destinationIdNum
        ? linkResolution.data
        : undefined

  useEffect(() => {
    if (
      visibility === 'Public' &&
      linkedDestination != null &&
      linkedDestination.visibility !== 'Public'
    ) {
      setValue('destinationId', '', { shouldDirty: true, shouldValidate: true })
      editedRef.current = true
    }
  }, [visibility, linkedDestination, setValue])

  const selectDestination = (destination: DestinationSummary) => {
    setPickedDestination(destination)
    setValue('destinationId', String(destination.id), {
      shouldDirty: true,
      shouldValidate: true,
    })
    editedRef.current = true
    setSelectorOpen(false)
  }

  const clearDestination = () => {
    setPickedDestination(null)
    setValue('destinationId', '', { shouldDirty: true, shouldValidate: true })
    editedRef.current = true
  }

  let destinationReferenceValue: EntityReference | null = null
  let resolvingDestination = false
  if (destinationIdNum != null) {
    if (linkedDestination != null) {
      destinationReferenceValue = destinationReference(linkedDestination)
    } else if (initialDestinationId === destinationIdNum && linkResolution.isLoading) {
      resolvingDestination = true
    } else {
      destinationReferenceValue = {
        primary: trip?.destinationName ?? t('common.unknownDestination'),
        secondary: trip?.destinationCountry ?? undefined,
        unavailable: true,
      }
    }
  }

  const mutation = useMutation({
    mutationFn: (request: CreateTravelTripRequest) =>
      mode === 'create'
        ? travelApi.createTrip(request)
        : travelApi.updateTrip(tripId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create') onCreated(saved)
      else onSaved(saved)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => travelApi.deleteTrip(tripId as number),
    onSuccess: () => {
      if (trip != null) onDeleted(trip)
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

  const addEntry = () => {
    if (itinerary.fields.length >= maxItineraryEntries) return
    editedRef.current = true
    itinerary.append(blankItineraryEntry(getValues('startDate')))
  }

  const catalogOptions = (rows: ReadonlyArray<{ id: number; name: string }>) =>
    rows.map((row) => ({ value: String(row.id), label: row.name }))

  const submitting = mutation.isPending
  const busy = submitting || deleteMutation.isPending

  return (
    <>
      <Dialog
        scrollable
        width={820}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          activeTab === 'expenses' ? (
            <Button variant="ghost" onClick={requestClose}>
              {t('editor.actions.cancel')}
            </Button>
          ) : (
            <>
              {mode === 'edit' && (
                <Button
                  variant="ghost"
                  className="seg-trv-editor__delete"
                  iconLeft={<Trash2 size={15} />}
                  onClick={() => setConfirmingDelete(true)}
                  disabled={busy}
                >
                  {t('tripEditor.delete.action')}
                </Button>
              )}
              <Button variant="ghost" onClick={requestClose} disabled={submitting}>
                {t('editor.actions.cancel')}
              </Button>
              <Button type="submit" form="seg-trv-trip-form" disabled={busy}>
                {mode === 'create'
                  ? submitting
                    ? t('editor.actions.creating')
                    : t('editor.actions.create')
                  : submitting
                    ? t('editor.actions.saving')
                    : t('editor.actions.save')}
              </Button>
            </>
          )
        }
      >
        <Tabs
          className="seg-trv-editor__tabs"
          variant="line"
          value={activeTab}
          onChange={(value) => setActiveTab(value as 'details' | 'expenses')}
          tabs={[
            { value: 'details', label: t('tripEditor.tabs.details') },
            ...(mode === 'edit'
              ? [{ value: 'expenses', label: t('tripEditor.tabs.expenses') }]
              : []),
          ]}
        />

        <form
          hidden={activeTab !== 'details'}
          id="seg-trv-trip-form"
          className="seg-trv-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-trv-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-trv-editor__section">
            <h3>{t('tripEditor.sections.general')}</h3>
            <Input
              label={t('tripEditor.fields.name')}
              placeholder={t('tripEditor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-trv-editor__grid">
              <Field
                label={t('tripEditor.fields.tripType')}
                error={formState.errors.tripTypeId?.message}
              >
                <Select
                  {...register('tripTypeId')}
                  aria-invalid={formState.errors.tripTypeId != null}
                  options={catalogOptions(tripTypes)}
                />
              </Field>
              <Field label={t('tripEditor.fields.status')}>
                <Select
                  {...register('status')}
                  options={statuses.map((value) => ({
                    value,
                    label: t(`trips.status.${value}`),
                  }))}
                />
              </Field>
              <Input
                type="date"
                label={t('tripEditor.fields.startDate')}
                required
                error={formState.errors.startDate?.message}
                {...register('startDate')}
              />
              <Input
                type="date"
                label={t('tripEditor.fields.endDate')}
                required
                error={formState.errors.endDate?.message}
                {...register('endDate')}
              />
              <ToggleField
                id="trv-field-visibility"
                label={t('tripEditor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="trv-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`trips.visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
            <div className="seg-trv-editor__link">
              <span className="seg-trv-editor__field-label" id="trv-field-destination">
                {t('tripEditor.fields.destination')}
              </span>
              <EntityReferenceField
                aria-labelledby="trv-field-destination"
                value={destinationReferenceValue}
                busy={resolvingDestination}
                busyLabel={t('tripEditor.destinationLink.resolving')}
                icon={<Link2 size={19} aria-hidden="true" />}
                placeholder={t('tripEditor.fields.destinationPlaceholder')}
                helperText={
                  visibility === 'Public'
                    ? t('tripEditor.destinationLink.publicOnly')
                    : t('tripEditor.destinationLink.helper')
                }
                browseLabel={t('tripEditor.destinationLink.browse')}
                changeLabel={t('tripEditor.destinationLink.change')}
                clearLabel={t('tripEditor.destinationLink.clear')}
                onBrowse={() => setSelectorOpen(true)}
                onClear={clearDestination}
              />
            </div>
          </section>

          <section className="seg-trv-editor__section">
            <div className="seg-trv-editor__section-head">
              <h3>{t('tripEditor.sections.itinerary')}</h3>
            </div>
            <p className="seg-trv-editor__hint">{t('itinerary.hint')}</p>
            {itinerary.fields.length === 0 ? (
              <p className="seg-trv-editor__hint">{t('itinerary.empty')}</p>
            ) : (
              <ol className="seg-trv-editor__entries">
                {itinerary.fields.map((field, index) => (
                  <li key={field.id} className="seg-trv-editor__entry">
                    <div className="seg-trv-editor__entry-grid">
                      <Input
                        type="date"
                        label={t('itinerary.fields.date')}
                        error={formState.errors.itinerary?.[index]?.date?.message}
                        {...register(`itinerary.${index}.date` as const)}
                      />
                      <Input
                        label={t('itinerary.fields.time')}
                        placeholder={t('itinerary.fields.timePlaceholder')}
                        {...register(`itinerary.${index}.time` as const)}
                      />
                      <Input
                        className="seg-trv-editor__entry-title"
                        label={t('itinerary.fields.title')}
                        required
                        error={formState.errors.itinerary?.[index]?.title?.message}
                        {...register(`itinerary.${index}.title` as const)}
                      />
                      <button
                        type="button"
                        className="seg-trv-editor__icon seg-trv-editor__icon--danger"
                        onClick={() => {
                          editedRef.current = true
                          itinerary.remove(index)
                        }}
                        aria-label={t('itinerary.remove')}
                      >
                        <Trash2 size={16} aria-hidden="true" />
                      </button>
                    </div>
                    <div className="seg-trv-editor__entry-grid">
                      <Input
                        label={t('itinerary.fields.place')}
                        error={formState.errors.itinerary?.[index]?.place?.message}
                        {...register(`itinerary.${index}.place` as const)}
                      />
                      <Input
                        label={t('itinerary.fields.reservationLocator')}
                        error={
                          formState.errors.itinerary?.[index]?.reservationLocator
                            ?.message
                        }
                        {...register(`itinerary.${index}.reservationLocator` as const)}
                      />
                      <Input
                        className="seg-trv-editor__entry-note"
                        label={t('itinerary.fields.note')}
                        error={formState.errors.itinerary?.[index]?.note?.message}
                        {...register(`itinerary.${index}.note` as const)}
                      />
                    </div>
                  </li>
                ))}
              </ol>
            )}
            <div className="seg-trv-editor__entries-foot">
              <Button
                variant="outline"
                size="sm"
                iconLeft={<Plus size={15} />}
                onClick={addEntry}
                disabled={itinerary.fields.length >= maxItineraryEntries}
              >
                {t('itinerary.addEntry')}
              </Button>
              {itinerary.fields.length >= maxItineraryEntries && (
                <span className="seg-trv-editor__hint">
                  {t('itinerary.maxReached')}
                </span>
              )}
            </div>
          </section>

          <section className="seg-trv-editor__section">
            <h3>{t('tripEditor.sections.notes')}</h3>
            <label className="seg-trv-editor__notes">
              <span className="seg-trv-editor__notes-label">
                {t('tripEditor.fields.notes')}
              </span>
              <textarea
                className="seg-trv-editor__textarea"
                rows={4}
                placeholder={t('tripEditor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-trv-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-trv-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-trv-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && tripId != null ? (
              <TravelAttachments owner={{ kind: 'trip', tripId }} />
            ) : (
              <p className="seg-trv-editor__hint">
                {t('tripEditor.attachmentsAfterSave')}
              </p>
            )}
          </section>
        </form>

        {activeTab === 'expenses' && mode === 'edit' && trip != null && (
          <section className="seg-trv-editor__section">
            <TripExpenses
              tripId={trip.id}
              totals={trip.expenseTotals}
              language={language}
            />
          </section>
        )}
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
          title={t('tripEditor.delete.title')}
          description={t('tripEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('tripEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('tripEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('tripEditor.delete.deleting')
                  : t('tripEditor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}
      {selectorOpen && (
        <DestinationEntitySelector
          currentDestinationId={destinationIdNum}
          forcedVisibility={visibility === 'Public' ? 'Public' : null}
          description={
            visibility === 'Public'
              ? t('tripEditor.destinationLink.selectorPublicDescription')
              : t('tripEditor.destinationLink.selectorDescription')
          }
          onSelect={selectDestination}
          onClose={() => setSelectorOpen(false)}
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
    <div className="seg-trv-editor__field">
      <label className="seg-trv-editor__field-control">
        <span className="seg-trv-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-trv-editor__field-hint' +
            (error != null ? ' seg-trv-editor__field-hint--error' : '')
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
    <div className="seg-trv-editor__field">
      <span className="seg-trv-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-trv-editor__field-hint">{hint}</span>}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'travel.trip.validation':
        return t('tripEditor.errors.validation')
      case 'travel.itinerary.validation':
        return t('tripEditor.errors.itineraryValidation')
      case 'travel.trip.visibility_forbidden':
        return t('tripEditor.errors.visibilityForbidden')
      case 'travel.catalog.unknown_reference':
        return t('tripEditor.errors.unknownReference')
      case 'travel.trip.not_found':
        return t('tripEditor.errors.notFound')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('tripEditor.errors.conflict')
    }
  }
  return t('tripEditor.errors.generic')
}
