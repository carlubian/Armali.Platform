import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { AlertTriangle, Globe, Lock, PackageCheck, Plus, Trash2 } from 'lucide-react'
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
  inventoryApi,
  type CreateInventoryOrderRequest,
  type InventoryItemStatus,
  type InventoryOrder,
  type InventoryOrderStatus,
  type InventoryVisibility,
} from '@/app/api/inventory'
import type { Currency, Supplier } from '@/app/api/configuration'
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

import { InventoryAttachments } from './InventoryAttachments'
import { StagedAttachments } from './StagedAttachments'
import { parseAmount } from './itemForm'
import {
  blankLine,
  buildDefaults,
  createOrderSchema,
  fromOrder,
  toRequest,
  type OrderFormValues,
} from './orderForm'
import { inventoryKeys, useCurrencies, useSuppliers } from './queries'

import './InventoryDialog.css'

const maxLines = 100

interface KnownItem {
  id: number
  name: string
  status: InventoryItemStatus
  eligible: boolean
}

export interface OrderDialogProps {
  mode: 'create' | 'edit'
  orderId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (order: InventoryOrder, mode: 'create' | 'edit') => void
  onDeleted: (order: InventoryOrder) => void
  onReceived: (order: InventoryOrder) => void
}

export function OrderDialog({
  mode,
  orderId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
  onReceived,
}: OrderDialogProps) {
  const { t } = useTranslation('inventory')

  const suppliers = useSuppliers()
  const currencies = useCurrencies()

  const orderQuery = useQuery({
    queryKey: inventoryKeys.order(orderId as number),
    queryFn: ({ signal }) => inventoryApi.getOrder(orderId as number, signal),
    enabled: mode === 'edit' && orderId != null,
  })

  const catalogsReady = suppliers.data != null && currencies.data != null

  const title =
    mode === 'create' ? t('orderEditor.createTitle') : t('orderEditor.editTitle')
  const description =
    mode === 'create'
      ? t('orderEditor.createDescription')
      : t('orderEditor.editDescription')

  if (!catalogsReady || (mode === 'edit' && orderQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={820}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-inv-editor__status">
          <Spinner />
          <span>{t('orderEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && orderQuery.isError) {
    const notFound =
      isApiError(orderQuery.error) && orderQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={820}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-inv-editor__error" role="alert">
          {notFound ? t('orderEditor.notFound') : t('orderEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const order = mode === 'edit' ? (orderQuery.data as InventoryOrder) : undefined
  const initialValues =
    order != null
      ? fromOrder(order)
      : buildDefaults({
          supplierId: firstCatalogId(suppliers.data),
          currencyId: firstCatalogId(currencies.data),
        })

  const canChangeVisibility =
    order == null || (currentUserId != null && order.createdById === currentUserId)

  return (
    <OrderEditorForm
      mode={mode}
      orderId={orderId}
      order={order}
      title={title}
      description={description}
      initialValues={initialValues}
      suppliers={suppliers.data ?? []}
      currencies={currencies.data ?? []}
      canChangeVisibility={canChangeVisibility}
      onClose={onClose}
      onSaved={onSaved}
      onDeleted={onDeleted}
      onReceived={onReceived}
    />
  )
}

function firstCatalogId(items: ReadonlyArray<{ id: number }> | undefined): string {
  const first = items?.[0]
  return first != null ? String(first.id) : ''
}

interface OrderEditorFormProps {
  mode: 'create' | 'edit'
  orderId?: number
  order?: InventoryOrder
  title: string
  description: string
  initialValues: OrderFormValues
  suppliers: Supplier[]
  currencies: Currency[]
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (order: InventoryOrder, mode: 'create' | 'edit') => void
  onDeleted: (order: InventoryOrder) => void
  onReceived: (order: InventoryOrder) => void
}

const statuses: InventoryOrderStatus[] = ['Planning', 'Active', 'Received', 'Cancelled']
const visibilities: InventoryVisibility[] = ['Public', 'Private']

const visibilityMeta: Record<
  InventoryVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

function OrderEditorForm({
  mode,
  orderId,
  order,
  title,
  description,
  initialValues,
  suppliers,
  currencies,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
  onReceived,
}: OrderEditorFormProps) {
  const { t, i18n } = useTranslation('inventory')

  const schema = useMemo(
    () =>
      createOrderSchema({
        supplierRequired: t('orderEditor.validation.supplierRequired'),
        currencyRequired: t('orderEditor.validation.currencyRequired'),
        notesTooLong: t('orderEditor.validation.notesTooLong'),
        linesRequired: t('orderEditor.validation.linesRequired'),
        itemRequired: t('orderEditor.validation.itemRequired'),
        quantityInvalid: t('orderEditor.validation.quantityInvalid'),
        lineTotalInvalid: t('orderEditor.validation.lineTotalInvalid'),
      }),
    [t],
  )

  const form = useForm<OrderFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState } = form
  const lines = useFieldArray({ control, name: 'lines' })

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [confirmingReceive, setConfirmingReceive] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdOrder, setCreatedOrder] = useState<InventoryOrder | null>(null)
  const editedRef = useRef(false)

  const supplierId = useWatch({ control, name: 'supplierId' })
  const currencyId = useWatch({ control, name: 'currencyId' })
  const currencyCode =
    currencies.find((currency) => String(currency.id) === currencyId)?.code ?? 'EUR'

  const locked = mode === 'edit' && order?.status === 'Received'
  const canReceive = mode === 'edit' && order?.status === 'Active'

  // Items eligible for the selected supplier. The order's existing line items are
  // merged in so already-attached (possibly deprecated or now-ineligible) items
  // still render with their name and a warning.
  const eligibleQuery = useQuery({
    queryKey: ['inventory', 'orderEditor', 'eligibleItems', supplierId] as const,
    queryFn: ({ signal }) =>
      inventoryApi.listItems({ supplier: Number(supplierId), pageSize: 100 }, signal),
    enabled: supplierId !== '' && !locked,
  })

  const knownItems = useMemo(() => {
    const map = new Map<number, KnownItem>()
    order?.lines.forEach((line) =>
      map.set(line.itemId, {
        id: line.itemId,
        name: line.itemName,
        status: line.itemStatus,
        eligible: false,
      }),
    )
    eligibleQuery.data?.items.forEach((item) =>
      map.set(item.id, {
        id: item.id,
        name: item.name,
        status: item.status,
        eligible: true,
      }),
    )
    return map
  }, [order, eligibleQuery.data])

  const mutation = useMutation({
    mutationFn: (request: CreateInventoryOrderRequest) =>
      mode === 'create'
        ? inventoryApi.createOrder(request)
        : inventoryApi.updateOrder(orderId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedOrder(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => inventoryApi.deleteOrder(orderId as number),
    onSuccess: () => {
      if (order != null) onDeleted(order)
    },
    onError: (error) => {
      setConfirmingDelete(false)
      setServerError(mapServerError(error, t))
    },
  })

  const receiveMutation = useMutation({
    mutationFn: () => inventoryApi.receiveOrder(orderId as number),
    onSuccess: (received) => {
      setConfirmingReceive(false)
      onReceived(received)
    },
    onError: (error) => {
      setConfirmingReceive(false)
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

  const addLine = () => {
    if (lines.fields.length >= maxLines) return
    lines.append(blankLine())
  }

  const catalogOptions = (
    rows: ReadonlyArray<{ id: number; name?: string; code?: string }>,
    useCode = false,
  ) =>
    rows.map((row) => ({
      value: String(row.id),
      label: useCode ? (row.code ?? '') : (row.name ?? ''),
    }))

  const itemOptions = useMemo(() => {
    const options = Array.from(knownItems.values())
      .filter((entry) => entry.eligible)
      .map((entry) => ({ value: String(entry.id), label: entry.name }))
    return [{ value: '', label: t('orderEditor.fields.itemPlaceholder') }, ...options]
  }, [knownItems, t])

  const submitting = mutation.isPending
  const busy = submitting || deleteMutation.isPending || receiveMutation.isPending

  if (createdOrder != null) {
    const finish = () => onSaved(createdOrder, 'create')
    return (
      <Dialog
        scrollable
        width={820}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdOrder.supplierName,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-inv-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <InventoryAttachments
            kind="order"
            ownerId={createdOrder.id}
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
                className="seg-inv-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={busy}
              >
                {t('orderEditor.delete.action')}
              </Button>
            )}
            {canReceive && (
              <Button
                variant="outline"
                iconLeft={<PackageCheck size={15} />}
                onClick={() => setConfirmingReceive(true)}
                disabled={busy}
              >
                {t('orderEditor.receive.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-inv-order-form" disabled={busy}>
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
          id="seg-inv-order-form"
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

          {locked && (
            <p className="seg-inv-editor__notice" role="status">
              <AlertTriangle size={15} aria-hidden="true" />
              {t('orderEditor.receivedLockedHint')}
            </p>
          )}

          <section className="seg-inv-editor__section">
            <h3>{t('orderEditor.sections.general')}</h3>
            <div className="seg-inv-editor__grid">
              <Field
                label={t('orderEditor.fields.supplier')}
                error={formState.errors.supplierId?.message}
              >
                <Select
                  {...register('supplierId')}
                  disabled={locked}
                  aria-invalid={formState.errors.supplierId != null}
                  options={catalogOptions(suppliers)}
                />
              </Field>
              <Field label={t('orderEditor.fields.status')}>
                <Select
                  {...register('status')}
                  options={statuses.map((value) => ({
                    value,
                    label: t(`orders.status.${value}`),
                  }))}
                />
              </Field>
              <Field label={t('orderEditor.fields.currency')}>
                <Select
                  {...register('currencyId')}
                  disabled={locked}
                  options={catalogOptions(currencies, true)}
                />
              </Field>
              <Input
                type="date"
                label={t('orderEditor.fields.orderDate')}
                disabled={locked}
                {...register('orderDate')}
              />
              <Input
                type="date"
                label={t('orderEditor.fields.expectedReceiptDate')}
                disabled={locked}
                {...register('expectedReceiptDate')}
              />
              <ToggleField
                id="inv-order-visibility"
                label={t('orderEditor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="inv-order-visibility"
                  disabled={!canChangeVisibility || locked}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`orders.visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-inv-editor__section">
            <div className="seg-inv-editor__section-head">
              <h3>{t('orderEditor.sections.lines')}</h3>
            </div>
            {formState.errors.lines?.message != null && (
              <span className="seg-inv-editor__field-error" role="alert">
                {formState.errors.lines.message}
              </span>
            )}
            <ol className="seg-inv-editor__lines">
              {lines.fields.map((field, index) => (
                <LineRow
                  key={field.id}
                  index={index}
                  control={control}
                  register={register}
                  errors={formState.errors}
                  itemOptions={itemOptions}
                  knownItems={knownItems}
                  locked={locked}
                  canRemove={lines.fields.length > 1}
                  onRemove={() => lines.remove(index)}
                />
              ))}
            </ol>
            <div className="seg-inv-editor__lines-foot">
              <Button
                variant="outline"
                size="sm"
                iconLeft={<Plus size={15} />}
                onClick={addLine}
                disabled={locked || lines.fields.length >= maxLines}
              >
                {t('orderEditor.lines.addLine')}
              </Button>
              {lines.fields.length >= maxLines && (
                <span className="seg-inv-editor__hint">
                  {t('orderEditor.lines.maxReached')}
                </span>
              )}
              <div className="seg-inv-editor__total">
                <span>{t('orderEditor.total.label')}</span>
                <TotalPreview
                  control={control}
                  currencyCode={currencyCode}
                  language={i18n.language}
                />
              </div>
            </div>
          </section>

          <section className="seg-inv-editor__section">
            <h3>{t('orderEditor.sections.notes')}</h3>
            <label className="seg-inv-editor__notes">
              <span className="seg-inv-editor__notes-label">
                {t('orderEditor.fields.notes')}
              </span>
              <textarea
                className="seg-inv-editor__textarea"
                rows={4}
                disabled={locked}
                placeholder={t('orderEditor.fields.notesPlaceholder')}
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
            {mode === 'edit' && orderId != null ? (
              <InventoryAttachments kind="order" ownerId={orderId} />
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
          title={t('orderEditor.delete.title')}
          description={t('orderEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('orderEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('orderEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('orderEditor.delete.deleting')
                  : t('orderEditor.delete.confirm')}
              </Button>
            </>
          }
        />
      )}

      {confirmingReceive && (
        <Dialog
          width={460}
          title={t('orderEditor.receive.title')}
          description={t('orderEditor.receive.description')}
          onClose={() => setConfirmingReceive(false)}
          closeLabel={t('orderEditor.receive.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingReceive(false)}
                disabled={receiveMutation.isPending}
              >
                {t('orderEditor.receive.cancel')}
              </Button>
              <Button
                onClick={() => receiveMutation.mutate()}
                disabled={receiveMutation.isPending}
              >
                {receiveMutation.isPending
                  ? t('orderEditor.receive.receiving')
                  : t('orderEditor.receive.confirm')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}

interface LineRowProps {
  index: number
  control: Control<OrderFormValues>
  register: UseFormRegister<OrderFormValues>
  errors: ReturnType<typeof useForm<OrderFormValues>>['formState']['errors']
  itemOptions: { value: string; label: string }[]
  knownItems: Map<number, KnownItem>
  locked: boolean
  canRemove: boolean
  onRemove: () => void
}

function LineRow({
  index,
  control,
  register,
  errors,
  itemOptions,
  knownItems,
  locked,
  canRemove,
  onRemove,
}: LineRowProps) {
  const { t } = useTranslation('inventory')
  const lineErrors = errors.lines?.[index]
  const itemId = useWatch({ control, name: `lines.${index}.itemId` })

  const known = itemId === '' ? undefined : knownItems.get(Number(itemId))
  // An item already on the line but missing from the eligible list still needs to
  // be selectable so its name shows; surface it as an explicit option with a flag.
  const options =
    known != null && !known.eligible
      ? [...itemOptions, { value: String(known.id), label: known.name }]
      : itemOptions
  const warning =
    known == null
      ? null
      : known.status === 'Deprecated'
        ? t('orderEditor.lines.deprecatedWarning')
        : !known.eligible
          ? t('orderEditor.lines.ineligibleWarning')
          : null

  return (
    <li className="seg-inv-editor__line">
      <div className="seg-inv-editor__line-grid">
        <Field
          className="seg-inv-editor__line-item"
          label={t('orderEditor.fields.item')}
          error={lineErrors?.itemId?.message}
        >
          <Select
            {...register(`lines.${index}.itemId` as const)}
            disabled={locked}
            aria-invalid={lineErrors?.itemId != null}
            options={options}
          />
        </Field>
        <Input
          label={t('orderEditor.fields.quantity')}
          inputMode="decimal"
          required
          disabled={locked}
          error={lineErrors?.quantity?.message}
          {...register(`lines.${index}.quantity` as const)}
        />
        <Input
          label={t('orderEditor.fields.lineTotal')}
          inputMode="decimal"
          required
          disabled={locked}
          error={lineErrors?.lineTotal?.message}
          {...register(`lines.${index}.lineTotal` as const)}
        />
        <button
          type="button"
          className="seg-inv-editor__icon seg-inv-editor__icon--danger"
          onClick={onRemove}
          disabled={locked || !canRemove}
          aria-label={t('orderEditor.lines.remove')}
        >
          <Trash2 size={16} aria-hidden="true" />
        </button>
      </div>
      {warning != null && (
        <p className="seg-inv-editor__line-warning">
          <AlertTriangle size={13} aria-hidden="true" />
          {warning}
        </p>
      )}
    </li>
  )
}

interface TotalPreviewProps {
  control: Control<OrderFormValues>
  currencyCode: string
  language: string
}

function TotalPreview({ control, currencyCode, language }: TotalPreviewProps) {
  const lines = useWatch({ control, name: 'lines' })
  const total = (lines ?? []).reduce((sum, line) => {
    const amount = parseAmount(line?.lineTotal ?? '')
    return amount == null ? sum : sum + amount
  }, 0)
  return <strong>{formatCurrency(total, currencyCode, language)}</strong>
}

interface FieldProps {
  label: string
  hint?: string
  error?: string
  className?: string
  children: ReactNode
}

function Field({ label, hint, error, className, children }: FieldProps) {
  const message = error ?? hint
  return (
    <div
      className={'seg-inv-editor__field' + (className != null ? ` ${className}` : '')}
    >
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
      case 'inventory.order.validation':
        return t('orderEditor.errors.validation')
      case 'inventory.order.not_active':
        return t('orderEditor.errors.notActive')
      case 'inventory.order.received_locked':
        return t('orderEditor.errors.receivedLocked')
      case 'inventory.order.visibility_forbidden':
        return t('orderEditor.errors.visibilityForbidden')
      case 'inventory.order.line.supplier_not_allowed':
        return t('orderEditor.errors.lineSupplierNotAllowed')
      case 'inventory.order.line.item_not_accessible':
        return t('orderEditor.errors.lineItemNotAccessible')
      case 'inventory.catalog.unknown_reference':
        return t('orderEditor.errors.unknownReference')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('orderEditor.errors.conflict')
    }
  }
  return t('orderEditor.errors.generic')
}
