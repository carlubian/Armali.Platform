import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  ArrowDown,
  ArrowUp,
  FileText,
  Globe,
  Lock,
  Paperclip,
  Plus,
  Trash2,
  TrendingDown,
  TrendingUp,
  X,
} from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import {
  useFieldArray,
  useForm,
  useWatch,
  type Control,
  type UseFormRegister,
} from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  capexApi,
  type CapexCategory,
  type CapexEntry,
  type CapexEntryStatus,
  type CapexMovementType,
  type CapexVisibility,
  type CreateCapexEntryRequest,
} from '@/app/api/capex'
import type { CostCenter, Currency, Supplier } from '@/app/api/configuration'
import { isApiError } from '@/app/api/errors'
import { formatCurrency } from '@/app/i18n/formatters'
import {
  Button,
  Dialog,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  type SegmentTone,
} from '@/components/ui'

import { attachmentAccept, formatFileSize, rejectionFor } from './attachments'
import { EntryAttachments } from './EntryAttachments'
import {
  buildDefaults,
  blankItem,
  computeLineAmount,
  createEntrySchema,
  fromEntry,
  parseAmount,
  toRequest,
  type EntryFormValues,
} from './entryForm'
import {
  useCapexCategories,
  useCostCenters,
  useCurrencies,
  useSuppliers,
} from './queries'

import './EntryDialog.css'

const maxItems = 100

export interface EntryDialogProps {
  /** `create` opens a blank editor; `edit` loads and edits an existing entry. */
  mode: 'create' | 'edit'
  entryId?: number
  /** The signed-in user, used to gate creator-only visibility changes. */
  currentUserId: number | null
  /** Requested close (close button, backdrop, Escape, or Cancel). */
  onClose: () => void
  /** Invoked after a successful create or update with the saved entry. */
  onSaved: (entry: CapexEntry, mode: 'create' | 'edit') => void
  /** Invoked after an entry is irreversibly deleted from the editor. */
  onDeleted: (entry: CapexEntry) => void
}

/**
 * The Capex entry editor dialog. It resolves the seeded catalogs (and, when
 * editing, the entry itself) before mounting the form so React Hook Form starts
 * with correct defaults and dirty tracking. The actual form lives in
 * {@link EntryEditorForm}, which is only rendered once data is ready.
 */
