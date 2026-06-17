import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Trash2 } from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  travelApi,
  type CreateTravelExpenseRequest,
  type TravelExpense,
} from '@/app/api/travel'
import { isApiError } from '@/app/api/errors'
import { Button, Dialog, Input, Select, Spinner } from '@/components/ui'

import { TravelAttachments } from './TravelAttachments'
import {
  buildDefaults,
  createExpenseSchema,
  fromExpense,
  toRequest,
  type ExpenseFormValues,
} from './expenseForm'
import {
  travelKeys,
  useCostCenters,
  useCurrencies,
  useSuppliers,
  useTravelExpenseCategories,
} from './queries'

export interface ExpenseDialogProps {
  tripId: number
  mode: 'create' | 'edit'
  expenseId?: number
  onClose: () => void
  onSaved: (mode: 'create' | 'edit') => void
  onDeleted: () => void
}

export function ExpenseDialog({
  tripId,
  mode,
  expenseId,
  onClose,
  onSaved,
  onDeleted,
}: ExpenseDialogProps) {
  const { t } = useTranslation('travel')

  const categories = useTravelExpenseCategories()
  const currencies = useCurrencies()
  const suppliers = useSuppliers()
  const costCenters = useCostCenters()

  const expenseQuery = useQuery({
    queryKey: travelKeys.expense(tripId, expenseId as number),
    queryFn: ({ signal }) => travelApi.getExpense(tripId, expenseId as number, signal),
    enabled: mode === 'edit' && expenseId != null,
  })

  const catalogsReady =
    categories.data != null &&
    currencies.data != null &&
    suppliers.data != null &&
    costCenters.data != null

  const title =
    mode === 'create' ? t('expenseEditor.createTitle') : t('expenseEditor.editTitle')

  if (!catalogsReady || (mode === 'edit' && expenseQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={640}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-trv-editor__status">
          <Spinner />
          <span>{t('expenseEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && expenseQuery.isError) {
    const notFound =
      isApiError(expenseQuery.error) && expenseQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={640}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-trv-editor__error" role="alert">
          {notFound ? t('expenseEditor.notFound') : t('expenseEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const expense = mode === 'edit' ? (expenseQuery.data as TravelExpense) : undefined
  const initialValues =
    expense != null
      ? fromExpense(expense)
      : buildDefaults({ expenseCategoryId: firstCatalogId(categories.data) })

  return (
    <ExpenseEditorForm
      tripId={tripId}
      mode={mode}
      expenseId={expenseId}
      expense={expense}
      title={title}
      initialValues={initialValues}
      categories={categories.data ?? []}
      currencies={currencies.data ?? []}
      suppliers={suppliers.data ?? []}
      costCenters={costCenters.data ?? []}
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

interface ExpenseEditorFormProps {
  tripId: number
  mode: 'create' | 'edit'
  expenseId?: number
  expense?: TravelExpense
  title: string
  initialValues: ExpenseFormValues
  categories: ReadonlyArray<{ id: number; name: string }>
  currencies: ReadonlyArray<{ id: number; code: string }>
  suppliers: ReadonlyArray<{ id: number; name: string }>
  costCenters: ReadonlyArray<{ id: number; name: string }>
  onClose: () => void
  onSaved: (mode: 'create' | 'edit') => void
  onDeleted: () => void
}

function ExpenseEditorForm({
  tripId,
  mode,
  expenseId,
  expense,
  title,
  initialValues,
  categories,
  currencies,
  suppliers,
  costCenters,
  onClose,
  onSaved,
  onDeleted,
}: ExpenseEditorFormProps) {
  const { t } = useTranslation('travel')

  const schema = useMemo(
    () =>
      createExpenseSchema({
        categoryRequired: t('expenseEditor.validation.categoryRequired'),
        descriptionRequired: t('expenseEditor.validation.descriptionRequired'),
        descriptionTooLong: t('expenseEditor.validation.descriptionTooLong'),
        dateRequired: t('expenseEditor.validation.dateRequired'),
        amountInvalid: t('expenseEditor.validation.amountInvalid'),
        currencyRequired: t('expenseEditor.validation.currencyRequired'),
        notesTooLong: t('expenseEditor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<ExpenseFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState } = form

  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CreateTravelExpenseRequest) =>
      mode === 'create'
        ? travelApi.createExpense(tripId, request)
        : travelApi.updateExpense(tripId, expenseId as number, request),
    onSuccess: () => onSaved(mode),
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => travelApi.deleteExpense(tripId, expenseId as number),
    onSuccess: () => onDeleted(),
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

  const catalogOptions = (
    rows: ReadonlyArray<{ id: number; name?: string; code?: string }>,
    useCode = false,
  ) =>
    rows.map((row) => ({
      value: String(row.id),
      label: useCode ? (row.code ?? '') : (row.name ?? ''),
    }))

  const noneOption = { value: '', label: t('expenseEditor.fields.none') }
  const submitting = mutation.isPending
  const busy = submitting || deleteMutation.isPending

  return (
    <>
      <Dialog
        scrollable
        width={640}
        title={title}
        onClose={requestClose}
        closeLabel={t('editor.actions.cancel')}
        footer={
          <>
            {mode === 'edit' && (
              <Button
                variant="ghost"
                className="seg-trv-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={busy}
              >
                {t('expenseEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-trv-expense-form" disabled={busy}>
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
          id="seg-trv-expense-form"
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
            <Input
              label={t('expenseEditor.fields.description')}
              placeholder={t('expenseEditor.fields.descriptionPlaceholder')}
              required
              error={formState.errors.description?.message}
              {...register('description')}
            />
            <div className="seg-trv-editor__grid">
              <Field
                label={t('expenseEditor.fields.category')}
                error={formState.errors.expenseCategoryId?.message}
              >
                <Select
                  {...register('expenseCategoryId')}
                  aria-invalid={formState.errors.expenseCategoryId != null}
                  options={catalogOptions(categories)}
                />
              </Field>
              <Input
                type="date"
                label={t('expenseEditor.fields.date')}
                required
                error={formState.errors.date?.message}
                {...register('date')}
              />
              <Input
                label={t('expenseEditor.fields.amount')}
                inputMode="decimal"
                required
                error={formState.errors.amount?.message}
                {...register('amount')}
              />
              <Field
                label={t('expenseEditor.fields.currency')}
                error={formState.errors.currencyId?.message}
              >
                <Select
                  {...register('currencyId')}
                  aria-invalid={formState.errors.currencyId != null}
                  options={[
                    { value: '', label: t('expenseEditor.fields.currencyPlaceholder') },
                    ...catalogOptions(currencies, true),
                  ]}
                />
              </Field>
              <Field label={t('expenseEditor.fields.supplier')}>
                <Select
                  {...register('supplierId')}
                  options={[noneOption, ...catalogOptions(suppliers)]}
                />
              </Field>
              <Field label={t('expenseEditor.fields.costCenter')}>
                <Select
                  {...register('costCenterId')}
                  options={[noneOption, ...catalogOptions(costCenters)]}
                />
              </Field>
            </div>
          </section>

          <section className="seg-trv-editor__section">
            <label className="seg-trv-editor__notes">
              <span className="seg-trv-editor__notes-label">
                {t('expenseEditor.fields.notes')}
              </span>
              <textarea
                className="seg-trv-editor__textarea"
                rows={3}
                placeholder={t('expenseEditor.fields.notesPlaceholder')}
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
            {mode === 'edit' && expense != null ? (
              <TravelAttachments
                owner={{ kind: 'expense', tripId, expenseId: expense.id }}
              />
            ) : (
              <p className="seg-trv-editor__hint">
                {t('expenseEditor.attachmentsAfterSave')}
              </p>
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
          title={t('expenseEditor.delete.title')}
          description={t('expenseEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('expenseEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('expenseEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('expenseEditor.delete.deleting')
                  : t('expenseEditor.delete.confirm')}
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
  error?: string
  children: ReactNode
}

function Field({ label, error, children }: FieldProps) {
  return (
    <div className="seg-trv-editor__field">
      <label className="seg-trv-editor__field-control">
        <span className="seg-trv-editor__field-label">{label}</span>
        {children}
      </label>
      {error != null && (
        <span className="seg-trv-editor__field-hint seg-trv-editor__field-hint--error">
          {error}
        </span>
      )}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'travel.expense.validation':
        return t('expenseEditor.errors.validation')
      case 'travel.catalog.unknown_reference':
        return t('expenseEditor.errors.unknownReference')
      case 'travel.expense.not_found':
      case 'travel.trip.not_found':
        return t('expenseEditor.errors.notFound')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('expenseEditor.errors.conflict')
    }
  }
  return t('expenseEditor.errors.generic')
}
