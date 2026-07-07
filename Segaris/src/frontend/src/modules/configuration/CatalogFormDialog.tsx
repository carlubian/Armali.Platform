import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useController, useForm, useWatch, type Control } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { isApiError } from '@/app/api/errors'
import { gamePlatforms, type GamePlatform } from '@/app/api/games'
import { Button, Dialog, Input, Select } from '@/components/ui'

import type { CatalogDescriptor, CatalogRow, CatalogWriteBody } from './catalogs'

/** Hex pattern shared by the schema, swatch fallback, and server-error mapping. */
const hexColor = /^#[0-9A-Fa-f]{6}$/

/** The EUR currency code is fixed at an exchange rate of 1. */
const euroCode = 'EUR'

/** A positive decimal with at most eight fractional digits, mirroring the server. */
const exchangeRatePattern = /^\d+(\.\d{1,8})?$/

export interface CatalogFormDialogProps {
  descriptor: CatalogDescriptor
  mode: 'create' | 'edit'
  /** The row being edited; omitted when creating. */
  row?: CatalogRow
  onClose: () => void
  onSaved: (row: CatalogRow, mode: 'create' | 'edit') => void
}

type TFunc = ReturnType<typeof useTranslation<'configuration'>>['t']

function buildSchema(t: TFunc, descriptor: CatalogDescriptor) {
  const name = z
    .string()
    .trim()
    .min(1, t('form.nameRequired'))
    .max(100, t('form.nameTooLong'))
  if (descriptor.hasCode) {
    const code = z
      .string()
      .trim()
      .transform((value) => value.toUpperCase())
      .pipe(z.string().regex(/^[A-Z]{3}$/, t('form.codeInvalid')))
    const exchangeRateToEur = z
      .string()
      .trim()
      .min(1, t('form.rateRequired'))
      .refine(
        (value) => exchangeRatePattern.test(value) && Number(value) > 0,
        t('form.rateInvalid'),
      )
    return z
      .object({ name, code, exchangeRateToEur })
      .refine(
        (value) => value.code !== euroCode || Number(value.exchangeRateToEur) === 1,
        { path: ['exchangeRateToEur'], message: t('form.rateMustBeOne') },
      )
  }
  if (descriptor.hasColorValue) {
    return z.object({
      name,
      colorValue: z.string().trim().regex(hexColor, t('form.colorInvalid')),
    })
  }
  if (descriptor.hasPlatform) {
    return z.object({
      name,
      platform: z.enum(gamePlatforms as [GamePlatform, ...GamePlatform[]]),
    })
  }
  return z.object({ name })
}

