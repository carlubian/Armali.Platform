import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Globe, Lock, Trash2 } from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  inventoryApi,
  type CreateInventoryItemRequest,
  type InventoryItem,
  type InventoryItemStatus,
  type InventoryVisibility,
} from '@/app/api/inventory'
import type { Supplier } from '@/app/api/configuration'
import { isApiError } from '@/app/api/errors'
import {
  Button,
  Dialog,
  Input,
  SegmentedControl,
  Select,
  Spinner,
  type SegmentTone,
} from '@/components/ui'

import { InventoryAttachments } from './InventoryAttachments'
import { StagedAttachments } from './StagedAttachments'
import {
  buildDefaults,
  createItemSchema,
  fromItem,
  toRequest,
  type ItemFormValues,
} from './itemForm'
import {
  inventoryKeys,
  useInventoryCategories,
  useInventoryLocations,
  useSuppliers,
} from './queries'

import './InventoryDialog.css'

export interface ItemDialogProps {
  mode: 'create' | 'edit'
  itemId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (item: InventoryItem, mode: 'create' | 'edit') => void
  onDeleted: (item: InventoryItem) => void
}

export function ItemDialog({
  mode,
  itemId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: ItemDialogProps) {
  const { t } = useTranslation('inventory')

  const categories = useInventoryCategories()
  const locations = useInventoryLocations()
  const suppliers = useSuppliers()

  const itemQuery = useQuery({
    queryKey: inventoryKeys.item(itemId as number),
    queryFn: ({ signal }) => inventoryApi.getItem(itemId as number, signal),
    enabled: mode === 'edit' && itemId != null,
  })

  const catalogsReady =
    categories.data != null && locations.data != null && suppliers.data != null

  const title =
    mode === 'create' ? t('itemEditor.createTitle') : t('itemEditor.editTitle')
  const description =
    mode === 'create'
      ? t('itemEditor.createDescription')
      : t('itemEditor.editDescription')

  if (!catalogsReady || (mode === 'edit' && itemQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-inv-editor__status">
          <Spinner />
          <span>{t('itemEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && itemQuery.isError) {
    const notFound = isApiError(itemQuery.error) && itemQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-inv-editor__error" role="alert">
          {notFound ? t('itemEditor.notFound') : t('itemEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const item = mode === 'edit' ? (itemQuery.data as InventoryItem) : undefined
  const initialValues =
    item != null
      ? fromItem(item)
      : buildDefaults({
          categoryId: firstCatalogId(categories.data),
          locationId: firstCatalogId(locations.data),
        })

  const canChangeVisibility =
    item == null || (currentUserId != null && item.createdById === currentUserId)

  return (
    <ItemEditorForm
      mode={mode}
      itemId={itemId}
      item={item}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data ?? []}
      locations={locations.data ?? []}
      suppliers={suppliers.data ?? []}
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

interface ItemEditorFormProps {
  mode: 'create' | 'edit'
  itemId?: number
  item?: InventoryItem
  title: string
  description: string
  initialValues: ItemFormValues
  categories: ReadonlyArray<{ id: number; name: string }>
  locations: ReadonlyArray<{ id: number; name: string }>
  suppliers: Supplier[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (item: InventoryItem, mode: 'create' | 'edit') => void
  onDeleted: (item: InventoryItem) => void
}

const statuses: InventoryItemStatus[] = ['Candidate', 'Active', 'Deprecated']
const visibilities: InventoryVisibility[] = ['Public', 'Private']

const visibilityMeta: Record<
  InventoryVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

function ItemEditorForm({
  mode,
  itemId,
  item,
  title,
  description,
  initialValues,
  categories,
  locations,
  suppliers,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: ItemEditorFormProps) {
  const { t } = useTranslation('inventory')

  const schema = useMemo(
    () =>
      createItemSchema({
        nameRequired: t('itemEditor.validation.nameRequired'),
        nameTooLong: t('itemEditor.validation.nameTooLong'),
        categoryRequired: t('itemEditor.validation.categoryRequired'),
        locationRequired: t('itemEditor.validation.locationRequired'),
        stockInvalid: t('itemEditor.validation.stockInvalid'),
        suppliersRequired: t('itemEditor.validation.suppliersRequired'),
        notesTooLong: t('itemEditor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<ItemFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState, setValue } = form

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdItem, setCreatedItem] = useState<InventoryItem | null>(null)
  const editedRef = useRef(false)

  const selectedSupplierIds = useWatch({ control, name: 'supplierIds' }) ?? []

  const toggleSupplier = (supplierId: number, checked: boolean) => {
    editedRef.current = true
    const next = checked
      ? [...selectedSupplierIds, supplierId]
      : selectedSupplierIds.filter((id) => id !== supplierId)
    setValue('supplierIds', next, { shouldValidate: formState.isSubmitted })
  }

  const mutation = useMutation({
    mutationFn: (request: CreateInventoryItemRequest) =>
      mode === 'create'
        ? inventoryApi.createItem(request)
        : inventoryApi.updateItem(itemId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedItem(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => inventoryApi.deleteItem(itemId as number),
    onSuccess: () => {
      if (item != null) onDeleted(item)
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

  const catalogOptions = (rows: ReadonlyArray<{ id: number; name: string }>) =>
    rows.map((row) => ({ value: String(row.id), label: row.name }))

  const submitting = mutation.isPending

  if (createdItem != null) {
    const finish = () => onSaved(createdItem, 'create')
    return (
      <Dialog
        scrollable
        width={760}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdItem.name,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-inv-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <InventoryAttachments
            kind="item"
            ownerId={createdItem.id}
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
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-inv-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('itemEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-inv-item-form" disabled={submitting}>
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
          id="seg-inv-item-form"
          className="seg-inv-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-inv-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-inv-editor__section">
            <h3>{t('itemEditor.sections.general')}</h3>
            <Input
              label={t('itemEditor.fields.name')}
              placeholder={t('itemEditor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-inv-editor__grid">
              <Field label={t('itemEditor.fields.status')}>
                <Select
                  {...register('status')}
                  options={statuses.map((value) => ({
                    value,
                    label: t(`items.status.${value}`),
                  }))}
                />
              </Field>
              <Field
                label={t('itemEditor.fields.category')}
                error={formState.errors.categoryId?.message}
              >
                <Select
                  {...register('categoryId')}
                  aria-invalid={formState.errors.categoryId != null}
                  options={catalogOptions(categories)}
                />
              </Field>
              <Field
                label={t('itemEditor.fields.location')}
                error={formState.errors.locationId?.message}
              >
                <Select
                  {...register('locationId')}
                  aria-invalid={formState.errors.locationId != null}
                  options={catalogOptions(locations)}
                />
              </Field>
              <Input
                label={t('itemEditor.fields.currentStock')}
                inputMode="decimal"
                required
                error={formState.errors.currentStock?.message}
                {...register('currentStock')}
              />
              <Input
                label={t('itemEditor.fields.minimumStock')}
                inputMode="decimal"
                required
                error={formState.errors.minimumStock?.message}
                {...register('minimumStock')}
              />
              <ToggleField
                id="inv-field-visibility"
                label={t('itemEditor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="inv-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`items.visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-inv-editor__section">
            <h3>{t('itemEditor.sections.suppliers')}</h3>
            <p className="seg-inv-editor__hint">{t('itemEditor.suppliers.hint')}</p>
            <fieldset
              className="seg-inv-editor__suppliers"
              aria-invalid={formState.errors.supplierIds != null}
            >
              <legend className="seg-inv__sr">
                {t('itemEditor.fields.suppliers')}
              </legend>
              {suppliers.length === 0 ? (
                <p className="seg-inv-editor__hint">{t('itemEditor.suppliers.none')}</p>
              ) : (
                suppliers.map((supplier) => (
                  <label key={supplier.id} className="seg-inv-editor__supplier">
                    <input
                      type="checkbox"
                      checked={selectedSupplierIds.includes(supplier.id)}
                      onChange={(event) =>
                        toggleSupplier(supplier.id, event.target.checked)
                      }
                    />
                    <span>{supplier.name}</span>
                  </label>
                ))
              )}
            </fieldset>
            {formState.errors.supplierIds?.message != null && (
              <span className="seg-inv-editor__field-error" role="alert">
                {formState.errors.supplierIds.message}
              </span>
            )}
          </section>

          <section className="seg-inv-editor__section">
            <h3>{t('itemEditor.sections.notes')}</h3>
            <label className="seg-inv-editor__notes">
              <span className="seg-inv-editor__notes-label">
                {t('itemEditor.fields.notes')}
              </span>
              <textarea
                className="seg-inv-editor__textarea"
                rows={4}
                placeholder={t('itemEditor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-inv-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-inv-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-inv-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && itemId != null ? (
              <InventoryAttachments kind="item" ownerId={itemId} />
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
        <ItemDeleteDialog
          itemId={itemId as number}
          deleting={deleteMutation.isPending}
          onClose={() => setConfirmingDelete(false)}
          onConfirm={() => deleteMutation.mutate()}
        />
      )}
    </>
  )
}

function ItemDeleteDialog({
  itemId,
  deleting,
  onClose,
  onConfirm,
}: {
  itemId: number
  deleting: boolean
  onClose: () => void
  onConfirm: () => void
}) {
  const { t } = useTranslation('inventory')
  const impactQuery = useQuery({
    queryKey: [...inventoryKeys.item(itemId), 'deletion-impact'] as const,
    queryFn: ({ signal }) => inventoryApi.itemDeletionImpact(itemId, signal),
    staleTime: 0,
    gcTime: 0,
  })

  const description =
    impactQuery.data?.isReferenced === true
      ? t('itemEditor.delete.referencedDescription', {
          count: impactQuery.data.referenceCount,
        })
      : t('itemEditor.delete.description')

  return (
    <Dialog
      width={480}
      title={t('itemEditor.delete.title')}
      description={description}
      onClose={onClose}
      closeLabel={t('itemEditor.delete.cancel')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={deleting}>
            {t('itemEditor.delete.cancel')}
          </Button>
          <Button
            variant="danger"
            onClick={onConfirm}
            disabled={deleting || impactQuery.isPending || impactQuery.isError}
          >
            {deleting
              ? t('itemEditor.delete.deleting')
              : t('itemEditor.delete.confirm')}
          </Button>
        </>
      }
    >
      {impactQuery.isPending && (
        <div className="seg-inv-editor__status">
          <Spinner />
          <span>{t('itemEditor.delete.loadingImpact')}</span>
        </div>
      )}
      {impactQuery.isError && (
        <p className="seg-inv-editor__error" role="alert">
          {t('itemEditor.delete.impactError')}
        </p>
      )}
    </Dialog>
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
    <div className="seg-inv-editor__field">
      <label className="seg-inv-editor__field-control">
        <span className="seg-inv-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-inv-editor__field-hint' +
            (error != null ? ' seg-inv-editor__field-hint--error' : '')
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
    <div className="seg-inv-editor__field">
      <span className="seg-inv-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-inv-editor__field-hint">{hint}</span>}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'inventory.item.validation':
        return t('itemEditor.errors.validation')
      case 'inventory.item.supplier_required':
        return t('itemEditor.errors.supplierRequired')
      case 'inventory.item.referenced':
        return t('itemEditor.errors.referenced')
      case 'inventory.item.visibility_forbidden':
        return t('itemEditor.errors.visibilityForbidden')
      case 'inventory.catalog.unknown_reference':
        return t('itemEditor.errors.unknownReference')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('itemEditor.errors.conflict')
    }
  }
  return t('itemEditor.errors.generic')
}
