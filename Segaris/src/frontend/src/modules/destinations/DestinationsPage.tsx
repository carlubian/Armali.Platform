import { zodResolver } from '@hookform/resolvers/zod'
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  ArrowDownAZ,
  ArrowUpZA,
  ChevronLeft,
  ChevronRight,
  Globe,
  ImageOff,
  Lock,
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
  type ReactNode,
  type TextareaHTMLAttributes,
} from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import {
  destinationPageSizes,
  destinationPlacesRoutePath,
  destinationsApi,
  type CreateDestinationRequest,
  type Destination,
  type DestinationDeletionImpact,
  type DestinationPageSize,
  type DestinationSortField,
  type DestinationSummary,
  type DestinationVisibility,
} from '@/app/api/destinations'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import {
  Badge,
  Button,
  Dialog,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  Toast,
  type SegmentTone,
} from '@/components/ui'

import { DestinationAttachments } from './DestinationAttachments'
import { StagedDestinationAttachments } from './StagedDestinationAttachments'
import {
  activeDestinationFilterCount,
  type DestinationsFilterPatch,
  type DestinationsState,
  useDestinationsState,
} from './destinationsState'
import {
  buildDefaults,
  createDestinationSchema,
  fromDestination,
  toRequest,
  type DestinationFormValues,
} from './destinationForm'
import { destinationsKeys, useDestinationCategories } from './queries'
import { travelKeys } from '@/modules/travel/queries'

import './DestinationsPage.css'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

const sortFields: DestinationSortField[] = ['name', 'category']
const visibilities: DestinationVisibility[] = ['Public', 'Private']

const visibilityMeta: Record<
  DestinationVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