type FormValues = {
  name: string
  code?: string
  colorValue?: string
  exchangeRateToEur?: string
  platform?: GamePlatform
}

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
    resolver: zodResolver(buildSchema(t, descriptor)),
    defaultValues: {
      name: row?.name ?? '',
      code: row?.code ?? '',
      colorValue: row?.colorValue ?? '#000000',
      exchangeRateToEur:
        row?.exchangeRateToEur != null ? String(row.exchangeRateToEur) : '',
      platform: row?.platform ?? gamePlatforms[0],
    },
  })
  const { register, handleSubmit, formState, setError, control } = form
  // Edits are tracked with a ref rather than by subscribing to `formState.isDirty`
  // during render. Subscribing would re-render on every keystroke, and the Dialog
  // refocuses its panel whenever its `onClose` handler identity changes on
  // re-render, which would interrupt typing.
  const editedRef = useRef(false)

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body: CatalogWriteBody = descriptor.hasCode
        ? {
            name: values.name.trim(),
            code: (values.code ?? '').trim().toUpperCase(),
            exchangeRateToEur: Number((values.exchangeRateToEur ?? '').trim()),
          }
        : descriptor.hasColorValue
          ? { name: values.name.trim(), colorValue: (values.colorValue ?? '').trim() }
          : descriptor.hasPlatform
            ? { name: values.name.trim(), platform: values.platform }
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
      if (code.endsWith('exchange_rate_required')) {
        setError('exchangeRateToEur', { message: t('form.rateRequired') })
        return
      }
      if (code.endsWith('exchange_rate_not_one')) {
        setError('exchangeRateToEur', { message: t('form.rateMustBeOne') })
        return
      }
      if (code.endsWith('exchange_rate_invalid')) {
        setError('exchangeRateToEur', { message: t('form.rateInvalid') })
        return
      }
      // The colour value is validated on the client; this guards the rare case
      // where the server still rejects it (the failure is field-keyed).
      if (descriptor.hasColorValue && error.problem?.errors?.colorValue != null) {
        setError('colorValue', { message: t('form.colorInvalid') })
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
          {descriptor.hasCode && (
            <ExchangeRateField
              control={control}
              label={t(`${labels}.rateLabel`)}
              placeholder={t(`${labels}.ratePlaceholder`)}
              euroHint={t(`${labels}.rateEuroHint`)}
              error={formState.errors.exchangeRateToEur?.message}
            />
          )}
          {descriptor.hasColorValue && (
            <ColorField
              control={control}
              label={t(`${labels}.colorLabel`)}
              pickerLabel={t(`${labels}.colorPickerLabel`)}
              error={formState.errors.colorValue?.message}
            />
          )}
          {descriptor.hasPlatform && (
            <PlatformField control={control} label={t(`${labels}.platformLabel`)} />
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

interface ExchangeRateFieldProps {
  control: Control<FormValues>
  label: string
  placeholder: string
  /** Shown instead of the editable hint when the code is EUR and the field locks. */
  euroHint: string
  error?: string
}

/**
 * Exchange-rate-to-EUR sub-editor for currencies. It owns its own controlled
 * subscription so rate edits re-render only this component, keeping the Dialog's
 * `onClose` identity stable and focus uninterrupted while typing. When the code is
 * EUR the rate is fixed at 1, so the field locks to `1` and is disabled.
 */
function ExchangeRateField({
  control,
  label,
  placeholder,
  euroHint,
  error,
}: ExchangeRateFieldProps) {
  const { field } = useController({ name: 'exchangeRateToEur', control })
  const code = useWatch({ control, name: 'code' }) ?? ''
  const isEuro = code.trim().toUpperCase() === euroCode

  // Keep the stored value aligned with the locked display so submission always
  // sends 1 for EUR, even if a non-EUR rate was typed before switching the code.
  useEffect(() => {
    if (isEuro && field.value !== '1') {
      field.onChange('1')
    }
  }, [isEuro, field])

  const value = typeof field.value === 'string' ? field.value : ''
  return (
    <Input
      label={label}
      type="text"
      inputMode="decimal"
      autoComplete="off"
      placeholder={placeholder}
      disabled={isEuro}
      required={!isEuro}
      value={isEuro ? '1' : value}
      hint={isEuro ? euroHint : undefined}
      error={error}
      onChange={(event) => field.onChange(event.target.value)}
      onBlur={field.onBlur}
    />
  )
}

interface ColorFieldProps {
  control: Control<FormValues>
  label: string
  pickerLabel: string
  error?: string
}

/**
 * Colour value sub-editor for `ClothingColor`: a native swatch picker beside a
 * hex text field, both bound to the same `colorValue` field. It owns its
 * controlled subscription through `useController` so colour edits re-render only
 * this component, leaving the surrounding Dialog's `onClose` identity stable and
 * keyboard focus uninterrupted while typing.
 */
function ColorField({ control, label, pickerLabel, error }: ColorFieldProps) {
  const { field } = useController({ name: 'colorValue', control })
  const value = typeof field.value === 'string' ? field.value : ''
  const swatch = hexColor.test(value) ? value : '#000000'
  return (
    <div className="seg-catalog__color-field">
      <input
        type="color"
        className="seg-catalog__color-picker"
        aria-label={pickerLabel}
        value={swatch}
        onChange={(event) => field.onChange(event.target.value)}
      />
      <Input
        className="seg-catalog__color-hex"
        label={label}
        placeholder="#000000"
        autoComplete="off"
        maxLength={7}
        required
        value={value}
        error={error}
        onChange={(event) => field.onChange(event.target.value)}
        onBlur={field.onBlur}
      />
    </div>
  )
}

interface PlatformFieldProps {
  control: Control<FormValues>
  label: string
}

/**
 * Fixed-platform selector for `Game`. Every game has exactly one platform from the
 * frozen vocabulary, so this is a plain required select. Platform labels are the
 * canonical Games ones, reused across the `games` namespace to avoid divergence. It
 * owns its own controlled subscription so selecting a platform re-renders only this
 * component, leaving the Dialog's `onClose` identity stable.
 */
function PlatformField({ control, label }: PlatformFieldProps) {
  const { t } = useTranslation('games')
  const { field } = useController({ name: 'platform', control })
  const value = typeof field.value === 'string' ? field.value : gamePlatforms[0]
  return (
    <div className="seg-catalog__field">
      <label className="seg-catalog__field-control">
        <span className="seg-catalog__field-label">{label}</span>
        <Select
          value={value}
          onChange={(event) => field.onChange(event.target.value)}
          onBlur={field.onBlur}
          options={gamePlatforms.map((platform) => ({
            value: platform,
            label: t(`platform.${platform}`),
          }))}
        />
      </label>
    </div>
  )
}
