import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Globe, Lock, Trash2 } from 'lucide-react'
import { useMemo, useRef, useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import {
  assetsApi,
  type Asset,
  type AssetStatus,
  type AssetVisibility,
  type CreateAssetRequest,
} from '@/app/api/assets'
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

import { AssetAttachments } from './AssetAttachments'
import { StagedAssetAttachments } from './StagedAssetAttachments'
import {
  buildDefaults,
  createAssetSchema,
  fromAsset,
  toRequest,
  type AssetFormValues,
} from './assetForm'
import { assetsKeys, useAssetCategories, useAssetLocations } from './queries'

import './AssetsDialog.css'

export interface AssetDialogProps {
  mode: 'create' | 'edit'
  assetId?: number
  currentUserId: number | null
  onClose: () => void
  onSaved: (asset: Asset, mode: 'create' | 'edit') => void
  onDeleted: (asset: Asset) => void
}

export function AssetDialog({
  mode,
  assetId,
  currentUserId,
  onClose,
  onSaved,
  onDeleted,
}: AssetDialogProps) {
  const { t } = useTranslation('assets')
  const categories = useAssetCategories()
  const locations = useAssetLocations()

  const assetQuery = useQuery({
    queryKey: assetsKeys.asset(assetId as number),
    queryFn: ({ signal }) => assetsApi.getAsset(assetId as number, signal),
    enabled: mode === 'edit' && assetId != null,
  })

  const title =
    mode === 'create' ? t('assetEditor.createTitle') : t('assetEditor.editTitle')
  const description =
    mode === 'create'
      ? t('assetEditor.createDescription')
      : t('assetEditor.editDescription')

  if (
    categories.data == null ||
    locations.data == null ||
    (mode === 'edit' && assetQuery.isPending)
  ) {
    return (
      <Dialog
        scrollable
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
      >
        <div className="seg-assets-editor__status">
          <Spinner />
          <span>{t('assetEditor.loading')}</span>
        </div>
      </Dialog>
    )
  }

  if (mode === 'edit' && assetQuery.isError) {
    const notFound = isApiError(assetQuery.error) && assetQuery.error.kind === 'not-found'
    return (
      <Dialog
        width={760}
        title={title}
        onClose={onClose}
        closeLabel={t('editor.actions.cancel')}
        footer={<Button onClick={onClose}>{t('editor.actions.cancel')}</Button>}
      >
        <p className="seg-assets-editor__error" role="alert">
          {notFound ? t('assetEditor.notFound') : t('assetEditor.loadError')}
        </p>
      </Dialog>
    )
  }

  const asset = mode === 'edit' ? (assetQuery.data as Asset) : undefined
  const initialValues =
    asset != null
      ? fromAsset(asset)
      : buildDefaults({
          categoryId: firstCatalogId(categories.data),
          locationId: firstCatalogId(locations.data),
        })
  const canChangeVisibility =
    asset == null || (currentUserId != null && asset.createdById === currentUserId)

  return (
    <AssetEditorForm
      mode={mode}
      assetId={assetId}
      asset={asset}
      title={title}
      description={description}
      initialValues={initialValues}
      categories={categories.data}
      locations={locations.data}
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

interface AssetEditorFormProps {
  mode: 'create' | 'edit'
  assetId?: number
  asset?: Asset
  title: string
  description: string
  initialValues: AssetFormValues
  categories: ReadonlyArray<{ id: number; name: string }>
  locations: ReadonlyArray<{ id: number; name: string }>
  canChangeVisibility: boolean
  onClose: () => void
  onSaved: (asset: Asset, mode: 'create' | 'edit') => void
  onDeleted: (asset: Asset) => void
}

const statuses: AssetStatus[] = ['Active', 'Stored', 'Retired']
const visibilities: AssetVisibility[] = ['Public', 'Private']

const visibilityMeta: Record<
  AssetVisibility,
  { icon: ReactNode; tone: SegmentTone }
> = {
  Public: { icon: <Globe size={15} />, tone: 'accent' },
  Private: { icon: <Lock size={15} />, tone: 'neutral' },
}

function AssetEditorForm({
  mode,
  assetId,
  asset,
  title,
  description,
  initialValues,
  categories,
  locations,
  canChangeVisibility,
  onClose,
  onSaved,
  onDeleted,
}: AssetEditorFormProps) {
  const { t } = useTranslation('assets')
  const schema = useMemo(
    () =>
      createAssetSchema({
        nameRequired: t('assetEditor.validation.nameRequired'),
        nameTooLong: t('assetEditor.validation.nameTooLong'),
        categoryRequired: t('assetEditor.validation.categoryRequired'),
        locationRequired: t('assetEditor.validation.locationRequired'),
        codeTooLong: t('assetEditor.validation.codeTooLong'),
        brandModelTooLong: t('assetEditor.validation.brandModelTooLong'),
        serialNumberTooLong: t('assetEditor.validation.serialNumberTooLong'),
        dateInvalid: t('assetEditor.validation.dateInvalid'),
        notesTooLong: t('assetEditor.validation.notesTooLong'),
      }),
    [t],
  )

  const form = useForm<AssetFormValues>({
    resolver: zodResolver(schema),
    defaultValues: initialValues,
  })
  const { register, handleSubmit, formState } = form
  const [serverError, setServerError] = useState<string | null>(null)
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [stagedFiles, setStagedFiles] = useState<File[]>([])
  const [createdAsset, setCreatedAsset] = useState<Asset | null>(null)
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (request: CreateAssetRequest) =>
      mode === 'create'
        ? assetsApi.createAsset(request)
        : assetsApi.updateAsset(assetId as number, request),
    onSuccess: (saved) => {
      if (mode === 'create' && stagedFiles.length > 0) {
        setCreatedAsset(saved)
        return
      }
      onSaved(saved, mode)
    },
    onError: (error) => setServerError(mapServerError(error, t)),
  })

  const deleteMutation = useMutation({
    mutationFn: () => assetsApi.deleteAsset(assetId as number),
    onSuccess: () => {
      if (asset != null) onDeleted(asset)
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

  if (createdAsset != null) {
    const finish = () => onSaved(createdAsset, 'create')
    return (
      <Dialog
        scrollable
        width={760}
        title={t('editor.attachments.uploadTitle')}
        description={t('editor.attachments.uploadDescription', {
          name: createdAsset.name,
        })}
        onClose={finish}
        closeLabel={t('editor.attachments.close')}
        footer={<Button onClick={finish}>{t('editor.attachments.done')}</Button>}
      >
        <section className="seg-assets-editor__section">
          <h3>{t('editor.attachments.title')}</h3>
          <AssetAttachments assetId={createdAsset.id} autoUpload={stagedFiles} />
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
                className="seg-assets-editor__delete"
                iconLeft={<Trash2 size={15} />}
                onClick={() => setConfirmingDelete(true)}
                disabled={submitting || deleteMutation.isPending}
              >
                {t('assetEditor.delete.action')}
              </Button>
            )}
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('editor.actions.cancel')}
            </Button>
            <Button type="submit" form="seg-assets-form" disabled={submitting}>
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
          id="seg-assets-form"
          className="seg-assets-editor"
          onSubmit={submit}
          onChange={() => {
            editedRef.current = true
          }}
          noValidate
        >
          {serverError != null && (
            <p className="seg-assets-editor__error" role="alert">
              {serverError}
            </p>
          )}

          <section className="seg-assets-editor__section">
            <h3>{t('assetEditor.sections.general')}</h3>
            <Input
              label={t('assetEditor.fields.name')}
              placeholder={t('assetEditor.fields.namePlaceholder')}
              required
              error={formState.errors.name?.message}
              {...register('name')}
            />
            <div className="seg-assets-editor__grid">
              <Field label={t('assetEditor.fields.status')}>
                <Select
                  {...register('status')}
                  options={statuses.map((value) => ({
                    value,
                    label: t(`assets.status.${value}`),
                  }))}
                />
              </Field>
              <Field
                label={t('assetEditor.fields.category')}
                error={formState.errors.categoryId?.message}
              >
                <Select
                  {...register('categoryId')}
                  aria-invalid={formState.errors.categoryId != null}
                  options={catalogOptions(categories)}
                />
              </Field>
              <Field
                label={t('assetEditor.fields.location')}
                error={formState.errors.locationId?.message}
              >
                <Select
                  {...register('locationId')}
                  aria-invalid={formState.errors.locationId != null}
                  options={catalogOptions(locations)}
                />
              </Field>
              <Input
                label={t('assetEditor.fields.code')}
                placeholder={t('assetEditor.fields.codePlaceholder')}
                error={formState.errors.code?.message}
                {...register('code')}
              />
              <Input
                label={t('assetEditor.fields.brandModel')}
                error={formState.errors.brandModel?.message}
                {...register('brandModel')}
              />
              <Input
                label={t('assetEditor.fields.serialNumber')}
                error={formState.errors.serialNumber?.message}
                {...register('serialNumber')}
              />
              <Input
                label={t('assetEditor.fields.acquisitionDate')}
                type="date"
                error={formState.errors.acquisitionDate?.message}
                {...register('acquisitionDate')}
              />
              <Input
                label={t('assetEditor.fields.expectedEndOfLifeDate')}
                type="date"
                error={formState.errors.expectedEndOfLifeDate?.message}
                {...register('expectedEndOfLifeDate')}
              />
              <ToggleField
                id="assets-field-visibility"
                label={t('assetEditor.fields.visibility')}
                hint={
                  canChangeVisibility ? undefined : t('editor.visibilityHint.locked')
                }
              >
                <SegmentedControl
                  aria-labelledby="assets-field-visibility"
                  disabled={!canChangeVisibility}
                  {...register('visibility')}
                  options={visibilities.map((value) => ({
                    value,
                    label: t(`assets.visibility.${value}`),
                    icon: visibilityMeta[value].icon,
                    tone: visibilityMeta[value].tone,
                  }))}
                />
              </ToggleField>
            </div>
          </section>

          <section className="seg-assets-editor__section">
            <h3>{t('assetEditor.sections.notes')}</h3>
            <label className="seg-assets-editor__notes">
              <span className="seg-assets-editor__notes-label">
                {t('assetEditor.fields.notes')}
              </span>
              <textarea
                className="seg-assets-editor__textarea"
                rows={4}
                placeholder={t('assetEditor.fields.notesPlaceholder')}
                aria-invalid={formState.errors.notes != null}
                {...register('notes')}
              />
              {formState.errors.notes?.message != null && (
                <span className="seg-assets-editor__field-error" role="alert">
                  {formState.errors.notes.message}
                </span>
              )}
            </label>
          </section>

          <section className="seg-assets-editor__section">
            <h3>{t('editor.attachments.title')}</h3>
            <p className="seg-assets-editor__hint">{t('editor.attachments.hint')}</p>
            {mode === 'edit' && assetId != null ? (
              <AssetAttachments assetId={assetId} />
            ) : (
              <StagedAssetAttachments
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
          title={t('assetEditor.delete.title')}
          description={t('assetEditor.delete.description')}
          onClose={() => setConfirmingDelete(false)}
          closeLabel={t('assetEditor.delete.cancel')}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => setConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
              >
                {t('assetEditor.delete.cancel')}
              </Button>
              <Button
                variant="danger"
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t('assetEditor.delete.deleting')
                  : t('assetEditor.delete.confirm')}
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
    <div className="seg-assets-editor__field">
      <label className="seg-assets-editor__field-control">
        <span className="seg-assets-editor__field-label">{label}</span>
        {children}
      </label>
      {message != null && (
        <span
          className={
            'seg-assets-editor__field-hint' +
            (error != null ? ' seg-assets-editor__field-hint--error' : '')
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
    <div className="seg-assets-editor__field">
      <span className="seg-assets-editor__field-label" id={id}>
        {label}
      </span>
      {children}
      {hint != null && <span className="seg-assets-editor__field-hint">{hint}</span>}
    </div>
  )
}

function mapServerError(error: unknown, t: (key: string) => string): string {
  if (isApiError(error)) {
    switch (error.problem?.code) {
      case 'assets.asset.validation':
        return t('assetEditor.errors.validation')
      case 'assets.asset.duplicate_code':
        return t('assetEditor.errors.duplicateCode')
      case 'assets.asset.visibility_forbidden':
        return t('assetEditor.errors.visibilityForbidden')
      case 'assets.catalog.unknown_reference':
        return t('assetEditor.errors.unknownReference')
    }
    if (error.kind === 'not-found') return t('assetEditor.notFound')
    if (error.kind === 'transient' || error.kind === 'unavailable') {
      return t('assetEditor.errors.conflict')
    }
  }
  return t('assetEditor.errors.generic')
}
