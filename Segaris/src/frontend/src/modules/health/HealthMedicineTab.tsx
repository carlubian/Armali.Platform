import { zodResolver } from '@hookform/resolvers/zod'
import {
  ArrowDownAZ,
  ArrowUpZA,
  ChevronLeft,
  ChevronRight,
  FileText,
  Globe,
  Lock,
  Package,
  Pill,
  Plus,
  Search,
  Trash2,
  X,
} from 'lucide-react'
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  healthApi,
  healthPageSizes,
  type DiseaseSummary,
  type HealthPageSize,
  type HealthVisibility,
  type Medicine,
  type MedicineCategory,
  type MedicineRequest,
  type MedicineSortField,
  type MedicineSummary,
} from '@/app/api/health'
import type { InventoryItemSummary } from '@/app/api/inventory'
import { isApiError } from '@/app/api/errors'
import {
  EntityReferenceField,
  type EntityReference,
} from '@/components/entity-selection'
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
  type BadgeTone,
  type SegmentTone,
} from '@/components/ui'
import {
  InventoryItemEntitySelector,
  inventoryItemReference,
} from '@/modules/recipes/InventoryItemEntitySelector'
import {
  attachmentAccept,
  formatFileSize,
  rejectionFor,
} from '@/modules/clothes/attachments'

import { DiseaseEntitySelector } from './DiseaseEntitySelector'
import { MedicineAttachments } from './MedicineAttachments'
import { healthKeys, useMedicineCategories } from './queries'
import {
  activeMedicineFilterCount,
  type MedicineFilterPatch,
  type MedicineListState,
  type useHealthState,
} from './healthState'
import {
  buildMedicineDefaults,
  createMedicineFormSchema,
  fromMedicine,
  toMedicineRequest,
  type MedicineFormValues,
} from './medicineForm'

const visibilities: HealthVisibility[] = ['Public', 'Private']
const medicineSortFields: MedicineSortField[] = ['name', 'category']

const visibilityMeta: Record<HealthVisibility, { icon: ReactNode; tone: SegmentTone }> =
  {
    Public: { icon: <Globe size={15} />, tone: 'accent' },
    Private: { icon: <Lock size={15} />, tone: 'neutral' },
  }
const visibilityBadgeTone: Record<HealthVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

type MedicineToastKind = 'medicineCreated' | 'medicineUpdated' | 'medicineDeleted'
interface MedicineToastState {
  kind: MedicineToastKind
  name: string
}

export interface MedicinesTabProps {
  health: ReturnType<typeof useHealthState>
  currentUserId: number | null
}

