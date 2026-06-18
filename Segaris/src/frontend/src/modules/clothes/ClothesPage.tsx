import { zodResolver } from '@hookform/resolvers/zod'
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import {
  ChevronLeft,
  ChevronRight,
  Globe,
  Lock,
  Plus,
  Search,
  Shirt,
  Trash2,
} from 'lucide-react'
import { useEffect, useId, useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  clothesApi,
  clothesPageSizes,
  type ClothesGarment,
  type ClothesGarmentSummary,
  type ClothesGarmentSortField,
  type ClothesGarmentStatus,
  type ClothesPageSize,
  type ClothesVisibility,
  type ClothingCategory,
  type ClothingColor,
  type CreateClothesGarmentRequest,
} from '@/app/api/clothes'
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
  Tooltip,
  type SegmentTone,
} from '@/components/ui'

import {
  dryCleaningCareSymbols,
  dryingCareSymbols,
  ironingCareSymbols,
  washingCareSymbols,
} from './careSymbols'
import { ClothesAttachments } from './ClothesAttachments'
import { clothesKeys } from './contracts'
import {
  activeGarmentFilterCount,
  type GarmentsFilterPatch,
  type GarmentsState,
  useGarmentsState,
} from './garmentsState'
import {
  buildDefaults,
  createGarmentSchema,
  dryCleaningCareValues,
  dryingCareValues,
  fromGarment,
  ironingCareValues,
  toRequest,
  washingCareValues,
  type GarmentFormValues,
} from './garmentForm'
import { useClothingCategories, useClothingColors } from './queries'

import './ClothesPage.css'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

const statuses: ClothesGarmentStatus[] = ['Active', 'Unavailable', 'Deprecated']
const visibilities: ClothesVisibility[] = ['Public', 'Private']
const sortFields: ClothesGarmentSortField[] = [
  'name',
  'category',
  'status',
  'visibility',
]

const visibilityMeta: Record<
  ClothesVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