export function EntryDialog({
  mode,
  entryId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: EntryDialogProps) {
  const { t } = useTranslation('capex')

  const categories = useCapexCategories()
  const suppliers = useSuppliers()
  const costCenters = useCostCenters()
  const currencies = useCurrencies()

  const entryQuery = useQuery({
    queryKey: ['capex', 'entry', entryId] as const,
    queryFn: ({ signal }) => capexApi.getEntry(entryId as number, signal),
    enabled: mode === 'edit' && entryId != null,
  })

  const catalogsReady =
    categories.data != null &&
    suppliers.data != null &&
    costCenters.data != null &&
    currencies.data != null

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  // Loading and error states share the dialog chrome so focus stays trapped.
  if (!catalogsReady || (mode === 'edit' && entryQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-capex-editor__status">
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
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-capex-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const entry = mode === 'edit' ? (entryQuery.data as CapexEntry) : undefined
  const initialValues =
    entry != null
      ? fromEntry(entry)
      : buildDefaults({
          categoryId: defaultCatalogId(categories.data, 'OTHER'),
          currencyId: defaultCatalogId(currencies.data, 'EUR'),
        })

  // Only the creator may change visibility; everyone with access may edit a
  // public entry's other fields.
  const canChangeVisibility =
    entry == null || (currentUserId != null && entry.createdById === currentUserId)

  return (
    <EntryEditorForm
      mode={mode}
      entryId={entryId}
      entry={entry}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data ?? []}
      suppliers={suppliers.data ?? []}
      costCenters={costCenters.data ?? []}
      currencies={currencies.data ?? []}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
    />
  )
}

function defaultCatalogId(
  items: ReadonlyArray<{ id: number; code: string }> | undefined,
  code: string,
): string {
  const match = items?.find((item) => item.code === code)
  return match != null ? String(match.id) : ''
}

interface EntryEditorFormProps {
  mode: 'create' | 'edit'
  entryId?: number
  /** The loaded entry when editing; used for deletion display and visibility. */
  entry?: CapexEntry
  title: string
  description: string
  initialValues: EntryFormValues
  categories: CapexCategory[]
  suppliers: Supplier[]
  costCenters: CostCenter[]
  currencies: Currency[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (entry: CapexEntry, mode: 'create' | 'edit') => void
  onDeleted: (entry: CapexEntry) => void
}

const movementTypes: CapexMovementType[] = ['Income', 'Expense']
const statuses: CapexEntryStatus[] = ['Planning', 'Completed', 'Canceled']
const visibilities: CapexVisibility[] = ['Public', 'Private']

// Icon and active-colour cues so the segmented toggles read at a glance and stay
// consistent with the entries table (income = positive/green).
const movementTypeMeta: Record<
  CapexMovementType,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Income: { icon: <TrendingUp size={15} />, tone: 'success' },
  Expense: { icon: <TrendingDown size={15} />, tone: 'neutral' },
}
const visibilityMeta: Record<CapexVisibility, { icon: ReactNode; tone: SegmentTone }> =
  {
    Public: { icon: <Globe size={15} />, tone: 'accent' },
    Private: { icon: <Lock size={15} />, tone: 'neutral' },
  }

function EntryEditorForm({
  mode,
  entryId,
  entry,
  title,
  description,
  initialValues,
  categories,
  suppliers,
  costCenters,
  currencies,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: EntryEditorFormProps) {
  const { t, i18n } = useTranslation('capex')

  const schema = useMemo(
    () =>
      createEntrySchema({
        titleRequired: t('editor.validation.titleRequired'),
        titleTooLong: t('editor.validation.titleTooLong'),
        dueDateRequired: t('editor.validation.dueDateRequired'),
        categoryRequired: t('editor.validation.categoryRequired'),
        currencyRequired: t('editor.validation.currencyRequired'),
        notesTooLong: t('editor.validation.notesTooLong'),
        descriptionRequired: t('editor.validation.descriptionRequired'),
        descriptionTooLong: t('editor.validation.descriptionTooLong'),
        quantityInvalid: t('editor.validation.quantityInvalid'),
        unitAmountInvalid: t('editor.validation.unitAmountInvalid'),
        itemsBounds: t('editor.validation.itemsBounds'),
      }),
    [t],
  )

  const form = useForm<EntryFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, setValue, getValues } = form
  const items = useFieldArray({ control, name: 'items' })

  // A plain single line uses the simplified experience where the title doubles
  // as the item description. An entry with several lines, a non-unit quantity, or
  // a description that already differs from the title starts itemised so nothing
  // is hidden or overwritten.
  const startsItemized =
    initialValues.items.length > 1 ||
    initialValues.items[0]?.quantity !== '1' ||
    initialValues.items[0]?.description.trim() !== initialValues.title.trim()
  const [itemized, setItemized] = useState(startsItemized)

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  // Files chosen before the entry exists (create mode). They are uploaded after
  // the entry is created in the post-create phase below.
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  // Set once a create succeeds while files are staged: the dialog switches to a
  // dedicated upload phase against the now-existing entry.
  const [createdEntry, setCreatedEntry] = useState<CapexEntry | null>(null)
  // A plain ref flagged from the form's change event. Subscribing to RHF's
  // `formState.isDirty` during render would re-render on the first keystroke and
  // interrupt typing inside the field array, so the discard guard tracks edits
  // here instead.
  const editedRef = useRef(false)

  // Only currency selection (rare) re-renders the form body. Live amounts are
  // watched inside the isolated total components below so typing into item
  // fields never re-renders — and therefore never resets — the inputs.
  const currencyId = useWatch({ control, name: 'currencyId' })
  const currencyCode =
    currencies.find((currency) => String(currency.id) === currencyId)?.code ?? 'EUR'

  const mutation = useMutation({
    mutationFn: (request: CreateCapexEntryRequest) =>
      mode === 'create'
        ? capexApi.createEntry(request)
        : capexApi.updateEntry(entryId as number, request),
    onSuccess: (saved) => {
      // On create with staged files, hold the dialog open to upload them against
      // the new entry; otherwise the save is complete.
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedEntry(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => capexApi.deleteEntry(entryId as number),
    onSuccess: () => {
      if (entry != null) onDeleted(entry)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapServerError(error, t))
    },
  })

  // In the simplified view the title is the single item's description. Syncing it
  // here (on submit, never during typing) keeps editing smooth and satisfies the
  // schema's required-description rule.
  const submit = (event: React.FormEvent<HTMLFormElement>) => {
    if (!itemized) {
      setValue('items.0.description', getValues('title'), { shouldValidate: true })
    }
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

  const addItem = () => {
    if (items.fields.length >= maxItems) return
    if (!itemized) {
      // Carry the title into the first item's description as the starting point;
      // the user may now edit each line independently.
      setValue('items.0.description', getValues('title'))
      setItemized(true)
    }
    items.append(blankItem())
  }

  const collapseToSingle = () => {
    setItemized(false)
  }

  const catalogOptions = (
    rows: ReadonlyArray<{ id: number; name?: string; code?: string }>,
    useCode = false,
  ) =>
    rows.map((row) => ({
      value: String(row.id),
      label: useCode ? (row.code ?? '') : (row.name ?? ''),
    }))

  const noneOption = { value: '', label: t('editor.fields.none') }
  const submitting = mutation.isPending
  const canCollapse = !itemized || items.fields.length === 1

  // Post-create phase: the entry exists, so upload the staged files against it
  // and let per-file failures surface without discarding the created entry.
  if (createdEntry != null) {
    const finish = () => onSaved(createdEntry, 'create')
    return (
      <Dialog
        scrollable
        width={760}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          title: createdEntry.title,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-capex-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <EntryAttachments entryId={createdEntry.id} autoUpload={stagedFiles} />
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
                className="seg-capex-editor__delete"
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
            <Button type="submit" form="seg-capex-entry-form" disabled={submitting}>
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
          id="seg-capex-entry-form"
          className="seg-capex-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-capex-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-capex-editor__section">
            <h3>{t('editor.sections.general')}</h3>
            <Input
              label={t('editor.fields.title')}
              placeholder={t('editor.fields.titlePlaceholder')}
              required
              error={formState.errors.title?.message}
              {...register('title')}
            />
            <div className="seg-capex-editor__grid">
              <ToggleField id="capex-field-type" label={t('editor.fields.type')}>
                <SegmentedControl
                  aria-labelledby="capex-field-type"
                  {...register('movementType')}
                  options={movementTypes.map((value) => ({
                    value,
                    label: t(`entries.type.${value}`),
                    icon: movementTypeMeta[value].icon,
                    tone: movementTypeMeta[value].tone,
                  }))}
                />
              </ToggleField>
              <Field label={t('editor.fields.status')}>
                <Select
                  {...register('status')}
                  options={statuses.map((value) => ({
                    value,
                    label: t(`entries.status.${value}`),
                  }))}
                />
              </Field>
              <Input
                type="date"
                label={t('editor.fields.dueDate')}
                required
                error={formState.errors.dueDate?.message}
                {...register('dueDate')}
              />
              <Field
                label={t('editor.fields.category')}
                error={formState.errors.categoryId?.message}
              >
                <Select
                  {...register('categoryId')}
                  aria-invalid={formState.errors.categoryId != null}
                  options={catalogOptions(categories)}
                />
              </Field>
              <Field label={t('editor.fields.supplier')}>
                <Select
                  {...register('supplierId')}
                  options={[noneOption, ...catalogOptions(suppliers)]}
                />
              </Field>
              <Field label={t('editor.fields.costCenter')}>
                <Select
                  {...register('costCenterId')}
                  options={[noneOption, ...catalogOptions(costCenters)]}
                />
              </Field>
              <Field label={t('editor.fields.currency')}>
                <Select
                  {...register('currencyId')}
                  options={catalogOptions(currencies, true)}
                />
              </Field>
              <ToggleField
                id="capex-field-visibility"
                label={t('editor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="capex-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`entries.visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-capex-editor__section">
            <div className="seg-capex-editor__section-head">
              <h3>{t('editor.sections.items')}</h3>
              {itemized && canCollapse && items.fields.length === 1 && (
                <Button variant="ghost" size="sm" onClick={collapseToSingle}>
                  {t('editor.items.simplify')}
                </Button>
              )}
            </div>

            {itemized ? (
              <ol className="seg-capex-editor__items">
                {items.fields.map((field, index) => (
                  <ItemRow
                    key={field.id}
                    index={index}
                    control={control}
                    register={register}
                    errors={formState.errors}
                    currencyCode={currencyCode}
                    language={i18n.language}
                    canRemove={items.fields.length > 1}
                    canMoveUp={index > 0}
                    canMoveDown={index < items.fields.length - 1}
                    onRemove={() => items.remove(index)}
                    onMoveUp={() => items.move(index, index - 1)}
                    onMoveDown={() => items.move(index, index + 1)}
                  />
                ))}
              </ol>
            ) : (
              <div className="seg-capex-editor__grid">
                <Input
                  label={t('editor.fields.amount')}
                  inputMode="decimal"
                  required
                  error={formState.errors.items?.[0]?.unitAmount?.message}
                  {...register('items.0.unitAmount')}
                />
              </div>
            )}

            <div className="seg-capex-editor__items-foot">
              <Button
                variant="outline"
                size="sm"
                iconLeft={<Plus size={15} />}
                onClick={addItem}
                disabled={items.fields.length >= maxItems}
              >
                {t('editor.items.addItem')}
              </Button>
              {items.fields.length >= maxItems && (
                <span className="seg-capex-editor__hint">
                  {t('editor.items.maxReached')}
                </span>
              )}
              <div className="seg-capex-editor__total">
                <span>{t('editor.total.label')}</span>
                <TotalPreview
                  control={control}
                  currencyCode={currencyCode}
                  language={i18n.language}
                />
              </div>
            </div>
            <p className="seg-capex-editor__hint">{t('editor.total.hint')}</p>
          </section>

          <section className="seg-capex-editor__section">
            <h3>{t('editor.sections.notes')}</h3>
            <label className="seg-capex-editor__notes">
              <span className="seg-capex-editor__notes-label">
                {t('editor.fields.notes')}
              </span>
              <textarea
                className="seg-capex-editor__textarea"
                rows={4}
                placeholder={t('editor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-capex-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-capex-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-capex-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && entryId != null ? (
              <EntryAttachments entryId={entryId} />
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

interface StagedAttachmentsProps {
  files: File[]
  onChange: (files: File[]) => void
}

/**
 * Local file staging for create mode. The entry does not exist yet, so files are
 * held in memory and uploaded after a successful create (see the post-create
 * phase in {@link EntryEditorForm}). Obvious type/size rejections are flagged
 * here so the user can swap a file before the entry is even saved.
 */
function StagedAttachments({ files, onChange }: StagedAttachmentsProps) {
  const { t } = useTranslation('capex')
  const input = useRef<HTMLInputElement>(null)

  const removeAt = (index: number) =>
    onChange(files.filter((_, position) => position !== index))

  return (
    <div className="seg-capex-attach">
      <div className="seg-capex-attach__head">
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
          className="seg-capex-attach__input"
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
        <p className="seg-capex-attach__empty">{t('editor.attachments.empty')}</p>
      ) : (
        <>
          <p className="seg-capex-editor__hint">{t('editor.attachments.stagedHint')}</p>
          <ul className="seg-capex-attach__list">
            {files.map((file, index) => {
              const rejection = rejectionFor(file)
              return (
                <li
                  key={`${file.name}-${index}`}
                  className={
                    'seg-capex-attach__item' +
                    (rejection != null ? ' seg-capex-attach__item--error' : '')
                  }
                >
                  <FileText
                    size={18}
                    aria-hidden="true"
                    className="seg-capex-attach__icon"
                  />
                  <span className="seg-capex-attach__meta">
                    <span className="seg-capex-attach__name">{file.name}</span>
                    <span className="seg-capex-attach__size">
                      {rejection === 'tooLarge'
                        ? t('editor.attachments.errors.tooLarge')
                        : rejection === 'type'
                          ? t('editor.attachments.errors.type')
                          : formatFileSize(file.size)}
                    </span>
                  </span>
                  <button
                    type="button"
                    className="seg-capex-attach__action"
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

interface FieldProps {
  label: string
  hint?: string
  error?: string
  children: React.ReactNode
}

/** A labelled wrapper matching the Input field layout for Select controls. */
function Field({ label, hint, error, children }: FieldProps) {
  const message = error ?? hint
  return (
    <div className="seg-capex-editor__field">
      {/* The message lives outside the label so it does not leak into the
          control's accessible name. */}
      <label className="seg-capex-editor__field-control">
        <span className="seg-capex-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-capex-editor__field-hint' +
            (error != null ? ' seg-capex-editor__field-hint--error' : '')
          }
        >
          {message}
        </span>
      )}
    </div>
  )
}

interface ToggleFieldProps {
  /** Ids the group label so the radiogroup can reference it for its name. */
  id: string
  label: string
  hint?: string
  children: React.ReactNode
}

/**
 * Field wrapper for a {@link SegmentedControl}. Unlike {@link Field} it is not a
 * `<label>` element — wrapping a radio group in a label would steal clicks for
 * the first option — so the label is a plain span linked via `aria-labelledby`.
 */
function ToggleField({ id, label, hint, children }: ToggleFieldProps) {
  return (
    <div className="seg-capex-editor__field">
      <span className="seg-capex-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-capex-editor__field-hint">{hint}</span>}
    </div>
  )
}

interface ItemRowProps {
  index: number
  control: Control<EntryFormValues>
  register: UseFormRegister<EntryFormValues>
  errors: ReturnType<typeof useForm<EntryFormValues>>['formState']['errors']
  currencyCode: string
  language: string
  canRemove: boolean
  canMoveUp: boolean
  canMoveDown: boolean
  onRemove: () => void
  onMoveUp: () => void
  onMoveDown: () => void
}

function ItemRow({
  index,
  control,
  register,
  errors,
  currencyCode,
  language,
  canRemove,
  canMoveUp,
  canMoveDown,
  onRemove,
  onMoveUp,
  onMoveDown,
}: ItemRowProps) {
  const { t } = useTranslation('capex')
  const itemErrors = errors.items?.[index]

  return (
    <li className="seg-capex-editor__item">
      <div className="seg-capex-editor__item-grid">
        <Input
          className="seg-capex-editor__item-desc"
          label={t('editor.fields.description')}
          placeholder={t('editor.fields.descriptionPlaceholder')}
          required
          error={itemErrors?.description?.message}
          {...register(`items.${index}.description` as const)}
        />
        <Input
          label={t('editor.fields.quantity')}
          inputMode="decimal"
          required
          error={itemErrors?.quantity?.message}
          {...register(`items.${index}.quantity` as const)}
        />
        <Input
          label={t('editor.fields.unitAmount')}
          inputMode="decimal"
          required
          error={itemErrors?.unitAmount?.message}
          {...register(`items.${index}.unitAmount` as const)}
        />
        <div className="seg-capex-editor__line-total">
          <span>{t('editor.fields.lineTotal')}</span>
          <LineTotal
            control={control}
            index={index}
            currencyCode={currencyCode}
            language={language}
          />
        </div>
      </div>
      <div className="seg-capex-editor__item-actions">
        <button
          type="button"
          className="seg-capex-editor__icon"
          onClick={onMoveUp}
          disabled={!canMoveUp}
          aria-label={t('editor.items.moveUp')}
        >
          <ArrowUp size={16} aria-hidden="true" />
        </button>
        <button
          type="button"
          className="seg-capex-editor__icon"
          onClick={onMoveDown}
          disabled={!canMoveDown}
          aria-label={t('editor.items.moveDown')}
        >
          <ArrowDown size={16} aria-hidden="true" />
        </button>
        <button
          type="button"
          className="seg-capex-editor__icon seg-capex-editor__icon--danger"
          onClick={onRemove}
          disabled={!canRemove}
          aria-label={t('editor.items.remove')}
        >
          <Trash2 size={16} aria-hidden="true" />
        </button>
      </div>
    </li>
  )
}

interface TotalPreviewProps {
  control: Control<EntryFormValues>
  currencyCode: string
  language: string
}

/**
 * Live entry total. It subscribes to the item lines in isolation so that typing
 * into an amount re-renders only this node, never the inputs themselves.
 */
function TotalPreview({ control, currencyCode, language }: TotalPreviewProps) {
  const items = useWatch({ control, name: 'items' })
  const total = (items ?? []).reduce((sum, item) => {
    const amount = lineAmountOf(item)
    return amount == null ? sum : sum + amount
  }, 0)
  return <strong>{formatCurrency(total, currencyCode, language)}</strong>
}

interface LineTotalProps {
  control: Control<EntryFormValues>
  index: number
  currencyCode: string
  language: string
}

/** Live per-line total, isolated like {@link TotalPreview}. */
function LineTotal({ control, index, currencyCode, language }: LineTotalProps) {
  const item = useWatch({ control, name: `items.${index}` })
  const amount = item == null ? 0 : (lineAmountOf(item) ?? 0)
  return <strong>{formatCurrency(amount, currencyCode, language)}</strong>
}

function lineAmountOf(item: { quantity?: string; unitAmount?: string }): number | null {
  const quantity = parseAmount(item.quantity ?? '')
  const unitAmount = parseAmount(item.unitAmount ?? '')
  if (quantity == null || unitAmount == null) return null
  return computeLineAmount(quantity, unitAmount)
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'capex.entry.validation':
        return t('editor.errors.validation')
      case 'capex.catalog.unknown_reference':
        return t('editor.errors.unknownReference')
      case 'capex.entry.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}