export function MedicinesTab({ health, currentUserId }: MedicinesTabProps) {
  const { t } = useTranslation('health')
  const queryClient = useQueryClient()
  const [toast, setToast] = useState<MedicineToastState | null>(null)

  const {
    state,
    dialog,
    medicineListQuery,
    setMedicineFilters,
    clearMedicineFilters,
    setMedicineSort,
    setMedicinePage,
    setMedicinePageSize,
    openCreateMedicine,
    openEditMedicine,
    closeDialog,
  } = health

  const categories = useMedicineCategories()
  const medicinesQuery = useQuery({
    queryKey: healthKeys.medicineList(medicineListQuery),
    queryFn: ({ signal }) => healthApi.listMedicines(medicineListQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = medicinesQuery.data
  const medicines = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.medicines.pageSize))
  const hasFilters = activeMedicineFilterCount(state.medicines) > 0

  const invalidate = (medicineId?: number) => {
    void queryClient.invalidateQueries({ queryKey: healthKeys.medicines() })
    void queryClient.invalidateQueries({ queryKey: healthKeys.diseases() })
    if (medicineId != null) {
      void queryClient.invalidateQueries({ queryKey: healthKeys.medicine(medicineId) })
      void queryClient.invalidateQueries({
        queryKey: healthKeys.medicineDiseases(medicineId),
      })
      void queryClient.invalidateQueries({
        queryKey: healthKeys.medicineAttachments(medicineId),
      })
    }
  }

  const handleSaved = (medicine: Medicine, mode: 'create' | 'edit') => {
    queryClient.setQueryData(healthKeys.medicine(medicine.id), medicine)
    invalidate(medicine.id)
    setToast({
      kind: mode === 'create' ? 'medicineCreated' : 'medicineUpdated',
      name: medicine.name,
    })
    closeDialog()
  }

  const handleDeleted = (medicine: Medicine) => {
    invalidate(medicine.id)
    setToast({ kind: 'medicineDeleted', name: medicine.name })
    closeDialog()
  }

  if (medicinesQuery.isError) {
    const error = medicinesQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void medicinesQuery.refetch()} />
    }
  }

  return (
    <section className="seg-health__tab">
      <div className="seg-health__toolbar">
        <Badge tone="neutral">{t('medicines.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateMedicine}>
          {t('medicines.newMedicine')}
        </Button>
      </div>

      <MedicineFilters
        state={state.medicines}
        categories={categories.data ?? []}
        onChange={setMedicineFilters}
        onClear={clearMedicineFilters}
      />

      {medicinesQuery.isPending ? (
        <div className="seg-health__loading">
          <Spinner label={t('medicines.states.loading')} />
        </div>
      ) : medicinesQuery.isError ? (
        <p className="seg-health__error" role="alert">
          {t('medicines.states.loadError')}
        </p>
      ) : medicines.length === 0 ? (
        <p className="seg-health__empty">
          {hasFilters
            ? t('medicines.states.emptyFiltered')
            : t('medicines.states.empty')}
        </p>
      ) : (
        <MedicineGallery
          medicines={medicines}
          state={state.medicines}
          busy={medicinesQuery.isFetching && !medicinesQuery.isPending}
          onSort={setMedicineSort}
          onOpen={openEditMedicine}
        />
      )}

      <Pager
        page={state.medicines.page}
        pageSize={state.medicines.pageSize}
        totalPages={totalPages}
        fetching={medicinesQuery.isFetching}
        onPage={setMedicinePage}
        onPageSize={setMedicinePageSize}
      />

      {dialog.mode === 'createMedicine' || dialog.mode === 'editMedicine' ? (
        <MedicineDialog
          mode={dialog.mode === 'createMedicine' ? 'create' : 'edit'}
          medicineId={dialog.mode === 'editMedicine' ? dialog.medicineId : undefined}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      ) : null}

      {toast != null && (
        <div className="seg-health__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            closeLabel={t('medicineEditor.actions.cancel')}
            onClose={() => setToast(null)}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </section>
  )
}

interface MedicineFiltersProps {
  state: MedicineListState
  categories: MedicineCategory[]
  onChange: (patch: MedicineFilterPatch) => void
  onClear: () => void
}

function MedicineFilters({
  state,
  categories,
  onChange,
  onClear,
}: MedicineFiltersProps) {
  const { t } = useTranslation('health')
  const count = activeMedicineFilterCount(state)
  return (
    <section
      className="seg-health__filters"
      aria-label={t('medicines.filters.active', { count })}
    >
      <Input
        className="seg-health__search"
        label={t('medicines.filters.search')}
        placeholder={t('medicines.filters.searchPlaceholder')}
        iconLeft={<Search size={16} />}
        value={state.search}
        onChange={(event) => onChange({ search: event.target.value })}
      />
      <FilterSelect
        label={t('medicines.filters.category')}
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
        label={t('medicines.filters.prescription')}
        value={
          state.requiresPrescription == null ? '' : String(state.requiresPrescription)
        }
        allLabel={t('medicines.filters.anyPrescription')}
        onChange={(value) =>
          onChange({
            requiresPrescription: value === '' ? null : value === 'true',
          })
        }
        options={[
          { value: 'true', label: t('medicines.filters.requiresPrescription') },
          { value: 'false', label: t('medicines.filters.noPrescription') },
        ]}
      />
      <FilterSelect
        label={t('medicines.filters.visibility')}
        value={state.visibility}
        allLabel={t('medicines.filters.allVisibilities')}
        onChange={(value) => onChange({ visibility: value as HealthVisibility | '' })}
        options={visibilities.map((value) => ({
          value,
          label: t(`visibility.${value}`),
        }))}
      />
      <label className="seg-health__mine">
        <input
          type="checkbox"
          checked={state.mine}
          onChange={(event) => onChange({ mine: event.target.checked })}
        />
        <span>{t('medicines.filters.mine')}</span>
      </label>
      {count > 0 && (
        <Button variant="ghost" onClick={onClear}>
          {t('medicines.filters.clear')}
        </Button>
      )}
    </section>
  )
}

interface FilterSelectProps {
  label: string
  value: string
  options: Array<{ value: string; label: string }>
  onChange: (value: string) => void
  allLabel?: string
}

function FilterSelect({
  label,
  value,
  options,
  onChange,
  allLabel,
}: FilterSelectProps) {
  const { t } = useTranslation('health')
  return (
    <label className="seg-health__field">
      <span>{label}</span>
      <Select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        options={[
          { value: '', label: allLabel ?? t('medicines.filters.all') },
          ...options,
        ]}
      />
    </label>
  )
}

interface MedicineGalleryProps {
  medicines: MedicineSummary[]
  state: MedicineListState
  busy: boolean
  onSort: (field: MedicineSortField) => void
  onOpen: (medicineId: number) => void
}

function MedicineGallery({
  medicines,
  state,
  busy,
  onSort,
  onOpen,
}: MedicineGalleryProps) {
  const { t } = useTranslation('health')
  return (
    <section className="seg-health-gallery-wrap" aria-busy={busy}>
      <div className="seg-health-gallery__sort" aria-label={t('medicines.sort.label')}>
        {medicineSortFields.map((field) => {
          const active = state.sort === field
          return (
            <Button
              key={field}
              variant={active ? 'outline' : 'ghost'}
              size="sm"
              iconRight={
                active ? (
                  state.sortDirection === 'asc' ? (
                    <ArrowDownAZ size={14} />
                  ) : (
                    <ArrowUpZA size={14} />
                  )
                ) : undefined
              }
              onClick={() => onSort(field)}
            >
              {t(`medicines.sort.${field}`)}
            </Button>
          )
        })}
      </div>
      <div className="seg-health-gallery">
        {medicines.map((medicine) => (
          <article key={medicine.id} className="seg-health-card">
            <button
              type="button"
              className="seg-health-card__open"
              onClick={() => onOpen(medicine.id)}
              aria-label={t('medicines.open', { name: medicine.name })}
            >
              <span className="seg-health-card__thumb">
                {medicine.thumbnail.url != null ? (
                  <img
                    src={medicine.thumbnail.url}
                    alt={t('medicines.thumbnailAlt', { name: medicine.name })}
                  />
                ) : (
                  <Pill size={30} aria-hidden="true" />
                )}
              </span>
              <span className="seg-health-card__body">
                <strong>{medicine.name}</strong>
                <span>{medicine.categoryName}</span>
              </span>
            </button>
            <div className="seg-health-card__meta">
              {medicine.requiresPrescription && (
                <Badge tone="gold">{t('medicines.prescriptionRequired')}</Badge>
              )}
              <Badge tone={visibilityBadgeTone[medicine.visibility]}>
                {t(`visibility.${medicine.visibility}`)}
              </Badge>
            </div>
            {medicine.inventoryItemId != null && (
              <div className="seg-health-card__item">
                <Package size={14} aria-hidden="true" />
                <span>
                  {medicine.inventoryItemName ?? t('medicines.itemUnavailable')}
                </span>
              </div>
            )}
          </article>
        ))}
      </div>
    </section>
  )
}

interface PagerProps {
  page: number
  pageSize: HealthPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: HealthPageSize) => void
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
}: PagerProps) {
  const { t } = useTranslation('health')
  return (
    <nav className="seg-health__pager" aria-label={t('medicines.pagination.label')}>
      <label className="seg-health__rows">
        <span>{t('medicines.pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) => onPageSize(Number(event.target.value) as HealthPageSize)}
          options={healthPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-health__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('medicines.pagination.previous')}
        </Button>
        <span className="seg-health__page" aria-live="polite">
          {t('medicines.pagination.status', { page, pages: totalPages })}
        </span>
        <Button
          variant="ghost"
          size="sm"
          iconRight={<ChevronRight size={16} />}
          disabled={page >= totalPages || fetching}
          onClick={() => onPage(Math.min(totalPages, page + 1))}
        >
          {t('medicines.pagination.next')}
        </Button>
      </div>
    </nav>
  )
}

interface MedicineDialogProps {
  mode: 'create' | 'edit'
  medicineId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (medicine: Medicine, mode: 'create' | 'edit') => void
  onDeleted: (medicine: Medicine) => void
}

function MedicineDialog({
  mode,
  medicineId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: MedicineDialogProps) {
  const { t } = useTranslation('health')
  const categories = useMedicineCategories()
  const medicineQuery = useQuery({
    queryKey: healthKeys.medicine(medicineId as number),
    queryFn: ({ signal }) => healthApi.getMedicine(medicineId as number, signal),
    enabled: mode === 'edit' && medicineId != null,
  })
  const diseasesQuery = useQuery({
    queryKey: healthKeys.medicineDiseases(medicineId as number),
    queryFn: ({ signal }) =>
      healthApi.listMedicineDiseases(medicineId as number, signal),
    enabled: mode === 'edit' && medicineId != null,
  })

  const title =
    mode === 'create' ? t('medicineEditor.createTitle') : t('medicineEditor.editTitle')
  const description =
    mode === 'create'
      ? t('medicineEditor.createDescription')
      : t('medicineEditor.editDescription')

  const loading =
    categories.data == null ||
    (mode === 'edit' && (medicineQuery.isPending || diseasesQuery.isPending))

  if (loading) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('medicineEditor.actions.cancel')}
      >
        <div className="seg-health-editor__status">
          <Spinner />
          <span>{t('medicineEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && medicineQuery.isError) {
    const notFound =
      isApiError(medicineQuery.error) && medicineQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('medicineEditor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('medicineEditor.actions.cancel')}</Button>}
      >
        <p className="seg-health-editor__error" role="alert">
          {notFound ? t('medicineEditor.notFound') : t('medicineEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const medicine = mode === 'edit' ? (medicineQuery.data as Medicine) : undefined
  const initialValues =
    medicine != null
      ? fromMedicine(medicine)
      : buildMedicineDefaults(firstCatalogId(categories.data))
  const canChangeVisibility =
    medicine == null || (currentUserId != null && medicine.creatorId === currentUserId)

  return (
    <MedicineEditorForm
      mode={mode}
      medicineId={medicineId}
      medicine={medicine}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data}
      initialDiseases={mode === 'edit' ? (diseasesQuery.data ?? []) : []}
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

interface MedicineEditorFormProps {
  mode: 'create' | 'edit'
  medicineId?: number
  medicine?: Medicine
  title: string
  description: string
  initialValues: MedicineFormValues
  categories: MedicineCategory[]
  initialDiseases: DiseaseSummary[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (medicine: Medicine, mode: 'create' | 'edit') => void
  onDeleted: (medicine: Medicine) => void
}

function MedicineEditorForm({
  mode,
  medicineId,
  medicine,
  title,
  description,
  initialValues,
  categories,
  initialDiseases,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: MedicineEditorFormProps) {
  const { t } = useTranslation('health')
  const schema = useMemo(
    () =>
      createMedicineFormSchema({
        nameRequired: t('medicineEditor.validation.nameRequired'),
        nameTooLong: t('medicineEditor.validation.nameTooLong'),
        categoryRequired: t('medicineEditor.validation.categoryRequired'),
        posologyTooLong: t('medicineEditor.validation.posologyTooLong'),
        notesTooLong: t('medicineEditor.validation.notesTooLong'),
      }),
    [t],
  )
  const form = useForm<MedicineFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, setValue } = form
  const { errors, isDirty } = formState
  const visibility = useWatch({ control, name: 'visibility' })
  const itemId = useWatch({ control, name: 'inventoryItemId' })
  const itemName = useWatch({ control, name: 'inventoryItemName' })

  const originalIds = useMemo(
    () => new Set(initialDiseases.map((disease) => disease.id)),
    [initialDiseases],
  )
  const [selected, setSelected] = useState<Map<number, DiseaseSummary>>(
    () => new Map(initialDiseases.map((disease) => [disease.id, disease])),
  )
  const [selectingDiseases, setSelectingDiseases] = useState(false)
  const [selectingItem, setSelectingItem] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdMedicine, setCreatedMedicine] = useState<Medicine | null>(null)
  const createdRef = useRef<Medicine | null>(null)

  const associationsDirty = useMemo(() => {
    if (selected.size !== originalIds.size) return true
    for (const id of selected.keys()) if (!originalIds.has(id)) return true
    return false
  }, [selected, originalIds])
  const attachmentsDirty = stagedFiles.length > 0
  const dirty = isDirty || associationsDirty || attachmentsDirty

  const mutation = useMutation({
    mutationFn: async (request: MedicineRequest) => {
      const existingId =
        mode === 'edit' ? (medicineId as number) : createdRef.current?.id
      const saved =
        existingId != null
          ? await healthApi.updateMedicine(existingId, request)
          : await healthApi.createMedicine(request)
      if (mode === 'create') createdRef.current = saved
      const stagedIds = new Set(selected.keys())
      const operations = [
        ...[...stagedIds]
          .filter((id) => !originalIds.has(id))
          .map((id) => healthApi.addMedicineDisease(saved.id, id)),
        ...[...originalIds]
          .filter((id) => !stagedIds.has(id))
          .map((id) => healthApi.removeMedicineDisease(saved.id, id)),
      ]
      const results = await Promise.allSettled(operations)
      const failure = results.find((result) => result.status === 'rejected')
      if (failure?.status === 'rejected') throw failure.reason
      return saved
    },
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedMedicine(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapMedicineError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => healthApi.deleteMedicine(medicineId as number),
    onSuccess: () => {
      if (medicine != null) onDeleted(medicine)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapMedicineError(error, t))
    },
  })

  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    void handleSubmit((values) => {
      setServerError(null)
      mutation.mutate(toMedicineRequest(values))
    })(event)
  }

  const requestClose = () => {
    if (dirty && !mutation.isSuccess) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const toggleDisease = (disease: DiseaseSummary, isSelected: boolean) => {
    setSelected((current) => {
      const next = new Map(current)
      if (isSelected) next.set(disease.id, disease)
      else next.delete(disease.id)
      return next
    })
  }

  const removeDisease = (diseaseId: number) => {
    setSelected((current) => {
      const next = new Map(current)
      next.delete(diseaseId)
      return next
    })
  }

  const finishAttachmentUpload = () => {
    if (createdMedicine != null) onSaved(createdMedicine, 'create')
  }

  const selectedIds = useMemo(
    () => new Set([...selected.keys()].map(String)),
    [selected],
  )

  if (createdMedicine != null) {
    return (
      <Dialog
        scrollable
        width={760}
        title={t('medicineEditor.attachments.uploadTitle')}
        description={t('medicineEditor.attachments.uploadDescription', {
          name: createdMedicine.name,
        })}
        onClose={finishAttachmentUpload}
        closeLabel={t('medicineEditor.attachments.close')}
        footer={
          <Button onClick={finishAttachmentUpload}>
            {t('medicineEditor.attachments.done')}
          </Button>
        }
      >
        <section className="seg-health-editor__section">
          <h3>{t('medicineEditor.sections.attachments')}</h3>
          <MedicineAttachments
            medicineId={createdMedicine.id}
            autoUpload={stagedFiles}
          />
        </section>
      </Dialog>
    )
  }

  const submitting = mutation.isPending
  const selectedList = [...selected.values()]
  const itemReference: EntityReference | null =
    itemId == null
      ? null
      : inventoryItemReference({
          name: itemName || t('medicineEditor.itemLink.unavailable'),
        })

  return (
    <>
      <Dialog
        scrollable
        width={760}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('medicineEditor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-health-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('medicineEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('medicineEditor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-health-medicine-form" disabled={submitting}>
              {mode === 'create'
                ? submitting
                  ? t('medicineEditor.actions.creating')
                  : t('medicineEditor.actions.create')
                : submitting
                  ? t('medicineEditor.actions.saving')
                  : t('medicineEditor.actions.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-health-medicine-form"
          className="seg-health-editor"
          onSubmit={submit}
          noValidate
        >
          {serverError != null && (
            <p className="seg-health-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-health-editor__section">
            <h3>{t('medicineEditor.sections.identity')}</h3>
            <Input
              label={t('medicineEditor.fields.name')}
              placeholder={t('medicineEditor.fields.namePlaceholder')}
              error={errors.name?.message}
              {...register('name')}
            />
            <div className="seg-health-editor__grid">
              <label className="seg-health-editor__field">
                <span>{t('medicineEditor.fields.category')}</span>
                <Select
                  {...register('categoryId')}
                  options={categories.map((category) => ({
                    value: String(category.id),
                    label: category.name,
                  }))}
                />
                {errors.categoryId != null && (
                  <span className="seg-health-editor__hint seg-health-editor__hint--error">
                    {errors.categoryId.message}
                  </span>
                )}
              </label>
              <label className="seg-health-editor__check">
                <input type="checkbox" {...register('requiresPrescription')} />
                <span>{t('medicineEditor.fields.requiresPrescription')}</span>
              </label>
            </div>
            <label className="seg-health-editor__field">
              <span>{t('medicineEditor.fields.posology')}</span>
              <textarea
                className="seg-health-editor__textarea"
                rows={3}
                placeholder={t('medicineEditor.fields.posologyPlaceholder')}
                {...register('posology')}
              />
              <span className="seg-health-editor__hint">
                {errors.posology?.message ?? t('medicineEditor.hints.posology')}
              </span>
            </label>
          </section>

          <section className="seg-health-editor__section">
            <h3>{t('medicineEditor.sections.inventory')}</h3>
            <EntityReferenceField
              value={itemReference}
              icon={<Package size={18} />}
              placeholder={t('medicineEditor.itemLink.empty')}
              helperText={t('medicineEditor.itemLink.helper')}
              browseLabel={t('medicineEditor.itemLink.select')}
              changeLabel={t('medicineEditor.itemLink.change')}
              clearLabel={t('medicineEditor.itemLink.clear')}
              onBrowse={() => setSelectingItem(true)}
              onClear={() => {
                setValue('inventoryItemId', null, { shouldDirty: true })
                setValue('inventoryItemName', '', { shouldDirty: true })
              }}
            />
          </section>

          <section className="seg-health-editor__section">
            <div className="seg-health-editor__section-head">
              <h3>{t('medicineEditor.sections.diseases')}</h3>
              <Button
                type="button"
                variant="outline"
                size="sm"
                iconLeft={<Plus size={14} />}
                onClick={() => setSelectingDiseases(true)}
              >
                {t('medicineEditor.diseases.add')}
              </Button>
            </div>
            {selectedList.length === 0 ? (
              <p className="seg-health-editor__empty">
                {t('medicineEditor.diseases.empty')}
              </p>
            ) : (
              <ul className="seg-health-chips">
                {selectedList.map((disease) => (
                  <li key={disease.id} className="seg-health-chip">
                    <Pill size={14} aria-hidden="true" />
                    <span>{disease.name}</span>
                    <button
                      type="button"
                      onClick={() => removeDisease(disease.id)}
                      aria-label={t('medicineEditor.diseases.remove', {
                        name: disease.name,
                      })}
                    >
                      <X size={13} aria-hidden="true" />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="seg-health-editor__section">
            <h3>{t('medicineEditor.sections.notes')}</h3>
            <label className="seg-health-editor__field">
              <span className="seg-health-editor__sr">
                {t('medicineEditor.fields.notes')}
              </span>
              <textarea
                className="seg-health-editor__textarea"
                rows={3}
                placeholder={t('medicineEditor.fields.notesPlaceholder')}
                {...register('notes')}
              />
              <span className="seg-health-editor__hint">
                {errors.notes?.message ?? t('medicineEditor.hints.notes')}
              </span>
            </label>
          </section>

          <section className="seg-health-editor__section">
            <h3>{t('medicineEditor.sections.attachments')}</h3>
            <p className="seg-health-editor__hint">
              {t('medicineEditor.attachments.hint')}
            </p>
            {mode === 'edit' && medicineId != null ? (
              <MedicineAttachments medicineId={medicineId} />
            ) : (
              <StagedAttachments files={stagedFiles} onChange={setStagedFiles} />
            )}
          </section>

          <section className="seg-health-editor__section">
            <span
              id="seg-health-medicine-visibility"
              className="seg-health-editor__label"
            >
              {t('medicineEditor.fields.visibility')}
            </span>
            <SegmentedControl
              aria-labelledby="seg-health-medicine-visibility"
              value={visibility}
              disabled={!canChangeVisibility}
              onChange={(event) =>
                setValue('visibility', event.target.value as HealthVisibility, {
                  shouldDirty: true,
                })
              }
              options={visibilities.map((value) => ({
                value,
                label: t(`visibility.${value}`),
                icon: visibilityMeta[value].icon,
                tone: visibilityMeta[value].tone,
              }))}
            />
            <span className="seg-health-editor__hint">
              {canChangeVisibility
                ? t('medicineEditor.hints.visibility')
                : t('medicineEditor.hints.visibilityLocked')}
            </span>
          </section>
        </form>
      </Dialog>

      {confirmingDelete && (
        <Dialog
          width={460}
          title={t('medicineEditor.delete.title')}
          description={t('medicineEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('medicineEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                disabled={deleteMutation.isPending}
                onClick={() => setConfirmingDelete(false)}
              >
                {t('medicineEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                disabled={deleteMutation.isPending}
                onClick={() => deleteMutation.mutate()}
              >
                {deleteMutation.isPending
                  ? t('medicineEditor.delete.deleting')
                  : t('medicineEditor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}

      {confirmingClose && (
        <Dialog
          width={460}
          title={t('medicineEditor.unsaved.title')}
          description={t('medicineEditor.unsaved.description')}
          onClose={() => setConfirmingClose(false)}
          closeLabel={t('medicineEditor.unsaved.stay')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingClose(false)}>
                {t('medicineEditor.unsaved.stay')}
              </Button>
              <Button variant="danger" onClick={onClose}>
                {t('medicineEditor.unsaved.leave')}
              </Button>
            </>
          }
        />
      )}

      {selectingDiseases && (
        <DiseaseEntitySelector
          medicineVisibility={visibility}
          selectedIds={selectedIds}
          onToggle={toggleDisease}
          onClose={() => setSelectingDiseases(false)}
        />
      )}

      {selectingItem && (
        <InventoryItemEntitySelector
          currentItemId={itemId}
          forcedVisibility={visibility === 'Public' ? 'Public' : null}
          eyebrow={t('medicineEditor.itemLink.selectorEyebrow')}
          description={t('medicineEditor.itemLink.selectorDescription')}
          onClose={() => setSelectingItem(false)}
          onSelect={(item: InventoryItemSummary) => {
            setValue('inventoryItemId', item.id, {
              shouldDirty: true,
              shouldValidate: true,
            })
            setValue('inventoryItemName', item.name, { shouldDirty: true })
            setSelectingItem(false)
          }}
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
  const { t } = useTranslation('health')
  const input = useRef<HTMLInputElement>(null)
  const removeAt = (index: number) =>
    onChange(files.filter((_, position) => position !== index))

  return (
    <div className="seg-health-attach">
      <div className="seg-health-attach__head">
        <Button
          variant="outline"
          size="sm"
          iconLeft={<Plus size={15} />}
          onClick={() => input.current?.click()}
        >
          {t('medicineEditor.attachments.add')}
        </Button>
        <input
          ref={input}
          type="file"
          multiple
          accept={attachmentAccept}
          className="seg-health-attach__input"
          onChange={(event) => {
            const next =
              event.target.files == null ? [] : Array.from(event.target.files)
            onChange([...files, ...next])
            event.target.value = ''
          }}
          aria-label={t('medicineEditor.attachments.add')}
        />
      </div>
      {files.length === 0 ? (
        <p className="seg-health-attach__empty">
          {t('medicineEditor.attachments.empty')}
        </p>
      ) : (
        <>
          <p className="seg-health-editor__hint">
            {t('medicineEditor.attachments.stagedHint')}
          </p>
          <ul className="seg-health-attach__list">
            {files.map((file, index) => {
              const rejection = rejectionFor(file)
              return (
                <li
                  key={`${file.name}-${index}`}
                  className={
                    'seg-health-attach__item' +
                    (rejection != null ? ' seg-health-attach__item--error' : '')
                  }
                >
                  <FileText size={18} aria-hidden="true" />
                  <span className="seg-health-attach__meta">
                    <span className="seg-health-attach__name">{file.name}</span>
                    <span className="seg-health-attach__size">
                      {rejection === 'tooLarge'
                        ? t('medicineEditor.attachments.errors.tooLarge')
                        : rejection === 'type'
                          ? t('medicineEditor.attachments.errors.type')
                          : formatFileSize(file.size)}
                    </span>
                  </span>
                  <button
                    type="button"
                    className="seg-health-attach__action"
                    onClick={() => removeAt(index)}
                    aria-label={t('medicineEditor.attachments.remove')}
                  >
                    <Trash2 size={16} aria-hidden="true" />
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

function mapMedicineError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'health.medicine.validation':
        return t('medicineEditor.errors.validation')
      case 'health.medicine.visibility_forbidden':
      case 'health.association.publish_blocked':
        return t('medicineEditor.errors.visibilityForbidden')
      case 'health.medicine.item_not_accessible':
        return t('medicineEditor.errors.itemNotAccessible')
      case 'health.medicine.item_visibility_forbidden':
        return t('medicineEditor.errors.itemVisibilityForbidden')
      case 'health.association.not_accessible':
      case 'health.association.visibility_forbidden':
        return t('medicineEditor.errors.associationForbidden')
      case 'health.medicine_category.not_found':
        return t('medicineEditor.errors.unknownReference')
    }
    if (error.kind === 'not-found') return t('medicineEditor.notFound')
    if (error.kind === 'validation') return t('medicineEditor.errors.conflict')
  }
  return t('medicineEditor.errors.generic')
}
