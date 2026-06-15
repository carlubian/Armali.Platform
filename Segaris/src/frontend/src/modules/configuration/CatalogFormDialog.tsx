import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { isApiError } from '@/app/api/errors'
import { Button, Dialog, Input } from '@/components/ui'

import type { CatalogDescriptor, CatalogRow, CatalogWriteBody } from './catalogs'

export interface CatalogFormDialogProps {
  descriptor: CatalogDescriptor
  mode: 'create' | 'edit'
  /** The row being edited; omitted when creating. */
  row?: CatalogRow
  onClose: () => void
  onSaved: (row: CatalogRow, mode: 'create' | 'edit') => void
}

type TFunc = ReturnType<typeof useTranslation<'configuration'>>['t']

function buildSchema(t: TFunc, hasCode: boolean) {
  const name = z
    .string()
    .trim()
    .min(1, t('form.nameRequired'))
    .max(100, t('form.nameTooLong'))
  if (!hasCode) return z.object({ name })
  return z.object({
    name,
    code: z
      .string()
      .trim()
      .transform((value) => value.toUpperCase())
      .pipe(z.string().regex(/^[A-Z]{3}$/, t('form.codeInvalid'))),
  })
}

type FormValues = { name: string; code?: string }

/**
 * Creation and editing dialog shared by the four catalogs. Currency adds the
 * three-letter code field. Closing with unsaved edits asks for confirmation, and
 * structured server validation errors map back onto the originating field.
 */
export function CatalogFormDialog({
  descriptor,
  mode,
  row,
  onClose,
  onSaved,
}: CatalogFormDialogProps) {
  const { t } = useTranslation('configuration')
  const labels = `catalogs.${descriptor.key}` as const
  const [confirmingClose, setConfirmingClose] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const form = useForm<FormValues>({
    resolver: zodResolver(buildSchema(t, descriptor.hasCode)),
    defaultValues: {
      name: row?.name ?? '',
      code: row?.code ?? '',
    },
  })
  const { register, handleSubmit, formState, setError } = form
  // Edits are tracked with a ref rather than by subscribing to `formState.isDirty`
  // during render. Subscribing would re-render on every keystroke, and the Dialog
  // refocuses its panel whenever its `onClose` handler identity changes on
  // re-render, which would interrupt typing.
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body: CatalogWriteBody = descriptor.hasCode
        ? { name: values.name.trim(), code: (values.code ?? '').trim().toUpperCase() }
        : { name: values.name.trim() }
      return mode === 'create'
        ? descriptor.management.create(body)
        : descriptor.management.update(row!.id, body)
    },
    onSuccess: (saved) => onSaved(saved, mode),
    onError: (error) => applyServerError(error),
  })

  const applyServerError = (error: unknown) => {
    if (isApiError(error)) {
      const code = error.problem?.code ?? ''
      if (code.endsWith('duplicate_name')) {
        setError('name', { message: t('form.duplicateName') })
        return
      }
      if (code.endsWith('duplicate_code')) {
        setError('code', { message: t('form.duplicateCode') })
        return
      }
      if (code.endsWith('invalid_code')) {
        setError('code', { message: t('form.codeInvalid') })
        return
      }
    }
    setServerError(t('form.genericError'))
  }

  const submit = handleSubmit((values) => {
    setServerError(null)
    mutation.mutate(values)
  })

  const requestClose = () => {
    if (editedRef.current && !mutation.isSuccess) {
      setConfirmingClose(true)
      return
    }
    onClose()
  }

  const title =
    mode === 'create' ? t(`${labels}.createTitle`) : t(`${labels}.editTitle`)
  const submitting = mutation.isPending

  return (
    <>
      <Dialog
        width={460}
        title={title}
        onClose={requestClose}
        closeLabel={t('form.close')}
        footer={
          <>
            <Button variant="ghost" onClick={requestClose} disabled={submitting}>
              {t('form.cancel')}
            </Button>
            <Button
              type="submit"
              form="seg-catalog-form"
              variant="primary"
              disabled={submitting}
            >
              {mode === 'create'
                ? submitting
                  ? t('form.creating')
                  : t('form.create')
                : submitting
                  ? t('form.saving')
                  : t('form.save')}
            </Button>
          </>
        }
      >
        <form
          id="seg-catalog-form"
          className="seg-catalog__form"
          noValidate
          onSubmit={(event) => void submit(event)}
          onChange={() => {
            editedRef.current = true
          }}
        >
          {serverError != null && (
            <p className="seg-catalog__form-error" role="alert">
              {serverError}
            </p>
          )}
          <Input
            label={t(`${labels}.nameLabel`)}
            placeholder={t(`${labels}.namePlaceholder`)}
            autoComplete="off"
            required
            error={formState.errors.name?.message}
            {...register('name')}
          />
          {descriptor.hasCode && (
            <Input
              label={t(`${labels}.codeLabel`)}
              placeholder={t(`${labels}.codePlaceholder`)}
              autoComplete="off"
              maxLength={3}
              required
              error={formState.errors.code?.message}
              {...register('code')}
            />
          )}
        </form>
      </Dialog>

      {confirmingClose && (
        <Dialog
          width={420}
          title={t('unsaved.title')}
          description={t('unsaved.description')}
          onClose={() => setConfirmingClose(false)}
          closeLabel={t('unsaved.stay')}
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmingClose(false)}>
                {t('unsaved.stay')}
              </Button>
              <Button variant="danger" onClick={onClose}>
                {t('unsaved.leave')}
              </Button>
            </>
          }
        />
      )}
    </>
  )
}
