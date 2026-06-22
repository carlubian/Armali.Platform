import { zodResolver } from '@hookform/resolvers/zod'
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  ArrowDownAZ,
  ArrowLeft,
  ArrowUpZA,
  ChevronLeft,
  ChevronRight,
  MapPin,
  Plus,
  Search,
  Star,
  Trash2,
} from 'lucide-react'
import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
  type TextareaHTMLAttributes,
} from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import {
  destinationsApi,
  destinationsRoutePath,
  placePageSizes,
  type CreatePlaceRequest,
  type Destination,
  type Place,
  type PlacePageSize,
  type PlaceRating,
  type PlaceSortField,
  type PlaceSummary,
} from '@/app/api/destinations'
import { isApiError } from '@/app/api/errors'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Dialog, Input, Select, Spinner, Toast } from '@/components/ui'

import {
  buildPlaceDefaults,
  createPlaceFormSchema,
  fromPlace,
  toPlaceRequest,
  type PlaceFormValues,
} from './placeForm'
import { type PlacesFilterPatch, type PlacesState, usePlacesState } from './placesState'
import { destinationsKeys, usePlaceCategories } from './queries'

import './DestinationsPage.css'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

const sortFields: PlaceSortField[] = ['name', 'category', 'rating']
const ratings: PlaceRating[] = [1, 2, 3, 4, 5]