export function DestinationsPage() {
  const { t } = useTranslation('destinations')
  const { session } = useSession()
  const currentUserId = session?.userId ?? null
  const queryClient = useQueryClient()
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
  } = useDestinationsState()

  const categories = useDestinationCategories()
  const destinationsQuery = useQuery({
    queryKey: destinationsKeys.destinationList(listQuery),
    queryFn: ({ signal }) => destinationsApi.listDestinations(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = destinationsQuery.data
  const destinations = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeDestinationFilterCount(state) > 0

  const invalidateDestinations = (destinationId?: number) => {
    void queryClient.invalidateQueries({ queryKey: destinationsKeys.destinations() })
    if (destinationId != null) {
      void queryClient.invalidateQueries({
        queryKey: destinationsKeys.destination(destinationId),
      })
      void queryClient.invalidateQueries({
        queryKey: destinationsKeys.destinationAttachments(destinationId),
      })
    }
  }

  const handleSaved = (destination: Destination, mode: 'create' | 'edit') => {
    queryClient.setQueryData(destinationsKeys.destination(destination.id), destination)
    invalidateDestinations(destination.id)
    setToast({
      kind: mode === 'create' ? 'created' : 'updated',
      name: destination.name,
    })
    closeDialog()
  }

  const handleDeleted = (destination: Destination) => {
    invalidateDestinations()
    void queryClient.invalidateQueries({ queryKey: travelKeys.trips() })
    setToast({ kind: 'deleted', name: destination.name })
    closeDialog()
  }

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (destinationsQuery.isError) {
    const error = destinationsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void destinationsQuery.refetch()} />
    }
  }

  return (
    <main className="seg-destinations armali-aurora">
      <section className="seg-destinations__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <section className="seg-destinations__panel-head">
        <Badge tone="neutral">{t('gallery.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
          {t('gallery.newDestination')}
        </Button>
      </section>

      <DestinationsFilters
        state={state}
        categories={categories.data ?? []}
        onChange={setFilters}
        onClear={clearFilters}
      />

      {destinationsQuery.isPending ? (
        <div className="seg-destinations__loading">
          <Spinner label={t('gallery.states.loading')} />
        </div>
      ) : destinationsQuery.isError ? (
        <p className="seg-destinations__error" role="alert">
          {t('gallery.states.loadError')}
        </p>
      ) : destinations.length === 0 ? (
        <p className="seg-destinations__empty">
          {hasFilters ? t('gallery.states.emptyFiltered') : t('gallery.states.empty')}
        </p>
      ) : (
        <DestinationGallery
          destinations={destinations}
          state={state}
          busy={destinationsQuery.isFetching && !destinationsQuery.isPending}
          onSort={setSort}
          onOpen={openEditDialog}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={destinationsQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
      />

      {dialog.mode !== 'closed' && (
        <DestinationDialog
          mode={dialog.mode}
          destinationId={dialog.mode === 'edit' ? dialog.destinationId : undefined}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {toast != null && (
        <div className="seg-destinations__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            closeLabel={t('editor.actions.cancel')}
            onClose={() => setToast(null)}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

interface DestinationsFiltersProps {
  state: DestinationsState
  categories: ReadonlyArray<{ id: number; name: string }>
  onChange: (patch: DestinationsFilterPatch) => void
  onClear: () => void
}

function DestinationsFilters({
  state,
  categories,
  onChange,
  onClear,
}: DestinationsFiltersProps) {
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
  const chips = buildFilterChips(state, {
    categoryName,
    labels: {
      search: (value) => t('gallery.filters.chip.search', { value }),
      category: (value) => t('gallery.filters.chip.category', { value }),
      schengen: t('gallery.filters.chip.schengen'),
      nonSchengen: t('gallery.filters.chip.nonSchengen'),
    },
  })

  return (
    <div className="seg-destinations__filters">
      <Input
        className="seg-destinations__search"
        iconLeft={<Search size={16} />}
        label={t('gallery.filters.searchLabel')}
        placeholder={t('gallery.filters.searchPlaceholder')}
        value={searchText}
        onChange={(event) => {
          setSearchText(event.target.value)
          onChange({ search: event.target.value })
        }}
      />
      <label className="seg-destinations__field">
        <span>{t('gallery.filters.category')}</span>
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
        <span>{t('gallery.filters.schengen')}</span>
        <Select
          value={
            state.isSchengenArea == null ? '' : state.isSchengenArea ? 'true' : 'false'
          }
          onChange={(event) =>
            onChange({
              isSchengenArea:
                event.target.value === '' ? null : event.target.value === 'true',
            })
          }
          options={[
            anyOption,
            { value: 'true', label: t('gallery.filters.schengenOnly') },
            { value: 'false', label: t('gallery.filters.nonSchengenOnly') },
          ]}
        />
      </label>

      {chips.length > 0 && (
        <div
          className="seg-destinations__chips"
          role="group"
          aria-label={t('gallery.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-destinations__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('gallery.filters.remove', { label: chip.label })}
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
  clear: DestinationsFilterPatch
}

function buildFilterChips(
  state: DestinationsState,
  resolved: {
    categoryName: string
    labels: {
      search: (value: string) => string
      category: (value: string) => string
      schengen: string
      nonSchengen: string
    }
  },
): FilterChip[] {
  const chips: FilterChip[] = []
  if (state.search.trim() !== '') {
    chips.push({
      key: 'search',
      label: resolved.labels.search(state.search.trim()),
      clear: { search: '' },
    })
  }
  if (state.category != null) {
    chips.push({
      key: 'category',
      label: resolved.labels.category(resolved.categoryName || String(state.category)),
      clear: { category: null },
    })
  }
  if (state.isSchengenArea != null) {
    chips.push({
      key: 'schengen',
      label: state.isSchengenArea
        ? resolved.labels.schengen
        : resolved.labels.nonSchengen,
      clear: { isSchengenArea: null },
    })
  }
  return chips
}

interface DestinationGalleryProps {
  destinations: DestinationSummary[]
  state: DestinationsState
  busy: boolean
  onSort: (field: DestinationSortField) => void
  onOpen: (destinationId: number) => void
}

function DestinationGallery({
  destinations,
  state,
  busy,
  onSort,
  onOpen,
}: DestinationGalleryProps) {
  const { t } = useTranslation('destinations')
  const navigate = useNavigate()

  return (
    <section className="seg-destinations__gallery-wrap" aria-busy={busy}>
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
              {t(`gallery.sort.${field}`)}
            </button>
          )
        })}
      </div>
      <div className="seg-destinations__gallery">
        {destinations.map((destination) => (
          <article key={destination.id} className="seg-destinations-card">
            <button
              type="button"
              className="seg-destinations-card__open"
              onClick={() => onOpen(destination.id)}
              aria-label={t('gallery.open', { name: destination.name })}
            >
              <span className="seg-destinations-thumb">
                {destination.thumbnail.url == null ? (
                  <span className="seg-destinations-thumb__placeholder">
                    <ImageOff size={28} aria-hidden="true" />
                  </span>
                ) : (
                  <img src={destination.thumbnail.url} alt="" loading="lazy" />
                )}
                {destination.isSchengenArea && (
                  <span className="seg-destinations-thumb__badge">
                    🇪🇺 {t('gallery.badge.schengen')}
                  </span>
                )}
              </span>
            </button>
            <div className="seg-destinations-card__body">
              <div className="seg-destinations-card__title-row">
                <h2>{destination.name}</h2>
                <Badge
                  tone={destination.visibility === 'Private' ? 'neutral' : 'azure'}
                >
                  {t(`visibility.${destination.visibility}`)}
                </Badge>
              </div>
              <p className="seg-destinations-card__meta">
                {destination.country == null
                  ? destination.categoryName
                  : `${destination.categoryName} · ${destination.country}`}
              </p>
              <div className="seg-destinations-card__chips">
                <Badge tone="aqua">{destination.categoryName}</Badge>
                {destination.averagePlaceRating != null && (
                  <Badge
                    tone="gold"
                    title={t('gallery.badge.ratingCount', {
                      count: destination.ratedPlaceCount,
                    })}
                  >
                    <Star size={13} aria-hidden="true" />
                    {t('gallery.badge.rating', {
                      rating: destination.averagePlaceRating.toFixed(1),
                    })}
                  </Badge>
                )}
              </div>
              <Button
                variant="outline"
                size="sm"
                iconLeft={<MapPin size={15} />}
                onClick={() =>
                  void navigate(destinationPlacesRoutePath(destination.id))
                }
                aria-label={t('gallery.goToPlaces', { name: destination.name })}
              >
                {t('gallery.places')}
              </Button>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

interface PagerProps {
  page: number
  pageSize: DestinationPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: DestinationPageSize) => void
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
          onChange={(event) =>
            onPageSize(Number(event.target.value) as DestinationPageSize)
          }
          options={destinationPageSizes.map((size) => ({
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

interface DestinationDialogProps {
  mode: 'create' | 'edit'
  destinationId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (destination: Destination, mode: 'create' | 'edit') => void
  onDeleted: (destination: Destination) => void
}

function DestinationDialog({
  mode,
  destinationId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: DestinationDialogProps) {
  const { t } = useTranslation('destinations')
  const categories = useDestinationCategories()
  const destinationQuery = useQuery({
    queryKey: destinationsKeys.destination(destinationId as number),
    queryFn: ({ signal }) =>
      destinationsApi.getDestination(destinationId as number, signal),
    enabled: mode === 'edit' && destinationId != null,
  })

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  if (categories.data == null || (mode === 'edit' && destinationQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-destinations-editor__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && destinationQuery.isError) {
    const notFound =
      isApiError(destinationQuery.error) && destinationQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-destinations-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const destination =
    mode === 'edit' ? (destinationQuery.data as Destination) : undefined
  const firstCategoryId = categories.data[0]?.id
  const initialValues =
    destination != null
      ? fromDestination(destination)
      : buildDefaults(firstCategoryId == null ? '' : String(firstCategoryId))
  const canChangeVisibility =
    destination == null ||
    (currentUserId != null && destination.createdById === currentUserId)

  return (
    <DestinationEditorForm
      mode={mode}
      destinationId={destinationId}
      destination={destination}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
    />
  )
}

interface DestinationEditorFormProps {
  mode: 'create' | 'edit'
  destinationId?: number
  destination?: Destination
  title: string
  description: string
  initialValues: DestinationFormValues
  categories: ReadonlyArray<{ id: number; name: string }>
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (destination: Destination, mode: 'create' | 'edit') => void
  onDeleted: (destination: Destination) => void
}

function DestinationEditorForm({
  mode,
  destinationId,
  destination,
  title,
  description,
  initialValues,
  categories,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: DestinationEditorFormProps) {
  const { t } = useTranslation('destinations')
  const navigate = useNavigate()
  const schema = useMemo(
    () =>
      createDestinationSchema({
        nameRequired: t('editor.validation.nameRequired'),
        nameTooLong: t('editor.validation.nameTooLong'),
        categoryRequired: t('editor.validation.categoryRequired'),
        countryTooLong: t('editor.validation.countryTooLong'),
        entryRequirementsTooLong: t('editor.validation.entryRequirementsTooLong'),
        notesTooLong: t('editor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<DestinationFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState } = form
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdDestination, setCreatedDestination] = useState<Destination | null>(null)
  const editedRef = useRef(false)

  const deletionImpactQuery = useQuery({
    queryKey: [
      ...destinationsKeys.destination(destinationId ?? 0),
      'deletion-impact',
    ] as const,
    queryFn: ({ signal }) =>
      destinationsApi.getDestinationDeletionImpact(destinationId as number, signal),
    enabled: confirmingDelete && mode === 'edit' && destinationId != null,
    staleTime: 0,
    gcTime: 0,
    retry: false,
  })

  const mutation = useMutation({
    mutationFn: (request: CreateDestinationRequest) =>
      mode === 'create'
        ? destinationsApi.createDestination(request)
        : destinationsApi.updateDestination(destinationId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedDestination(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => destinationsApi.deleteDestination(destinationId as number),
    onSuccess: () => {
      if (destination != null) onDeleted(destination)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapDeleteError(error, t))
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

  const catalogOptions = categories.map((row) => ({
    value: String(row.id),
    label: row.name,
  }))
  const submitting = mutation.isPending

  if (createdDestination != null) {
    const finish = () => onSaved(createdDestination, 'create')
    return (
      <Dialog
        scrollable
        width={760}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdDestination.name,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-destinations-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <DestinationAttachments
            destinationId={createdDestination.id}
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
        width={760}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && destinationId != null && (
              <>
                <Button
                  variant="outline"
                  iconLeft={<MapPin size={15} />}
                  onClick={() =>
                    void navigate(destinationPlacesRoutePath(destinationId))
                  }
                  disabled={submitting}
                >
                  {t('gallery.places')}
                </Button>
                <Button
                  variant="ghost"
                  className="seg-destinations-editor__delete"
                  iconLeft={<Trash2 size={15} />}
                  onClick={() => setConfirmingDelete(true)}
                  disabled={submitting || deleteMutation.isPending}
                >
                  {t('editor.delete.action')}
                </Button>
              </>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-destinations-form" disabled={submitting}>
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
          id="seg-destinations-form"
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
            <h3>{t('editor.sections.general')}</h3>
            <Input
              label={t('editor.fields.name')}
              placeholder={t('editor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-destinations-editor__grid">
              <Field
                label={t('editor.fields.category')}
                error={formState.errors.categoryId?.message}
              >
                <Select
                  {...register('categoryId')}
                  aria-invalid={formState.errors.categoryId != null}
                  options={catalogOptions}
                />
              </Field>
              <Input
                label={t('editor.fields.country')}
                placeholder={t('editor.fields.countryPlaceholder')}
                error={formState.errors.country?.message}
                {...register('country')}
              />
              <label className="seg-destinations-editor__check">
                <input type="checkbox" {...register('isSchengenArea')} />
                <span>{t('editor.fields.isSchengenArea')}</span>
              </label>
              <ToggleField
                id="destinations-field-visibility"
                label={t('editor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="destinations-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-destinations-editor__section">
            <h3>{t('editor.sections.notes')}</h3>
            <TextAreaField
              label={t('editor.fields.entryRequirements')}
              placeholder={t('editor.fields.entryRequirementsPlaceholder')}
              error={formState.errors.entryRequirements?.message}
              rows={4}
              {...register('entryRequirements')}
            />
            <TextAreaField
              label={t('editor.fields.notes')}
              placeholder={t('editor.fields.notesPlaceholder')}
              error={formState.errors.notes?.message}
              rows={4}
              {...register('notes')}
            />
          </section>

          <section className="seg-destinations-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-destinations-editor__hint">
              {t('editor.attachments.hint')}
            </p>
            {mode === 'edit' && destinationId != null ? (
              <DestinationAttachments destinationId={destinationId} />
            ) : (
              <StagedDestinationAttachments
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
          title={t('editor.delete.title')}
          description={deleteDescription(
            deletionImpactQuery.data,
            deletionImpactQuery.isPending,
            deletionImpactQuery.isError,
            t,
          )}
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
                disabled={deleteMutation.isPending || deletionImpactQuery.isError}
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

interface ToggleFieldProps {
  id: string
  label: string
  hint?: string
  children: ReactNode
}

function ToggleField({ id, label, hint, children }: ToggleFieldProps) {
  return (
    <div className="seg-destinations-editor__field">
      <span className="seg-destinations-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && (
        <span className="seg-destinations-editor__field-hint">{hint}</span>
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

function deleteDescription(
  impact: DestinationDeletionImpact | undefined,
  loading: boolean,
  failed: boolean,
  t: (key: string, options?: Record<string, unknown>) => string,
): string {
  if (loading) return t('editor.delete.loadingImpact')
  if (failed) return t('editor.delete.impactError')
  if (impact == null || !impact.isReferenced) return t('editor.delete.impactNone')
  return t('editor.delete.impact', { count: impact.referenceCount })
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'destinations.destination.validation':
        return t('editor.errors.validation')
      case 'destinations.destination.duplicate_name':
        return t('editor.errors.duplicateName')
      case 'destinations.destination.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
      case 'destinations.catalog.unknown_reference':
        return t('editor.errors.unknownReference')
    }
    if (error.kind === 'not-found') return t('editor.notFound')
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}

function mapDeleteError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error) && error.kind === 'not-found') return t('editor.notFound')
  return t('editor.delete.error')
}
