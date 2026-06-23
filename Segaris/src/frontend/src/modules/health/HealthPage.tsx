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
  HeartPulse,
  Lock,
  Pill,
  Plus,
  Search,
  Trash2,
  X,
} from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  healthApi,
  healthPageSizes,
  type Disease,
  type DiseaseCategory,
  type DiseaseRequest,
  type DiseaseSortField,
  type DiseaseSummary,
  type HealthPageSize,
  type HealthTab,
  type HealthVisibility,
  type MedicineSummary,
} from '@/app/api/health'
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
  Tabs,
  Toast,
  type BadgeTone,
  type SegmentTone,
} from '@/components/ui'

import { MedicineEntitySelector } from './MedicineEntitySelector'
import { healthKeys, useDiseaseCategories } from './queries'
import {
  activeDiseaseFilterCount,
  useHealthState,
  type DiseaseFilterPatch,
  type DiseaseListState,
} from './healthState'
import {
  buildDefaults,
  createDiseaseFormSchema,
  fromDisease,
  toRequest,
  type DiseaseFormValues,
} from './diseaseForm'

import './HealthPage.css'

const visibilities: HealthVisibility[] = ['Public', 'Private']
const diseaseSortFields: DiseaseSortField[] = ['name', 'category']

const visibilityMeta: Record<HealthVisibility, { icon: ReactNode; tone: SegmentTone }> =
  {
    Public: { icon: <Globe size={15} />, tone: 'accent' },
    Private: { icon: <Lock size={15} />, tone: 'neutral' },
  }
const visibilityBadgeTone: Record<HealthVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

type ToastKind = 'created' | 'updated' | 'deleted'
interface ToastState {
  kind: ToastKind
  name: string
}

export function HealthPage() {
  const { t } = useTranslation('health')
  const { session } = useSession()
  const currentUserId = session?.userId ?? null
  const health = useHealthState(currentUserId)
  const { state, setTab } = health

  return (
    <main className="seg-health armali-aurora">
      <section className="seg-health__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <Tabs
        variant="line"
        aria-label={t('tabs.label')}
        value={state.tab}
        onChange={(next) => setTab(next as HealthTab)}
        tabs={[
          {
            value: 'diseases',
            label: t('tabs.diseases'),
            icon: <HeartPulse size={16} aria-hidden="true" />,
          },
          {
            value: 'medicines',
            label: t('tabs.medicines'),
            icon: <Pill size={16} aria-hidden="true" />,
          },
        ]}
      />

      {state.tab === 'diseases' ? (
        <DiseasesTab health={health} currentUserId={currentUserId} />
      ) : (
        <MedicinesPlaceholder />
      )}
    </main>
  )
}

function MedicinesPlaceholder() {
  const { t } = useTranslation('health')
  return (
    <section className="seg-health__placeholder">
      <span className="seg-health__placeholder-icon">
        <Pill size={28} aria-hidden="true" />
      </span>
      <h2>{t('medicines.placeholderTitle')}</h2>
      <p>{t('medicines.placeholderBody')}</p>
    </section>
  )
}

interface DiseasesTabProps {
  health: ReturnType<typeof useHealthState>
  currentUserId: number | null
}

