import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  FileText,
  Globe,
  Lock,
  Paperclip,
  Trash2,
  TrendingDown,
  TrendingUp,
  X,
} from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import type {
  CreateOpexContractRequest,
  OpexContract,
  OpexContractStatus,
  OpexExpectedFrequency,
  OpexMovementType,
  OpexVisibility,
} from '@/app/api/opex'
import { opexApi } from '@/app/api/opex'
import { isApiError } from '@/app/api/errors'
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

import { attachmentAccept, formatFileSize, rejectionFor } from './attachments'
import { ContractAttachments } from './ContractAttachments'
import {
  buildDefaults,
  createContractSchema,
  fromContract,
  toRequest,
  type ContractFormValues,
} from './contractForm'
import {
  opexKeys,
  useOpexCategories,
  useCostCenters,
  useCurrencies,
  useSuppliers,
} from './queries'
import { OccurrencesTab } from './OccurrencesTab'

import './ContractDialog.css'

export interface ContractDialogProps {
  mode: 'create' | 'edit'
  contractId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (contract: OpexContract, mode: 'create' | 'edit') => void
  onDeleted: (contract: OpexContract) => void
}

export function ContractDialog({
  mode,
  contractId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: ContractDialogProps) {
  const { t } = useTranslation('opex')

  const categories = useOpexCategories()
  const suppliers = useSuppliers()
  const costCenters = useCostCenters()
  const currencies = useCurrencies()

  const contractQuery = useQuery({
    queryKey: opexKeys.contract(contractId as number),
    queryFn: ({ signal }) => opexApi.getContract(contractId as number, signal),
    enabled: mode === 'edit' && contractId != null,
  })

  const catalogsReady =
    categories.data != null &&
    suppliers.data != null &&
    costCenters.data != null &&
    currencies.data != null

  const title = mode === 'create' ? t('editor.createTitle') : t('editor.editTitle')
  const description =
    mode === 'create' ? t('editor.createDescription') : t('editor.editDescription')

  if (!catalogsReady || (mode === 'edit' && contractQuery.isPending)) {
    return (
      <Dialog
        scrollable
        width={820}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-opex-editor__status">
          <Spinner />
          <span>{t('editor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && contractQuery.isError) {
    const notFound =
      isApiError(contractQuery.error) && contractQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={820}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-opex-editor__error" role="alert">
          {notFound ? t('editor.notFound') : t('editor.loadError')}
        </p>
      </Dialog>
    )
  }

  const contract = mode === 'edit' ? (contractQuery.data as OpexContract) : undefined
  const initialValues =
    contract != null
      ? fromContract(contract)
      : buildDefaults({
          categoryId: firstCatalogId(categories.data),
          currencyId: firstCatalogId(currencies.data),
        })

  const canChangeVisibility =
    contract == null ||
    (currentUserId != null && contract.createdById === currentUserId)

  return (
    <ContractEditorForm
      mode={mode}
      contractId={contractId}
      contract={contract}
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

function firstCatalogId(items: ReadonlyArray<{ id: number }> | undefined): string {
  const first = items?.[0]
  return first != null ? String(first.id) : ''
}

interface ContractEditorFormProps {
  mode: 'create' | 'edit'
  contractId?: number
  contract?: OpexContract
  title: string
  description: string
  initialValues: ContractFormValues
  categories: ReadonlyArray<{ id: number; name: string }>
  suppliers: ReadonlyArray<{ id: number; name: string }>
  costCenters: ReadonlyArray<{ id: number; name: string }>
  currencies: ReadonlyArray<{ id: number; code: string }>
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (contract: OpexContract, mode: 'create' | 'edit') => void
  onDeleted: (contract: OpexContract) => void
}

const movementTypes: OpexMovementType[] = ['Income', 'Expense']
const statuses: OpexContractStatus[] = ['Planning', 'Active', 'OnHold', 'Closed']
const frequencies: OpexExpectedFrequency[] = [
  'None',
  'Weekly',
  'Monthly',
  'Quarterly',
  'SemiAnnual',
  'Annual',
  'Irregular',
]
const visibilities: OpexVisibility[] = ['Public', 'Private']

const movementTypeMeta: Record<
  OpexMovementType,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Income: { icon: <TrendingUp size={15} />, tone: 'success' },
  Expense: { icon: <TrendingDown size={15} />, tone: 'neutral' },
}
const visibilityMeta: Record<OpexVisibility, { icon: ReactNode; tone: SegmentTone }> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

function ContractEditorForm({
  mode,
  contractId,
  contract,
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
}: ContractEditorFormProps) {
  const { t } = useTranslation('opex')

  const schema = useMemo(
    () =>
      createContractSchema({
        nameRequired: t('editor.validation.nameRequired'),
        nameTooLong: t('editor.validation.nameTooLong'),
        categoryRequired: t('editor.validation.categoryRequired'),
        currencyRequired: t('editor.validation.currencyRequired'),
        estimatedAmountInvalid: t('editor.validation.estimatedAmountInvalid'),
        notesTooLong: t('editor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<ContractFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, control, handleSubmit, formState } = form

  const [activeTab, setActiveTab] = useState<'details' | 'occurrences'>('details')
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdContract, setCreatedContract] = useState<OpexContract | null>(null)
  const editedRef = useRef(false)

  const currencyId = useWatch({ control, name: 'currencyId' })
  const currencyCode =
    currencies.find((c) => String(c.id) === currencyId)?.code ?? 'EUR'

  const mutation = useMutation({
    mutationFn: (request: CreateOpexContractRequest) =>
      mode === 'create'
        ? opexApi.createContract(request)
        : opexApi.updateContract(contractId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedContract(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => opexApi.deleteContract(contractId as number),
    onSuccess: () => {
      if (contract != null) onDeleted(contract)
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

  if (createdContract != null) {
    const finish = () => onSaved(createdContract, 'create')
    return (
      <Dialog
        scrollable
        width={820}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdContract.name,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-opex-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <ContractAttachments
            contractId={createdContract.id}
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
          activeTab === 'occurrences' ? (
            <Button variant="ghost" onClick={requestClose}>
              {t('editor.actions.cancel')}
            </Button>
          ) : (
            <>
              {mode === 'edit' && (
                <Button
                  variant="ghost"
                  className="seg-opex-editor__delete"
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
              <Button type="submit" form="seg-opex-contract-form" disabled={submitting}>
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
          className="seg-opex-editor__tabs"
          variant="line"
          value={activeTab}
          onChange={(v) => setActiveTab(v as 'details' | 'occurrences')}
          tabs={[
            { value: 'details', label: t('editor.tabs.details') },
            ...(mode === 'edit'
              ? [{ value: 'occurrences', label: t('editor.tabs.occurrences') }]
              : []),
          ]}
        />

        {activeTab === 'occurrences' && mode === 'edit' && contractId != null && (
          <OccurrencesTab contractId={contractId} currencyCode={currencyCode} />
        )}

        <form
          hidden={activeTab !== 'details'}
          id="seg-opex-contract-form"
          className="seg-opex-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-opex-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-opex-editor__section">
            <h3>{t('editor.sections.general')}</h3>
            <Input
              label={t('editor.fields.name')}
              placeholder={t('editor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-opex-editor__grid">
              <ToggleField id="opex-field-type" label={t('editor.fields.type')}>
                <SegmentedControl
                  aria-labelledby="opex-field-type"
                  {...register('movementType')}
                  options={movementTypes.map((value) => ({
                    value,
                    label: t(`contracts.type.${value}`),
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
                    label: t(`contracts.status.${value}`),
                  }))}
                />
              </Field>
              <Input
                type="date"
                label={t('editor.fields.startDate')}
                {...register('startDate')}
              />
              <Input
                type="date"
                label={t('editor.fields.closedDate')}
                {...register('closedDate')}
              />
              <Input
                label={t('editor.fields.estimatedAnnualAmount')}
                inputMode="decimal"
                placeholder="0.00"
                error={formState.errors.estimatedAnnualAmount?.message}
                hint={currencyCode}
                {...register('estimatedAnnualAmount')}
              />
              <Field label={t('editor.fields.expectedFrequency')}>
                <Select
                  {...register('expectedFrequency')}
                  options={frequencies.map((value) => ({
                    value,
                    label: t(`contracts.frequency.${value}`),
                  }))}
                />
              </Field>
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
                id="opex-field-visibility"
                label={t('editor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="opex-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`contracts.visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-opex-editor__section">
            <h3>{t('editor.sections.notes')}</h3>
            <label className="seg-opex-editor__notes">
              <span className="seg-opex-editor__notes-label">
                {t('editor.fields.notes')}
              </span>
              <textarea
                className="seg-opex-editor__textarea"
                rows={4}
                placeholder={t('editor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-opex-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-opex-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-opex-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && contractId != null ? (
              <ContractAttachments contractId={contractId} />
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

interface StagedAttachmentsProps {
  files: File[]
  onChange: (files: File[]) => void
}

function StagedAttachments({ files, onChange }: StagedAttachmentsProps) {
  const { t } = useTranslation('opex')
  const input = useRef<HTMLInputElement>(null)

  const removeAt = (index: number) =>
    onChange(files.filter((_, position) => position !== index))

  return (
    <div className="seg-opex-attach">
      <div className="seg-opex-attach__head">
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
          className="seg-opex-attach__input"
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
        <p className="seg-opex-attach__empty">{t('editor.attachments.empty')}</p>
      ) : (
        <>
          <p className="seg-opex-editor__hint">{t('editor.attachments.stagedHint')}</p>
          <ul className="seg-opex-attach__list">
            {files.map((file, index) => {
              const rejection = rejectionFor(file)
              return (
                <li
                  key={`${file.name}-${index}`}
                  className={
                    'seg-opex-attach__item' +
                    (rejection != null ? ' seg-opex-attach__item--error' : '')
                  }
                >
                  <FileText
                    size={18}
                    aria-hidden="true"
                    className="seg-opex-attach__icon"
                  />
                  <span className="seg-opex-attach__meta">
                    <span className="seg-opex-attach__name">{file.name}</span>
                    <span className="seg-opex-attach__size">
                      {rejection === 'tooLarge'
                        ? t('editor.attachments.errors.tooLarge')
                        : rejection === 'type'
                          ? t('editor.attachments.errors.type')
                          : formatFileSize(file.size)}
                    </span>
                  </span>
                  <button
                    type="button"
                    className="seg-opex-attach__action"
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

function Field({ label, hint, error, children }: FieldProps) {
  const message = error ?? hint
  return (
    <div className="seg-opex-editor__field">
      <label className="seg-opex-editor__field-control">
        <span className="seg-opex-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-opex-editor__field-hint' +
            (error != null ? ' seg-opex-editor__field-hint--error' : '')
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
  children: React.ReactNode
}

function ToggleField({ id, label, hint, children }: ToggleFieldProps) {
  return (
    <div className="seg-opex-editor__field">
      <span className="seg-opex-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-opex-editor__field-hint">{hint}</span>}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'opex.contract.validation':
        return t('editor.errors.validation')
      case 'opex.contract.duplicate_name':
        return t('editor.errors.duplicateName')
      case 'opex.catalog.unknown_reference':
        return t('editor.errors.unknownReference')
      case 'opex.contract.visibility_forbidden':
        return t('editor.errors.visibilityForbidden')
    }
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('editor.errors.conflict')
    }
  }
  return t('editor.errors.generic')
}