export function ClothesPage() {
  const { t } = useTranslation('clothes')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const currentUserId = session?.userId ?? null
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
  } = useGarmentsState(currentUserId)

  const categories = useClothingCategories()
  const colors = useClothingColors()
  const garmentsQuery = useQuery({
    queryKey: clothesKeys.garmentList(listQuery),
    queryFn: ({ signal }) => clothesApi.listGarments(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = garmentsQuery.data
  const garments = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeGarmentFilterCount(state) > 0

  const invalidateGarments = (garmentId?: number) => {
    void queryClient.invalidateQueries({ queryKey: clothesKeys.garments() })
    if (garmentId != null)
      void queryClient.invalidateQueries({ queryKey: clothesKeys.garment(garmentId) })
  }

  const handleSaved = (garment: ClothesGarment, mode: 'create' | 'edit') => {
    queryClient.setQueryData(clothesKeys.garment(garment.id), garment)
    invalidateGarments(garment.id)
    setToast({ kind: mode === 'create' ? 'created' : 'updated', name: garment.name })
    closeDialog()
  }

  const handleDeleted = (garment: ClothesGarment) => {
    invalidateGarments()
    setToast({ kind: 'deleted', name: garment.name })
    closeDialog()
  }

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (garmentsQuery.isError) {
    const error = garmentsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void garmentsQuery.refetch()} />
    }
  }

  return (
    <main className="seg-clothes armali-aurora">
      <section className="seg-clothes__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <section className="seg-clothes__panel-head">
        <Badge tone="neutral">{t('gallery.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
          {t('gallery.newGarment')}
        </Button>
      </section>

      <GarmentsFilters
        state={state}
        categories={categories.data ?? []}
        colors={colors.data ?? []}
        onChange={setFilters}
        onClear={clearFilters}
      />

      {garmentsQuery.isPending ? (
        <div className="seg-clothes__loading">
          <Spinner />
        </div>
      ) : garmentsQuery.isError ? (
        <p className="seg-clothes__error" role="alert">
          {t('states.loadError')}
        </p>
      ) : garments.length === 0 ? (
        <p className="seg-clothes__empty">
          {hasFilters ? t('states.emptyFiltered') : t('states.empty')}
        </p>
      ) : (
        <GarmentGallery
          garments={garments}
          state={state}
          busy={garmentsQuery.isFetching && !garmentsQuery.isPending}
          onSort={setSort}
          onOpen={openEditDialog}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={garmentsQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
      />

      {dialog.mode !== 'closed' && (
        <GarmentDialog
          mode={dialog.mode}
          garmentId={dialog.mode === 'edit' ? dialog.garmentId : undefined}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {toast != null && (
        <div className="seg-clothes__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            onClose={() => setToast(null)}
            closeLabel={t('editor.actions.cancel')}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

interface GarmentsFiltersProps {
  state: GarmentsState
  categories: ClothingCategory[]
  colors: ClothingColor[]
  onChange: (patch: GarmentsFilterPatch) => void
  onClear: () => void
}

function GarmentsFilters({
  state,
  categories,
  colors,
  onChange,
  onClear,
}: GarmentsFiltersProps) {
  const { t } = useTranslation('clothes')
  const count = activeGarmentFilterCount(state)

  return (
    <section
      className="seg-clothes__filters"
      aria-label={t('filters.active', { count })}
    >
      <div className="seg-clothes__filters-primary">
        <Input
          className="seg-clothes__search"
          label={t('filters.search')}
          placeholder={t('filters.searchPlaceholder')}
          iconLeft={<Search size={16} />}
          value={state.search}
          onChange={(event) => onChange({ search: event.target.value })}
        />
        <FilterSelect
          label={t('filters.category')}
          value={state.category == null ? '' : String(state.category)}
          onChange={(value) =>
            onChange({ category: value === '' ? null : Number(value) })
          }
          options={categories.map((category) => ({
            value: String(category.id),
            label: category.name,
          }))}
        />
        <FilterSelect
          label={t('filters.status')}
          value={state.status}
          onChange={(value) => onChange({ status: value as ClothesGarmentStatus | '' })}
          options={statuses.map((status) => ({
            value: status,
            label: t(`status.${status}`),
          }))}
        />
        <FilterSelect
          label={t('filters.color')}
          value={state.color == null ? '' : String(state.color)}
          onChange={(value) => onChange({ color: value === '' ? null : Number(value) })}
          options={colors.map((color) => ({
            value: String(color.id),
            label: color.name,
          }))}
        />
        {count > 0 && (
          <Button variant="ghost" onClick={onClear}>
            {t('filters.clear')}
          </Button>
        )}
      </div>
    </section>
  )
}

interface FilterSelectProps {
  label: string
  value: string
  options: Array<{ value: string; label: string }>
  onChange: (value: string) => void
}

function FilterSelect({ label, value, options, onChange }: FilterSelectProps) {
  const { t } = useTranslation('clothes')
  return (
    <label className="seg-clothes__field">
      <span>{label}</span>
      <Select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        options={[{ value: '', label: t('filters.all') }, ...options]}
      />
    </label>
  )
}

interface GarmentGalleryProps {
  garments: ClothesGarmentSummary[]
  state: GarmentsState
  busy: boolean
  onSort: (sort: ClothesGarmentSortField) => void
  onOpen: (garmentId: number) => void
}

function GarmentGallery({
  garments,
  state,
  busy,
  onSort,
  onOpen,
}: GarmentGalleryProps) {
  const { t } = useTranslation('clothes')
  return (
    <section className="seg-clothes__gallery-wrap" aria-busy={busy}>
      <div className="seg-clothes__sortbar">
        <span className="seg-clothes__sort-label">{t('sort.label')}</span>
        {sortFields.map((field) => (
          <button
            key={field}
            type="button"
            className={
              'seg-clothes__sort' +
              (state.sort === field ? ' seg-clothes__sort--active' : '')
            }
            onClick={() => onSort(field)}
          >
            {t(`sort.${field}`)}
            {state.sort === field && <span>{t(`sort.${state.sortDirection}`)}</span>}
          </button>
        ))}
      </div>
      <div className="seg-clothes__gallery">
        {garments.map((garment) => (
          <article key={garment.id} className="seg-clothes-card">
            <button
              type="button"
              className="seg-clothes-card__open"
              onClick={() => onOpen(garment.id)}
              aria-label={t('gallery.open', { name: garment.name })}
            >
              {garment.thumbnail.url != null ? (
                <img
                  src={garment.thumbnail.url}
                  alt={t('gallery.thumbnailAlt', { name: garment.name })}
                  className="seg-clothes-card__image"
                />
              ) : (
                <span className="seg-clothes-card__placeholder">
                  <Shirt size={36} aria-hidden="true" />
                  <span>{t('gallery.placeholder')}</span>
                </span>
              )}
            </button>
            <div className="seg-clothes-card__body">
              <div className="seg-clothes-card__title-row">
                <h2>{garment.name}</h2>
                <Badge tone={garment.visibility === 'Private' ? 'neutral' : 'aqua'}>
                  {t(`visibility.${garment.visibility}`)}
                </Badge>
              </div>
              <p className="seg-clothes-card__meta">
                {garment.categoryName}
                {garment.size != null ? ` · ${garment.size}` : ''}
              </p>
              <div className="seg-clothes-card__chips">
                <Badge tone="neutral">{t(`status.${garment.status}`)}</Badge>
                <span>{t('gallery.by', { name: garment.creatorName })}</span>
              </div>
              <ColorSwatches colors={garment.colors} />
              <CareSymbolsRow garment={garment} />
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

function CareSymbolsRow({ garment }: { garment: ClothesGarmentSummary }) {
  const { t } = useTranslation('clothes')
  const symbols = [
    garment.washingCare == null
      ? null
      : {
          key: 'washing',
          src: washingCareSymbols[garment.washingCare],
          label: t(`care.washingValues.${garment.washingCare}`),
        },
    garment.dryingCare == null
      ? null
      : {
          key: 'drying',
          src: dryingCareSymbols[garment.dryingCare],
          label: t(`care.dryingValues.${garment.dryingCare}`),
        },
    garment.ironingCare == null
      ? null
      : {
          key: 'ironing',
          src: ironingCareSymbols[garment.ironingCare],
          label: t(`care.ironingValues.${garment.ironingCare}`),
        },
    garment.dryCleaningCare == null
      ? null
      : {
          key: 'dry-cleaning',
          src: dryCleaningCareSymbols[garment.dryCleaningCare],
          label: t(`care.dryCleaningValues.${garment.dryCleaningCare}`),
        },
  ].filter(
    (symbol): symbol is { key: string; src: string; label: string } => symbol != null,
  )

  if (symbols.length === 0) return null

  return (
    <ul className="seg-clothes-care-icons" aria-label={t('editor.sections.care')}>
      {symbols.map((symbol) => (
        <li key={symbol.key}>
          <Tooltip label={symbol.label}>
            <span className="seg-clothes-care-icons__trigger">
              <img src={symbol.src} alt={symbol.label} />
            </span>
          </Tooltip>
        </li>
      ))}
    </ul>
  )
}

function ColorSwatches({ colors }: { colors: ClothingColor[] }) {
  const { t } = useTranslation('clothes')
  if (colors.length === 0) {
    return <p className="seg-clothes-card__muted">{t('gallery.noColours')}</p>
  }
  return (
    <ul className="seg-clothes-colors" aria-label={t('filters.color')}>
      {colors.map((color) => (
        <li key={color.id} title={color.name}>
          <span
            className="seg-clothes-colors__swatch"
            style={{ backgroundColor: color.colorValue }}
          />
          <span>{color.name}</span>
        </li>
      ))}
    </ul>
  )
}

interface PagerProps {
  page: number
  pageSize: ClothesPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: ClothesPageSize) => void
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
}: PagerProps) {
  const { t } = useTranslation('clothes')
  return (
    <nav className="seg-clothes__pager" aria-label={t('pagination.label')}>
      <label className="seg-clothes__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) =>
            onPageSize(Number(event.target.value) as ClothesPageSize)
          }
          options={clothesPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-clothes__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-clothes__page" aria-live="polite">
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

interface GarmentDialogProps {
  mode: 'create' | 'edit'
  garmentId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (garment: ClothesGarment, mode: 'create' | 'edit') => void
  onDeleted: (garment: ClothesGarment) => void
}

function GarmentDialog({
  mode,
  garmentId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: GarmentDialogProps) {
  const { t } = useTranslation('clothes')
  const categories = useClothingCategories()
  const colors = useClothingColors()
  const garmentQuery = useQuery({
    queryKey: clothesKeys.garment(garmentId as number),
    queryFn: ({ signal }) => clothesApi.getGarment(garmentId as number, signal),
    enabled: mode === 'edit' && garmentId != null,
  })

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')
  const catalogsReady = categories.data != null && colors.data != null

  if (!catalogsReady || (mode === 'edit' && garmentQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={820}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-clothes-editor__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && garmentQuery.isError) {
    const notFound =
      isApiError(garmentQuery.error) && garmentQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={640}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-clothes-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const garment = mode === 'edit' ? (garmentQuery.data as ClothesGarment) : undefined
  const initialValues =
    garment != null
      ? fromGarment(garment)
      : buildDefaults(firstCatalogId(categories.data))
  const canChangeVisibility =
    garment == null || (currentUserId != null && garment.createdById === currentUserId)

  return (
    <GarmentEditorForm
      mode={mode}
      garmentId={garmentId}
      garment={garment}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data ?? []}
      colors={colors.data ?? []}
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

interface GarmentEditorFormProps {
  mode: 'create' | 'edit'
  garmentId?: number
  garment?: ClothesGarment
  title: string
  description: string
  initialValues: GarmentFormValues
  categories: ClothingCategory[]
  colors: ClothingColor[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (garment: ClothesGarment, mode: 'create' | 'edit') => void
  onDeleted: (garment: ClothesGarment) => void
}

function GarmentEditorForm({
  mode,
  garmentId,
  garment,
  title,
  description,
  initialValues,
  categories,
  colors,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: GarmentEditorFormProps) {
  const { t } = useTranslation('clothes')
  const schema = useMemo(
    () =>
      createGarmentSchema({
        nameRequired: t('editor.validation.nameRequired'),
        nameTooLong: t('editor.validation.nameTooLong'),
        categoryRequired: t('editor.validation.categoryRequired'),
        sizeTooLong: t('editor.validation.sizeTooLong'),
        notesTooLong: t('editor.validation.notesTooLong'),
      }),
    [t],
  )
  const form = useForm<GarmentFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, setValue } = form
  const selectedColorIds = useWatch({ control, name: 'colorIds' }) ?? []
  const washingCare = useWatch({ control, name: 'washingCare' })
  const dryingCare = useWatch({ control, name: 'dryingCare' })
  const ironingCare = useWatch({ control, name: 'ironingCare' })
  const dryCleaningCare = useWatch({ control, name: 'dryCleaningCare' })

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdGarment, setCreatedGarment] = useState<ClothesGarment | null>(null)
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CreateClothesGarmentRequest) =>
      mode === 'create'
        ? clothesApi.createGarment(request)
        : clothesApi.updateGarment(garmentId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedGarment(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => clothesApi.deleteGarment(garmentId as number),
    onSuccess: () => {
      if (garment != null) onDeleted(garment)
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

  const toggleColor = (colorId: number, checked: boolean) => {
    editedRef.current = true
    const next = checked
      ? [...selectedColorIds, colorId]
      : selectedColorIds.filter((id) => id !== colorId)
    setValue('colorIds', next, { shouldValidate: formState.isSubmitted })
  }

  const submitting = mutation.isPending

  if (createdGarment != null) {
    const finish = () => onSaved(createdGarment, 'create')
    return (
      <Dialog
        scrollable
        width={820}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdGarment.name,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-clothes-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <ClothesAttachments garmentId={createdGarment.id} autoUpload={stagedFiles} />
        </section>
      </Dialog>
    )
  }

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
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-clothes-editor__delete"
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
            <Button type="submit" form="seg-clothes-form" disabled={submitting}>
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
          id="seg-clothes-form"
          className="seg-clothes-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-clothes-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-clothes-editor__section">
            <h3>{t('editor.sections.general')}</h3>
            <Input
              label={t('editor.fields.name')}
              placeholder={t('editor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-clothes-editor__grid">
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
              <Field label={t('editor.fields.status')}>
                <Select
                  {...register('status')}
                  options={statuses.map((status) => ({
                    value: status,
                    label: t(`status.${status}`),
                  }))}
                />
              </Field>
              <Input
                label={t('editor.fields.size')}
                placeholder={t('editor.fields.sizePlaceholder')}
                error={formState.errors.size?.message}
                {...register('size')}
              />
              <ToggleField
                id="clothes-field-visibility"
                label={t('editor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="clothes-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((visibility) => ({
                    value: visibility,
                    label: t(`visibility.${visibility}`),
                    icon: visibilityMeta[visibility].icon,
                    tone: visibilityMeta[visibility].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-clothes-editor__section">
            <h3>{t('editor.sections.colours')}</h3>
            <p className="seg-clothes-editor__hint">{t('editor.colours.hint')}</p>
            <fieldset className="seg-clothes-editor__colors">
              <legend className="seg-clothes__sr">
                {t('editor.sections.colours')}
              </legend>
              {colors.length === 0 ? (
                <p className="seg-clothes-editor__hint">{t('editor.colours.none')}</p>
              ) : (
                colors.map((color) => (
                  <label key={color.id} className="seg-clothes-editor__color">
                    <input
                      type="checkbox"
                      checked={selectedColorIds.includes(color.id)}
                      onChange={(event) => toggleColor(color.id, event.target.checked)}
                    />
                    <span
                      className="seg-clothes-editor__swatch"
                      style={{ backgroundColor: color.colorValue }}
                    />
                    <span>{color.name}</span>
                  </label>
                ))
              )}
            </fieldset>
          </section>

          <section className="seg-clothes-editor__section">
            <h3>{t('editor.sections.care')}</h3>
            <div className="seg-clothes-editor__care-grid">
              <CarePicker
                label={t('care.washing')}
                value={washingCare}
                onChange={(value) => {
                  editedRef.current = true
                  setValue('washingCare', value as GarmentFormValues['washingCare'])
                }}
                options={washingCareValues.map((value) => ({
                  value,
                  label: t(`care.washingValues.${value}`),
                  symbol: washingCareSymbols[value],
                }))}
              />
              <CarePicker
                label={t('care.drying')}
                value={dryingCare}
                onChange={(value) => {
                  editedRef.current = true
                  setValue('dryingCare', value as GarmentFormValues['dryingCare'])
                }}
                options={dryingCareValues.map((value) => ({
                  value,
                  label: t(`care.dryingValues.${value}`),
                  symbol: dryingCareSymbols[value],
                }))}
              />
              <CarePicker
                label={t('care.ironing')}
                value={ironingCare}
                onChange={(value) => {
                  editedRef.current = true
                  setValue('ironingCare', value as GarmentFormValues['ironingCare'])
                }}
                options={ironingCareValues.map((value) => ({
                  value,
                  label: t(`care.ironingValues.${value}`),
                  symbol: ironingCareSymbols[value],
                }))}
              />
              <CarePicker
                label={t('care.dryCleaning')}
                value={dryCleaningCare}
                onChange={(value) => {
                  editedRef.current = true
                  setValue(
                    'dryCleaningCare',
                    value as GarmentFormValues['dryCleaningCare'],
                  )
                }}
                options={dryCleaningCareValues.map((value) => ({
                  value,
                  label: t(`care.dryCleaningValues.${value}`),
                  symbol: dryCleaningCareSymbols[value],
                }))}
              />
            </div>
          </section>

          <section className="seg-clothes-editor__section">
            <h3>{t('editor.sections.notes')}</h3>
            <label className="seg-clothes-editor__notes">
              <span className="seg-clothes-editor__notes-label">
                {t('editor.fields.notes')}
              </span>
              <textarea
                className="seg-clothes-editor__textarea"
                rows={4}
                placeholder={t('editor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-clothes-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-clothes-editor__section">
            <h3>{t('editor.sections.attachments')}</h3>
            <p className="seg-clothes-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && garmentId != null ? (
              <ClothesAttachments garmentId={garmentId} />
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

interface CarePickerProps {
  label: string
  value: string
  options: Array<{ value: string; label: string; symbol: string }>
  onChange: (value: string) => void
}

function CarePicker({ label, value, options, onChange }: CarePickerProps) {
  const { t } = useTranslation('clothes')
  const id = useId()
  const groupName = `care-${label.toLowerCase().replace(/\s+/g, '-')}`
  return (
    <fieldset className="seg-clothes-care">
      <legend>{label}</legend>
      <div className="seg-clothes-care__option">
        <input
          id={`${id}-none`}
          type="radio"
          name={groupName}
          checked={value === ''}
          onChange={() => onChange('')}
        />
        <label className="seg-clothes-care__tile" htmlFor={`${id}-none`}>
          <span className="seg-clothes-care__empty">{t('care.none')}</span>
        </label>
      </div>
      {options.map((option) => (
        <div key={option.value} className="seg-clothes-care__option">
          <input
            id={`${id}-${option.value}`}
            type="radio"
            name={groupName}
            checked={value === option.value}
            onChange={() => onChange(option.value)}
          />
          <label className="seg-clothes-care__tile" htmlFor={`${id}-${option.value}`}>
            <img src={option.symbol} alt="" aria-hidden="true" />
            <span>{option.label}</span>
          </label>
        </div>
      ))}
    </fieldset>
  )
}

interface StagedAttachmentsProps {
  files: File[]
  onChange: (files: File[]) => void
}

function StagedAttachments({ files, onChange }: StagedAttachmentsProps) {
  const { t } = useTranslation('clothes')
  const input = useRef<HTMLInputElement>(null)
  return (
    <div className="seg-clothes-staged">
      <Button
        variant="outline"
        size="sm"
        onClick={() => input.current?.click()}
        iconLeft={<Plus size={15} />}
      >
        {t('editor.attachments.add')}
      </Button>
      <input
        ref={input}
        type="file"
        multiple
        className="seg-clothes-staged__input"
        onChange={(event) => {
          const next = event.target.files == null ? [] : Array.from(event.target.files)
          onChange([...files, ...next])
          event.target.value = ''
        }}
        aria-label={t('editor.attachments.add')}
      />
      {files.length === 0 ? (
        <p className="seg-clothes-staged__empty">{t('editor.attachments.empty')}</p>
      ) : (
        <ul className="seg-clothes-staged__list">
          {files.map((file, index) => (
            <li key={`${file.name}-${index}`} className="seg-clothes-staged__item">
              <span>{file.name}</span>
              <button
                type="button"
                onClick={() =>
                  onChange(files.filter((_, candidate) => candidate !== index))
                }
                aria-label={t('editor.attachments.remove')}
              >
                <Trash2 size={15} aria-hidden="true" />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
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
    <div className="seg-clothes-editor__field">
      <label className="seg-clothes-editor__field-control">
        <span className="seg-clothes-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-clothes-editor__field-hint' +
            (error != null ? ' seg-clothes-editor__field-hint--error' : '')
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
    <div className="seg-clothes-editor__field">
      <span className="seg-clothes-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-clothes-editor__field-hint">{hint}</span>}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'clothes.garment.validation':
        return t('editor.errors.validation')
      case 'clothes.garment.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
      case 'clothes.catalog.unknown_reference':
        return t('editor.errors.unknownReference')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}