function DiseasesTab({ health, currentUserId }: DiseasesTabProps) {
  const { t } = useTranslation('health')
  const queryClient = useQueryClient()
  const [toast, setToast] = useState<ToastState | null>(null)

  const {
    state,
    dialog,
    diseaseListQuery,
    setDiseaseFilters,
    clearDiseaseFilters,
    setDiseaseSort,
    setDiseasePage,
    setDiseasePageSize,
    openCreateDisease,
    openEditDisease,
    closeDialog,
  } = health

  const categories = useDiseaseCategories()
  const diseasesQuery = useQuery({
    queryKey: healthKeys.diseaseList(diseaseListQuery),
    queryFn: ({ signal }) => healthApi.listDiseases(diseaseListQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = diseasesQuery.data
  const diseases = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.diseases.pageSize))
  const hasFilters = activeDiseaseFilterCount(state.diseases) > 0

  const invalidate = (diseaseId?: number) => {
    void queryClient.invalidateQueries({ queryKey: healthKeys.diseases() })
    void queryClient.invalidateQueries({ queryKey: healthKeys.medicines() })
    if (diseaseId != null) {
      void queryClient.invalidateQueries({ queryKey: healthKeys.disease(diseaseId) })
      void queryClient.invalidateQueries({
        queryKey: healthKeys.diseaseMedicines(diseaseId),
      })
    }
  }

  const handleSaved = (disease: Disease, mode: 'create' | 'edit') => {
    queryClient.setQueryData(healthKeys.disease(disease.id), disease)
    invalidate(disease.id)
    setToast({ kind: mode === 'create' ? 'created' : 'updated', name: disease.name })
    closeDialog()
  }

  const handleDeleted = (disease: Disease) => {
    invalidate(disease.id)
    setToast({ kind: 'deleted', name: disease.name })
    closeDialog()
  }

  if (diseasesQuery.isError) {
    const error = diseasesQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void diseasesQuery.refetch()} />
    }
  }

  return (
    <section className="seg-health__tab">
      <div className="seg-health__toolbar">
        <Badge tone="neutral">{t('diseases.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDisease}>
          {t('diseases.newDisease')}
        </Button>
      </div>

      <DiseaseFilters
        state={state.diseases}
        categories={categories.data ?? []}
        onChange={setDiseaseFilters}
        onClear={clearDiseaseFilters}
      />

      {diseasesQuery.isPending ? (
        <div className="seg-health__loading">
          <Spinner label={t('diseases.states.loading')} />
        </div>
      ) : diseasesQuery.isError ? (
        <p className="seg-health__error" role="alert">
          {t('diseases.states.loadError')}
        </p>
      ) : diseases.length === 0 ? (
        <p className="seg-health__empty">
          {hasFilters ? t('diseases.states.emptyFiltered') : t('diseases.states.empty')}
        </p>
      ) : (
        <DiseaseTable
          diseases={diseases}
          state={state.diseases}
          busy={diseasesQuery.isFetching && !diseasesQuery.isPending}
          onSort={setDiseaseSort}
          onOpen={openEditDisease}
        />
      )}

      <Pager
        page={state.diseases.page}
        pageSize={state.diseases.pageSize}
        totalPages={totalPages}
        fetching={diseasesQuery.isFetching}
        onPage={setDiseasePage}
        onPageSize={setDiseasePageSize}
      />

      {dialog.mode === 'createDisease' || dialog.mode === 'editDisease' ? (
        <DiseaseDialog
          mode={dialog.mode === 'createDisease' ? 'create' : 'edit'}
          diseaseId={dialog.mode === 'editDisease' ? dialog.diseaseId : undefined}
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
            closeLabel={t('diseaseEditor.actions.cancel')}
            onClose={() => setToast(null)}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </section>
  )
}

interface DiseaseFiltersProps {
  state: DiseaseListState
  categories: DiseaseCategory[]
  onChange: (patch: DiseaseFilterPatch) => void
  onClear: () => void
}

function DiseaseFilters({ state, categories, onChange, onClear }: DiseaseFiltersProps) {
  const { t } = useTranslation('health')
  const count = activeDiseaseFilterCount(state)
  return (
    <section
      className="seg-health__filters"
      aria-label={t('diseases.filters.active', { count })}
    >
      <Input
        className="seg-health__search"
        label={t('diseases.filters.search')}
        placeholder={t('diseases.filters.searchPlaceholder')}
        iconLeft={<Search size={16} />}
        value={state.search}
        onChange={(event) => onChange({ search: event.target.value })}
      />
      <FilterSelect
        label={t('diseases.filters.category')}
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
        label={t('diseases.filters.visibility')}
        value={state.visibility}
        allLabel={t('diseases.filters.allVisibilities')}
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
        <span>{t('diseases.filters.mine')}</span>
      </label>
      {count > 0 && (
        <Button variant="ghost" onClick={onClear}>
          {t('diseases.filters.clear')}
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
          { value: '', label: allLabel ?? t('diseases.filters.all') },
          ...options,
        ]}
      />
    </label>
  )
}

interface DiseaseTableProps {
  diseases: DiseaseSummary[]
  state: DiseaseListState
  busy: boolean
  onSort: (field: DiseaseSortField) => void
  onOpen: (diseaseId: number) => void
}

function DiseaseTable({ diseases, state, busy, onSort, onOpen }: DiseaseTableProps) {
  const { t } = useTranslation('health')
  return (
    <div className="seg-health-table-wrap" aria-busy={busy}>
      <table className="seg-health-table">
        <thead>
          <tr>
            {diseaseSortFields.map((field) => {
              const active = state.sort === field
              return (
                <th
                  key={field}
                  aria-sort={
                    active
                      ? state.sortDirection === 'asc'
                        ? 'ascending'
                        : 'descending'
                      : 'none'
                  }
                >
                  <button
                    type="button"
                    className={
                      'seg-health-table__sort' +
                      (active ? ' seg-health-table__sort--active' : '')
                    }
                    onClick={() => onSort(field)}
                  >
                    {t(`diseases.columns.${field}`)}
                    {active &&
                      (state.sortDirection === 'asc' ? (
                        <ArrowDownAZ size={14} aria-hidden="true" />
                      ) : (
                        <ArrowUpZA size={14} aria-hidden="true" />
                      ))}
                  </button>
                </th>
              )
            })}
            <th className="seg-health-table__num">{t('diseases.columns.medicines')}</th>
            <th>{t('diseases.columns.visibility')}</th>
          </tr>
        </thead>
        <tbody>
          {diseases.map((disease) => (
            <tr key={disease.id}>
              <td>
                <button
                  type="button"
                  className="seg-health-table__name"
                  onClick={() => onOpen(disease.id)}
                  aria-label={t('diseases.open', { name: disease.name })}
                >
                  {disease.name}
                </button>
              </td>
              <td>{disease.categoryName}</td>
              <td className="seg-health-table__num">
                {t('diseases.associatedMedicines', {
                  count: disease.associatedMedicineCount,
                })}
              </td>
              <td>
                <Badge tone={visibilityBadgeTone[disease.visibility]}>
                  {t(`visibility.${disease.visibility}`)}
                </Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
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
    <nav className="seg-health__pager" aria-label={t('diseases.pagination.label')}>
      <label className="seg-health__rows">
        <span>{t('diseases.pagination.rowsPerPage')}</span>
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
          {t('diseases.pagination.previous')}
        </Button>
        <span className="seg-health__page" aria-live="polite">
          {t('diseases.pagination.status', { page, pages: totalPages })}
        </span>
        <Button
          variant="ghost"
          size="sm"
          iconRight={<ChevronRight size={16} />}
          disabled={page >= totalPages || fetching}
          onClick={() => onPage(Math.min(totalPages, page + 1))}
        >
          {t('diseases.pagination.next')}
        </Button>
      </div>
    </nav>
  )
}

interface DiseaseDialogProps {
  mode: 'create' | 'edit'
  diseaseId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (disease: Disease, mode: 'create' | 'edit') => void
  onDeleted: (disease: Disease) => void
}

function DiseaseDialog({
  mode,
  diseaseId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: DiseaseDialogProps) {
  const { t } = useTranslation('health')
  const categories = useDiseaseCategories()
  const diseaseQuery = useQuery({
    queryKey: healthKeys.disease(diseaseId as number),
    queryFn: ({ signal }) => healthApi.getDisease(diseaseId as number, signal),
    enabled: mode === 'edit' && diseaseId != null,
  })
  const medicinesQuery = useQuery({
    queryKey: healthKeys.diseaseMedicines(diseaseId as number),
    queryFn: ({ signal }) =>
      healthApi.listDiseaseMedicines(diseaseId as number, signal),
    enabled: mode === 'edit' && diseaseId != null,
  })

  const title =
    mode === 'create' ? t('diseaseEditor.createTitle') : t('diseaseEditor.editTitle')
  const description =
    mode === 'create'
      ? t('diseaseEditor.createDescription')
      : t('diseaseEditor.editDescription')

  const loading =
    categories.data == null ||
    (mode === 'edit' && (diseaseQuery.isPending || medicinesQuery.isPending))

  if (loading) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('diseaseEditor.actions.cancel')}
      >
        <div className="seg-health-editor__status">
          <Spinner />
          <span>{t('diseaseEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && diseaseQuery.isError) {
    const notFound =
      isApiError(diseaseQuery.error) && diseaseQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={560}
        title={title}
        onClose={onClose}
        closeLabel={t('diseaseEditor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('diseaseEditor.actions.cancel')}</Button>}
      >
        <p className="seg-health-editor__error" role="alert">
          {notFound ? t('diseaseEditor.notFound') : t('diseaseEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const disease = mode === 'edit' ? (diseaseQuery.data as Disease) : undefined
  const initialValues =
    disease != null
      ? fromDisease(disease)
      : buildDefaults(firstCatalogId(categories.data))
  const canChangeVisibility =
    disease == null || (currentUserId != null && disease.creatorId === currentUserId)

  return (
    <DiseaseEditorForm
      mode={mode}
      diseaseId={diseaseId}
      disease={disease}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data}
      initialMedicines={mode === 'edit' ? (medicinesQuery.data ?? []) : []}
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

interface DiseaseEditorFormProps {
  mode: 'create' | 'edit'
  diseaseId?: number
  disease?: Disease
  title: string
  description: string
  initialValues: DiseaseFormValues
  categories: DiseaseCategory[]
  initialMedicines: MedicineSummary[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (disease: Disease, mode: 'create' | 'edit') => void
  onDeleted: (disease: Disease) => void
}

function DiseaseEditorForm({
  mode,
  diseaseId,
  disease,
  title,
  description,
  initialValues,
  categories,
  initialMedicines,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: DiseaseEditorFormProps) {
  const { t } = useTranslation('health')
  const schema = useMemo(
    () =>
      createDiseaseFormSchema({
        nameRequired: t('diseaseEditor.validation.nameRequired'),
        nameTooLong: t('diseaseEditor.validation.nameTooLong'),
        categoryRequired: t('diseaseEditor.validation.categoryRequired'),
        durationRange: t('diseaseEditor.validation.durationRange'),
        symptomsTooLong: t('diseaseEditor.validation.symptomsTooLong'),
        notesTooLong: t('diseaseEditor.validation.notesTooLong'),
      }),
    [t],
  )
  const form = useForm<DiseaseFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, setValue } = form
  const { errors, isDirty } = formState
  const visibility = useWatch({ control, name: 'visibility' })

  const originalIds = useMemo(
    () => new Set(initialMedicines.map((medicine) => medicine.id)),
    [initialMedicines],
  )
  const [selected, setSelected] = useState<Map<number, MedicineSummary>>(
    () => new Map(initialMedicines.map((medicine) => [medicine.id, medicine])),
  )
  const [selecting, setSelecting] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  // Remembers the disease created in this dialog so a retry after a failed
  // association update updates that record instead of creating a duplicate.
  const createdRef = useRef<Disease | null>(null)

  const associationsDirty = useMemo(() => {
    if (selected.size !== originalIds.size) return true
    for (const id of selected.keys()) if (!originalIds.has(id)) return true
    return false
  }, [selected, originalIds])

  const mutation = useMutation({
    mutationFn: async (request: DiseaseRequest) => {
      const existingId =
        mode === 'edit' ? (diseaseId as number) : createdRef.current?.id
      const saved =
        existingId != null
          ? await healthApi.updateDisease(existingId, request)
          : await healthApi.createDisease(request)
      if (mode === 'create') createdRef.current = saved
      const stagedIds = new Set(selected.keys())
      const operations = [
        ...[...stagedIds]
          .filter((id) => !originalIds.has(id))
          .map((id) => healthApi.addDiseaseMedicine(saved.id, id)),
        ...[...originalIds]
          .filter((id) => !stagedIds.has(id))
          .map((id) => healthApi.removeDiseaseMedicine(saved.id, id)),
      ]
      const results = await Promise.allSettled(operations)
      const failure = results.find((result) => result.status === 'rejected')
      if (failure?.status === 'rejected') throw failure.reason
      return saved
    },
    onSuccess: (saved) => onSaved(saved, mode),
    onError: (error) => setServerError(mapDiseaseError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => healthApi.deleteDisease(diseaseId as number),
    onSuccess: () => {
      if (disease != null) onDeleted(disease)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapDiseaseError(error, t))
    },
  })

  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    void handleSubmit((values) => {
      setServerError(null)
      mutation.mutate(toRequest(values))
    })(event)
  }

  const dirty = isDirty || associationsDirty
  const requestClose = () => {
    if (dirty && !mutation.isSuccess) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const toggleMedicine = (medicine: MedicineSummary, isSelected: boolean) => {
    setSelected((current) => {
      const next = new Map(current)
      if (isSelected) next.set(medicine.id, medicine)
      else next.delete(medicine.id)
      return next
    })
  }

  const removeMedicine = (medicineId: number) => {
    setSelected((current) => {
      const next = new Map(current)
      next.delete(medicineId)
      return next
    })
  }

  const submitting = mutation.isPending
  const selectedIds = useMemo(
    () => new Set([...selected.keys()].map(String)),
    [selected],
  )
  const selectedList = [...selected.values()]

  return (
    <>
      <Dialog
        scrollable
        width={760}
        title={title}
        description={description}
        onClose={requestClose}
        closeLabel={t('diseaseEditor.actions.cancel')}
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
                {t('diseaseEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('diseaseEditor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-health-disease-form" disabled={submitting}>
              {mode === 'create'
                ? submitting
                  ? t('diseaseEditor.actions.creating')
                  : t('diseaseEditor.actions.create')
                : submitting
                  ? t('diseaseEditor.actions.saving')
                  : t('diseaseEditor.actions.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-health-disease-form"
          className="seg-health-editor"
          onSubmit={submit}
        >
          {serverError != null && (
            <p className="seg-health-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-health-editor__section">
            <h3>{t('diseaseEditor.sections.identity')}</h3>
            <Input
              label={t('diseaseEditor.fields.name')}
              placeholder={t('diseaseEditor.fields.namePlaceholder')}
              error={errors.name != null ? errors.name.message : undefined}
              {...register('name')}
            />
            <div className="seg-health-editor__grid">
              <label className="seg-health-editor__field">
                <span>{t('diseaseEditor.fields.category')}</span>
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
              <Input
                type="number"
                inputMode="numeric"
                label={t('diseaseEditor.fields.averageDurationDays')}
                placeholder={t('diseaseEditor.fields.averageDurationPlaceholder')}
                hint={t('diseaseEditor.hints.averageDuration')}
                error={
                  errors.averageDurationDays != null
                    ? errors.averageDurationDays.message
                    : undefined
                }
                {...register('averageDurationDays')}
              />
            </div>
            <label className="seg-health-editor__field">
              <span>{t('diseaseEditor.fields.symptoms')}</span>
              <textarea
                className="seg-health-editor__textarea"
                rows={3}
                placeholder={t('diseaseEditor.fields.symptomsPlaceholder')}
                {...register('symptoms')}
              />
              <span className="seg-health-editor__hint">
                {errors.symptoms != null
                  ? errors.symptoms.message
                  : t('diseaseEditor.hints.symptoms')}
              </span>
            </label>
          </section>

          <section className="seg-health-editor__section">
            <div className="seg-health-editor__section-head">
              <h3>{t('diseaseEditor.sections.medicines')}</h3>
              <Button
                type="button"
                variant="outline"
                size="sm"
                iconLeft={<Plus size={14} />}
                onClick={() => setSelecting(true)}
              >
                {t('diseaseEditor.medicines.add')}
              </Button>
            </div>
            {selectedList.length === 0 ? (
              <p className="seg-health-editor__empty">
                {t('diseaseEditor.medicines.empty')}
              </p>
            ) : (
              <ul className="seg-health-chips">
                {selectedList.map((medicine) => (
                  <li key={medicine.id} className="seg-health-chip">
                    <Pill size={14} aria-hidden="true" />
                    <span>{medicine.name}</span>
                    <button
                      type="button"
                      onClick={() => removeMedicine(medicine.id)}
                      aria-label={t('diseaseEditor.medicines.remove', {
                        name: medicine.name,
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
            <h3>{t('diseaseEditor.sections.notes')}</h3>
            <label className="seg-health-editor__field">
              <span className="seg-health-editor__sr">
                {t('diseaseEditor.fields.notes')}
              </span>
              <textarea
                className="seg-health-editor__textarea"
                rows={3}
                placeholder={t('diseaseEditor.fields.notesPlaceholder')}
                {...register('notes')}
              />
              <span className="seg-health-editor__hint">
                {errors.notes != null
                  ? errors.notes.message
                  : t('diseaseEditor.hints.notes')}
              </span>
            </label>
          </section>

          <section className="seg-health-editor__section">
            <span id="seg-health-visibility-label" className="seg-health-editor__label">
              {t('diseaseEditor.fields.visibility')}
            </span>
            <SegmentedControl
              aria-labelledby="seg-health-visibility-label"
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
                ? t('diseaseEditor.hints.visibility')
                : t('diseaseEditor.hints.visibilityLocked')}
            </span>
          </section>
        </form>
      </Dialog>

      {confirmingDelete && (
        <Dialog
          width={460}
          title={t('diseaseEditor.delete.title')}
          description={t('diseaseEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('diseaseEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                disabled={deleteMutation.isPending}
                onClick={() => setConfirmingDelete(false)}
              >
                {t('diseaseEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                disabled={deleteMutation.isPending}
                onClick={() => deleteMutation.mutate()}
              >
                {deleteMutation.isPending
                  ? t('diseaseEditor.delete.deleting')
                  : t('diseaseEditor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}

      {confirmingClose && (
        <Dialog
          width={460}
          title={t('diseaseEditor.unsaved.title')}
          description={t('diseaseEditor.unsaved.description')}
          onClose={() => setConfirmingClose(false)}
          closeLabel={t('diseaseEditor.unsaved.stay')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingClose(false)}>
                {t('diseaseEditor.unsaved.stay')}
              </Button>
              <Button variant="danger" onClick={onClose}>
                {t('diseaseEditor.unsaved.leave')}
              </Button>
            </>
          }
        />
      )}

      {selecting && (
        <MedicineEntitySelector
          diseaseVisibility={visibility}
          selectedIds={selectedIds}
          onToggle={toggleMedicine}
          onClose={() => setSelecting(false)}
        />
      )}
    </>
  )
}

function mapDiseaseError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'health.disease.validation':
        return t('diseaseEditor.errors.validation')
      case 'health.disease.visibility_forbidden':
      case 'health.association.publish_blocked':
        return t('diseaseEditor.errors.visibilityForbidden')
      case 'health.association.not_accessible':
      case 'health.association.visibility_forbidden':
        return t('diseaseEditor.errors.associationForbidden')
      case 'health.disease_category.not_found':
        return t('diseaseEditor.errors.unknownReference')
    }
    if (error.kind === 'not-found') return t('diseaseEditor.notFound')
    if (error.kind === 'validation') return t('diseaseEditor.errors.conflict')
  }
  return t('diseaseEditor.errors.generic')
}