export function DestinationPlacesPage() {
  const { t } = useTranslation('destinations')
  const queryClient = useQueryClient()
  const params = useParams<{ destinationId: string }>()
  const destinationId = Number(params.destinationId)
  const validDestinationId = Number.isInteger(destinationId) && destinationId > 0
  const [toast, setToast] = useState<ToastState | null>(null)

  const {
    state,
    dialog,
    listQuery,
    setFilters,
    setSort,
    setPage,
    setPageSize,
    clearFilters,
    openCreateDialog,
    openEditDialog,
    closeDialog,
  } = usePlacesState()

  const destinationQuery = useQuery({
    queryKey: destinationsKeys.destination(destinationId),
    queryFn: ({ signal }) => destinationsApi.getDestination(destinationId, signal),
    enabled: validDestinationId,
  })

  const categories = usePlaceCategories()
  const placesQuery = useQuery({
    queryKey: destinationsKeys.placeList(destinationId, listQuery),
    queryFn: ({ signal }) =>
      destinationsApi.listPlaces(destinationId, listQuery, signal),
    enabled: validDestinationId && destinationQuery.data != null,
    placeholderData: keepPreviousData,
  })

  const places = placesQuery.data?.items ?? []
  const totalCount = placesQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activePlaceFilterCount(state) > 0

  const invalidatePlaces = (placeId?: number) => {
    void queryClient.invalidateQueries({
      queryKey: destinationsKeys.places(destinationId),
    })
    void queryClient.invalidateQueries({
      queryKey: destinationsKeys.destination(destinationId),
    })
    void queryClient.invalidateQueries({
      queryKey: destinationsKeys.destinations(),
    })
    if (placeId != null) {
      void queryClient.invalidateQueries({
        queryKey: destinationsKeys.place(destinationId, placeId),
      })
    }
  }

  const handleSaved = (place: Place, mode: 'create' | 'edit') => {
    queryClient.setQueryData(destinationsKeys.place(destinationId, place.id), place)
    invalidatePlaces(place.id)
    setToast({
      kind: mode === 'create' ? 'created' : 'updated',
      name: place.name,
    })
    closeDialog()
  }

  const handleDeleted = (place: Place) => {
    invalidatePlaces()
    setToast({ kind: 'deleted', name: place.name })
    closeDialog()
  }

  useEffect(() => {
    if (placesQuery.data != null && state.page > totalPages) setPage(totalPages)
  }, [placesQuery.data, state.page, totalPages, setPage])

  if (!validDestinationId) {
    return (
      <main className="seg-destinations armali-aurora">
        <section className="seg-destinations__empty" role="alert">
          {t('places.notFound')}
        </section>
      </main>
    )
  }

  if (destinationQuery.isError) {
    const error = destinationQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void destinationQuery.refetch()} />
    }
  }

  const destination = destinationQuery.data

  return (
    <main className="seg-destinations armali-aurora">
      <section className="seg-destinations__head">
        <div>
          <div className="armali-eyebrow">{t('places.eyebrow')}</div>
          <h1>
            {destination == null
              ? t('places.titleFallback')
              : t('places.title', { name: destination.name })}
          </h1>
          <p>{t('places.description')}</p>
        </div>
        <Link className="seg-destinations__view-link" to={destinationsRoutePath}>
          <ArrowLeft size={16} aria-hidden="true" />
          {t('places.backToGallery')}
        </Link>
      </section>

      {destinationQuery.isPending ? (
        <div className="seg-destinations__loading">
          <Spinner label={t('places.loading')} />
        </div>
      ) : destinationQuery.isError ? (
        <p className="seg-destinations__error" role="alert">
          {isApiError(destinationQuery.error) &&
          destinationQuery.error.kind === 'not-found'
            ? t('places.notFound')
            : t('places.loadError')}
        </p>
      ) : destination != null ? (
        <>
          <DestinationContext destination={destination} totalCount={totalCount} />
          <section className="seg-destinations__panel-head">
            <Badge tone="neutral">{t('places.count', { count: totalCount })}</Badge>
            <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
              {t('places.actions.newPlace')}
            </Button>
          </section>

          <PlacesFilters
            state={state}
            categories={categories.data ?? []}
            onChange={setFilters}
            onClear={clearFilters}
          />

          {placesQuery.isPending ? (
            <div className="seg-destinations__loading">
              <Spinner label={t('places.states.loading')} />
            </div>
          ) : placesQuery.isError ? (
            <p className="seg-destinations__error" role="alert">
              {t('places.states.loadError')}
            </p>
          ) : places.length === 0 ? (
            <p className="seg-destinations__empty">
              {hasFilters ? t('places.states.emptyFiltered') : t('places.states.empty')}
            </p>
          ) : (
            <PlacesList
              places={places}
              state={state}
              busy={placesQuery.isFetching && !placesQuery.isPending}
              onSort={setSort}
              onOpen={openEditDialog}
            />
          )}

          <Pager
            page={state.page}
            pageSize={state.pageSize}
            totalPages={totalPages}
            fetching={placesQuery.isFetching}
            onPage={setPage}
            onPageSize={setPageSize}
          />

          {dialog.mode !== 'closed' && (
            <PlaceDialog
              mode={dialog.mode}
              destinationId={destinationId}
              placeId={dialog.mode === 'edit' ? dialog.placeId : undefined}
              onClose={closeDialog}
              onSaved={handleSaved}
              onDeleted={handleDeleted}
            />
          )}
        </>
      ) : null}

      {toast != null && (
        <div className="seg-destinations__toast">
          <Toast
            tone="success"
            title={t(`places.toast.${toast.kind}`)}
            closeLabel={t('editor.actions.cancel')}
            onClose={() => setToast(null)}
          >
            {t(`places.toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

function DestinationContext({
  destination,
  totalCount,
}: {
  destination: Destination
  totalCount: number
}) {
  const { t } = useTranslation('destinations')
  return (
    <section className="seg-places-context" aria-label={t('places.context.label')}>
      <Badge tone="aqua">
        <MapPin size={14} aria-hidden="true" />
        {destination.country ?? t('common.none')}
      </Badge>
      <Badge tone={destination.visibility === 'Private' ? 'neutral' : 'azure'}>
        {t(`visibility.${destination.visibility}`)}
      </Badge>
      {destination.averagePlaceRating == null ? (
        <span className="seg-places-context__muted">
          {t('places.context.noRating')}
        </span>
      ) : (
        <Badge tone="gold">
          <Star size={14} aria-hidden="true" />
          {t('places.context.average', {
            rating: destination.averagePlaceRating.toFixed(1),
            count: destination.ratedPlaceCount,
          })}
        </Badge>
      )}
      <span className="seg-places-context__muted">
        {t('places.context.placeTotal', { count: totalCount })}
      </span>
    </section>
  )
}

interface PlacesFiltersProps {
  state: PlacesState
  categories: ReadonlyArray<{ id: number; name: string }>
  onChange: (patch: PlacesFilterPatch) => void
  onClear: () => void
}

function PlacesFilters({ state, categories, onChange, onClear }: PlacesFiltersProps) {
  const { t } = useTranslation('destinations')
  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)

  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }

  const anyOption = { value: '', label: t('gallery.filters.anyOption') }
  const categoryName =
    categories.find((category) => category.id === state.category)?.name ?? ''
  const chips = buildFilterChips(state, categoryName, t)

  return (
    <div className="seg-destinations__filters">
      <Input
        className="seg-destinations__search"
        iconLeft={<Search size={16} />}
        label={t('places.filters.searchLabel')}
        placeholder={t('places.filters.searchPlaceholder')}
        value={searchText}
        onChange={(event) => {
          setSearchText(event.target.value)
          onChange({ search: event.target.value })
        }}
      />
      <label className="seg-destinations__field">
        <span>{t('places.filters.category')}</span>
        <Select
          value={state.category == null ? '' : String(state.category)}
          onChange={(event) =>
            onChange({
              category: event.target.value === '' ? null : Number(event.target.value),
            })
          }
          options={[
            anyOption,
            ...categories.map((category) => ({
              value: String(category.id),
              label: category.name,
            })),
          ]}
        />
      </label>
      <label className="seg-destinations__field">
        <span>{t('places.filters.rating')}</span>
        <Select
          value={state.rating == null ? '' : String(state.rating)}
          onChange={(event) =>
            onChange({
              rating:
                event.target.value === ''
                  ? null
                  : (Number(event.target.value) as PlaceRating),
            })
          }
          options={[
            anyOption,
            ...ratings.map((rating) => ({
              value: String(rating),
              label: t('places.filters.ratingOption', { rating }),
            })),
          ]}
        />
      </label>

      {chips.length > 0 && (
        <div
          className="seg-destinations__chips"
          role="group"
          aria-label={t('places.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-destinations__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('places.filters.remove', { label: chip.label })}
            >
              {chip.label}
            </button>
          ))}
          <button
            type="button"
            className="seg-destinations__chip-clear"
            onClick={onClear}
          >
            {t('gallery.filters.clearAll')}
          </button>
        </div>
      )}
    </div>
  )
}

interface FilterChip {
  key: string
  label: string
  clear: PlacesFilterPatch
}

function buildFilterChips(
  state: PlacesState,
  categoryName: string,
  t: (key: string, values?: Record<string, unknown>) => string,
): FilterChip[] {
  const chips: FilterChip[] = []
  if (state.search.trim() !== '') {
    chips.push({
      key: 'search',
      label: t('places.filters.chip.search', { value: state.search.trim() }),
      clear: { search: '' },
    })
  }
  if (state.category != null) {
    chips.push({
      key: 'category',
      label: t('places.filters.chip.category', {
        value: categoryName || String(state.category),
      }),
      clear: { category: null },
    })
  }
  if (state.rating != null) {
    chips.push({
      key: 'rating',
      label: t('places.filters.chip.rating', { rating: state.rating }),
      clear: { rating: null },
    })
  }
  return chips
}

interface PlacesListProps {
  places: PlaceSummary[]
  state: PlacesState
  busy: boolean
  onSort: (field: PlaceSortField) => void
  onOpen: (placeId: number) => void
}

function PlacesList({ places, state, busy, onSort, onOpen }: PlacesListProps) {
  const { t } = useTranslation('destinations')
  return (
    <section className="seg-places-list-wrap" aria-busy={busy}>
      <div className="seg-destinations__sortbar">
        <span className="seg-destinations__sort-label">{t('gallery.sortLabel')}</span>
        {sortFields.map((field) => {
          const active = state.sort === field
          return (
            <button
              key={field}
              type="button"
              className={
                'seg-destinations__sort' +
                (active ? ' seg-destinations__sort--active' : '')
              }
              onClick={() => onSort(field)}
            >
              {active && state.sortDirection === 'desc' ? (
                <ArrowUpZA size={15} aria-hidden="true" />
              ) : (
                <ArrowDownAZ size={15} aria-hidden="true" />
              )}
              {t(`places.sort.${field}`)}
            </button>
          )
        })}
      </div>
      <div className="seg-places-list" aria-label={t('places.list.label')}>
        {places.map((place) => (
          <article key={place.id} className="seg-place-row">
            <button
              type="button"
              className="seg-place-row__main"
              onClick={() => onOpen(place.id)}
              aria-label={t('places.list.open', { name: place.name })}
            >
              <span className="seg-place-row__title">{place.name}</span>
              <span className="seg-place-row__meta">
                {place.address ?? t('places.list.noAddress')}
              </span>
            </button>
            <div className="seg-place-row__badges">
              <Badge tone="aqua">{place.categoryName}</Badge>
              {place.rating == null ? (
                <Badge tone="neutral">{t('places.list.unrated')}</Badge>
              ) : (
                <Badge tone="gold">
                  <Star size={13} aria-hidden="true" />
                  {t('places.list.rating', { rating: place.rating })}
                </Badge>
              )}
            </div>
            {place.review != null && (
              <p className="seg-place-row__review">{place.review}</p>
            )}
          </article>
        ))}
      </div>
    </section>
  )
}

interface PagerProps {
  page: number
  pageSize: PlacePageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: PlacePageSize) => void
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
}: PagerProps) {
  const { t } = useTranslation('destinations')
  return (
    <nav
      className="seg-destinations__pager"
      aria-label={t('pagination.status', { page, pages: totalPages })}
    >
      <label className="seg-destinations__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) => onPageSize(Number(event.target.value) as PlacePageSize)}
          options={placePageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-destinations__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-destinations__page" aria-live="polite">
          {t('pagination.status', { page, pages: totalPages })}
        </span>
        <Button
          variant="ghost"
          size="sm"
          iconRight={<ChevronRight size={16} />}
          disabled={page >= totalPages || fetching}
          onClick={() => onPage(Math.min(totalPages, page + 1))}
        >
          {t('pagination.next')}
        </Button>
      </div>
    </nav>
  )
}

interface PlaceDialogProps {
  mode: 'create' | 'edit'
  destinationId: number
  placeId?: number
  onClose: () => void
  onSaved: (place: Place, mode: 'create' | 'edit') => void
  onDeleted: (place: Place) => void
}

function PlaceDialog({
  mode,
  destinationId,
  placeId,
  onClose,
  onSaved,
  onDeleted,
}: PlaceDialogProps) {
  const { t } = useTranslation('destinations')
  const categories = usePlaceCategories()
  const placeQuery = useQuery({
    queryKey: destinationsKeys.place(destinationId, placeId as number),
    queryFn: ({ signal }) =>
      destinationsApi.getPlace(destinationId, placeId as number, signal),
    enabled: mode === 'edit' && placeId != null,
  })

  const title =
    mode === 'create' ? t('places.editor.createTitle') : t('places.editor.editTitle')
  const description =
    mode === 'create'
      ? t('places.editor.createDescription')
      : t('places.editor.editDescription')

  if (categories.data == null || (mode === 'edit' && placeQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={660}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-destinations-editor__status">
          <Spinner />
          <span>{t('places.editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && placeQuery.isError) {
    const notFound =
      isApiError(placeQuery.error) && placeQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={520}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-destinations-editor__error" role="alert">
          {notFound ? t('places.editor.notFound') : t('places.editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const place = mode === 'edit' ? (placeQuery.data as Place) : undefined
  const firstCategoryId = categories.data[0]?.id
  const initialValues =
    place != null
      ? fromPlace(place)
      : buildPlaceDefaults(firstCategoryId == null ? '' : String(firstCategoryId))

  return (
    <PlaceEditorForm
      mode={mode}
      destinationId={destinationId}
      placeId={placeId}
      place={place}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
    />
  )
}

interface PlaceEditorFormProps {
  mode: 'create' | 'edit'
  destinationId: number
  placeId?: number
  place?: Place
  title: string
  description: string
  initialValues: PlaceFormValues
  categories: ReadonlyArray<{ id: number; name: string }>
  onClose: () => void
  onSaved: (place: Place, mode: 'create' | 'edit') => void
  onDeleted: (place: Place) => void
}

function PlaceEditorForm({
  mode,
  destinationId,
  placeId,
  place,
  title,
  description,
  initialValues,
  categories,
  onClose,
  onSaved,
  onDeleted,
}: PlaceEditorFormProps) {
  const { t } = useTranslation('destinations')
  const schema = useMemo(
    () =>
      createPlaceFormSchema({
        nameRequired: t('places.editor.validation.nameRequired'),
        nameTooLong: t('places.editor.validation.nameTooLong'),
        categoryRequired: t('places.editor.validation.categoryRequired'),
        reviewTooLong: t('places.editor.validation.reviewTooLong'),
        addressTooLong: t('places.editor.validation.addressTooLong'),
      }),
    [t],
  )

  const form = useForm<PlaceFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState } = form
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CreatePlaceRequest) =>
      mode === 'create'
        ? destinationsApi.createPlace(destinationId, request)
        : destinationsApi.updatePlace(destinationId, placeId as number, request),
    onSuccess: (saved) => onSaved(saved, mode),
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => destinationsApi.deletePlace(destinationId, placeId as number),
    onSuccess: () => {
      if (place != null) onDeleted(place)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapDeleteError(error, t))
    },
  })

  const submit = (event: FormEvent<HTMLFormElement>) => {
    void handleSubmit((values) => {
      setServerError(null)
      mutation.mutate(toPlaceRequest(values))
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
  const catalogOptions = categories.map((category) => ({
    value: String(category.id),
    label: category.name,
  }))

  return (
    <>
      <Dialog
        scrollable
        width={660}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-destinations-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('places.editor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-place-form" disabled={submitting}>
              {mode === 'create'
                ? submitting
                  ? t('places.editor.actions.creating')
                  : t('places.editor.actions.create')
                : submitting
                  ? t('places.editor.actions.saving')
                  : t('places.editor.actions.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-place-form"
          className="seg-destinations-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-destinations-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-destinations-editor__section">
            <h3>{t('places.editor.sections.general')}</h3>
            <Input
              label={t('places.editor.fields.name')}
              placeholder={t('places.editor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-destinations-editor__grid">
              <Field
                label={t('places.editor.fields.category')}
                error={formState.errors.categoryId?.message}
              >
                <Select
                  {...register('categoryId')}
                  aria-invalid={formState.errors.categoryId != null}
                  options={catalogOptions}
                />
              </Field>
              <Field label={t('places.editor.fields.rating')}>
                <Select
                  {...register('rating')}
                  options={[
                    { value: '', label: t('places.editor.fields.noRating') },
                    ...ratings.map((rating) => ({
                      value: String(rating),
                      label: t('places.filters.ratingOption', { rating }),
                    })),
                  ]}
                />
              </Field>
              <Input
                label={t('places.editor.fields.address')}
                placeholder={t('places.editor.fields.addressPlaceholder')}
                error={formState.errors.address?.message}
                {...register('address')}
              />
            </div>
          </section>

          <section className="seg-destinations-editor__section">
            <h3>{t('places.editor.sections.notes')}</h3>
            <TextAreaField
              label={t('places.editor.fields.review')}
              placeholder={t('places.editor.fields.reviewPlaceholder')}
              error={formState.errors.review?.message}
              rows={5}
              {...register('review')}
            />
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
          title={t('places.editor.delete.title')}
          description={t('places.editor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('places.editor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('places.editor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('places.editor.delete.deleting')
                  : t('places.editor.delete.confirm')}
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
    <div className="seg-destinations-editor__field">
      <label className="seg-destinations-editor__field-control">
        <span className="seg-destinations-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-destinations-editor__field-hint' +
            (error != null ? ' seg-destinations-editor__field-hint--error' : '')
          }
        >
          {message}
        </span>
      )}
    </div>
  )
}

interface TextAreaFieldProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label: string
  error?: string
}

function TextAreaField({ label, error, ...props }: TextAreaFieldProps) {
  return (
    <label className="seg-destinations-editor__notes">
      <span className="seg-destinations-editor__notes-label">{label}</span>
      <textarea
        className="seg-destinations-editor__textarea"
        aria-invalid={error != null}
        {...props}
      />
      {error != null && (
        <span className="seg-destinations-editor__field-error" role="alert">
          {error}
        </span>
      )}
    </label>
  )
}

function activePlaceFilterCount(state: PlacesState): number {
  return [
    state.search.trim() !== '',
    state.category != null,
    state.rating != null,
  ].filter(Boolean).length
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'destinations.place.validation':
        return t('places.editor.errors.validation')
      case 'destinations.catalog.unknown_reference':
        return t('places.editor.errors.unknownReference')
    }
    if (error.kind === 'not-found') return t('places.editor.notFound')
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('places.editor.errors.conflict')
    }
  }
  return t('places.editor.errors.generic')
}

function mapDeleteError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error) && error.kind === 'not-found')
    return t('places.editor.notFound')
  return t('places.editor.delete.error')
}
