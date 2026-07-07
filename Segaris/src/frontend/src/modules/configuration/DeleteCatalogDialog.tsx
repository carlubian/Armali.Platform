import { useMutation, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { CatalogReplacementRequest } from '@/app/api/catalogs'
import { isApiError } from '@/app/api/errors'
import { Button, Dialog, Input, Select, Spinner } from '@/components/ui'

import type { CatalogDescriptor, CatalogRow } from './catalogs'

export interface DeleteCatalogDialogProps {
  descriptor: CatalogDescriptor
  /** The value being removed. */
  row: CatalogRow
  /** All current rows, used to offer replacement candidates. */
  rows: CatalogRow[]
  onClose: () => void
  onDeleted: (row: CatalogRow) => void
}

type Strategy = 'replace' | 'clear'

/**
 * Impact-driven removal dialog. It first asks the privacy-neutral deletion-impact
 * endpoint (which never reveals counts or record details), then offers only the
 * paths that are actually available: a direct delete for unreferenced values, or
 * replace/clear for referenced ones. A referenced currency takes the conversion
 * path: it is converted to another currency with an explicit exchange rate.
 */
export function DeleteCatalogDialog({
  descriptor,
  row,
  rows,
  onClose,
  onDeleted,
}: DeleteCatalogDialogProps) {
  const { t } = useTranslation('configuration')
  const [strategy, setStrategy] = useState<Strategy | null>(null)
  const [replacementId, setReplacementId] = useState<string>('')
  const [replacementError, setReplacementError] = useState<string | null>(null)
  const [rate, setRate] = useState<string>('')
  const [rateError, setRateError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const candidates = rows.filter((candidate) => candidate.id !== row.id)

  const impactQuery = useQuery({
    queryKey: ['configuration', 'impact', descriptor.key, row.id] as const,
    queryFn: ({ signal }) => descriptor.management.deletionImpact(row.id, signal),
    staleTime: 0,
    gcTime: 0,
  })

  const removeMutation = useMutation({
    mutationFn: (body: CatalogReplacementRequest | null) =>
      body == null
        ? descriptor.management.remove(row.id)
        : descriptor.management.replaceAndDelete(row.id, body),
    onSuccess: () => onDeleted(row),
    onError: (error) => applyError(error),
  })

  const applyError = (error: unknown) => {
    if (isApiError(error)) {
      const code = error.problem?.code ?? ''
      if (code.endsWith('invalid_replacement')) {
        setReplacementError(t('remove.replacementRequired'))
        return
      }
      if (code.endsWith('required_not_empty')) {
        setActionError(t('remove.requiredMin'))
        return
      }
      if (code.endsWith('migration_conflict')) {
        setActionError(t('remove.conflict'))
        return
      }
      if (
        code.endsWith('exchange_rate_required') ||
        code.endsWith('exchange_rate_invalid')
      ) {
        setRateError(t('remove.convertRateInvalid'))
        return
      }
    }
    setActionError(t('remove.error'))
  }

  const cancelFooterButton = (label: string) => (
    <Button variant="ghost" onClick={onClose} disabled={removeMutation.isPending}>
      {label}
    </Button>
  )

  // Loading and error states keep the dialog chrome so focus stays trapped.
  if (impactQuery.isPending) {
    return (
      <Dialog
        width={460}
        title={t('remove.directTitle', { name: row.name })}
        onClose={onClose}
        closeLabel={t('remove.cancel')}
      >
        <div className="seg-catalog__dialog-status">
          <Spinner />
        </div>
      </Dialog>
    )
  }

  if (impactQuery.isError) {
    return (
      <Dialog
        width={460}
        title={t('remove.directTitle', { name: row.name })}
        onClose={onClose}
        closeLabel={t('remove.cancel')}
        footer={cancelFooterButton(t('remove.close'))}
      >
        <p className="seg-catalog__form-error" role="alert">
          {t('remove.error')}
        </p>
      </Dialog>
    )
  }

  const impact = impactQuery.data

  // A referenced currency cannot be cleared or deleted directly: every entry that
  // uses it must be converted to another currency with an explicit exchange rate.
  // The conversion is irreversible and recalculates the affected amounts.
  if (descriptor.isCurrency && impact.requiresExchangeRate) {
    if (candidates.length === 0) {
      return (
        <Dialog
          width={460}
          title={t('remove.referencedTitle', { name: row.name })}
          onClose={onClose}
          closeLabel={t('remove.close')}
          footer={<Button onClick={onClose}>{t('remove.close')}</Button>}
        >
          <p className="seg-catalog__form-error" role="alert">
            {t('remove.noCandidates')}
          </p>
        </Dialog>
      )
    }

    const target = candidates.find(
      (candidate) => String(candidate.id) === replacementId,
    )
    const submitConversion = () => {
      setActionError(null)
      setReplacementError(null)
      setRateError(null)
      if (replacementId === '') {
        setReplacementError(t('remove.convertTargetRequired'))
        return
      }
      const parsedRate = parseExchangeRate(rate)
      if (parsedRate == null) {
        setRateError(t('remove.convertRateInvalid'))
        return
      }
      removeMutation.mutate({
        replacementId: Number(replacementId),
        clearReferences: false,
        exchangeRate: parsedRate,
      })
    }

    return (
      <Dialog
        width={520}
        title={t('remove.convertTitle', { name: row.name })}
        description={t('remove.convertDescription')}
        onClose={onClose}
        closeLabel={t('remove.cancel')}
        footer={
          <>
            {cancelFooterButton(t('remove.cancel'))}
            <Button
              variant="danger"
              disabled={removeMutation.isPending}
              onClick={submitConversion}
            >
              {removeMutation.isPending
                ? t('remove.deleting')
                : t('remove.convertConfirm')}
            </Button>
          </>
        }
      >
        {actionError != null && (
          <p className="seg-catalog__form-error" role="alert">
            {actionError}
          </p>
        )}
        <div className="seg-catalog__field">
          <label className="seg-catalog__field-control">
            <span className="seg-catalog__field-label">
              {t('remove.convertTargetLabel')}
            </span>
            <Select
              value={replacementId}
              aria-invalid={replacementError != null}
              onChange={(event) => {
                setReplacementId(event.target.value)
                setReplacementError(null)
              }}
              options={[
                { value: '', label: t('remove.convertTargetPlaceholder') },
                ...candidates.map((candidate) => ({
                  value: String(candidate.id),
                  label: `${candidate.name} (${candidate.code})`,
                })),
              ]}
            />
          </label>
          {replacementError != null && (
            <span className="seg-catalog__field-error" role="alert">
              {replacementError}
            </span>
          )}
        </div>
        <Input
          label={t('remove.convertRateLabel')}
          type="text"
          inputMode="decimal"
          autoComplete="off"
          value={rate}
          error={rateError ?? undefined}
          onChange={(event) => {
            setRate(event.target.value)
            setRateError(null)
          }}
        />
        <p className="seg-catalog__convert-formula">
          {t('remove.convertFormula', {
            source: row.code ?? row.name,
            rate: rate.trim() === '' ? '…' : rate.trim(),
            target: target?.code ?? '…',
          })}
        </p>
        <p className="seg-catalog__form-hint">{t('remove.convertIrreversible')}</p>
      </Dialog>
    )
  }

  // Unreferenced and removable: a plain confirmation.
  if (impact.canDeleteDirectly) {
    return (
      <Dialog
        width={460}
        title={t('remove.directTitle', { name: row.name })}
        description={t('remove.directDescription')}
        onClose={onClose}
        closeLabel={t('remove.cancel')}
        footer={
          <>
            {cancelFooterButton(t('remove.cancel'))}
            <Button
              variant="danger"
              disabled={removeMutation.isPending}
              onClick={() => {
                setActionError(null)
                removeMutation.mutate(null)
              }}
            >
              {removeMutation.isPending ? t('remove.deleting') : t('remove.confirm')}
            </Button>
          </>
        }
      >
        {actionError != null && (
          <p className="seg-catalog__form-error" role="alert">
            {actionError}
          </p>
        )}
      </Dialog>
    )
  }

  const canReplace = impact.hasReplacementCandidates && candidates.length > 0
  const canClear = impact.canClearReferences && descriptor.canClear

  // Referenced but no available path (a required last value, or referenced with
  // no other candidate to replace it). Nothing can be done here.
  if (!canReplace && !canClear) {
    return (
      <Dialog
        width={460}
        title={t('remove.referencedTitle', { name: row.name })}
        onClose={onClose}
        closeLabel={t('remove.close')}
        footer={<Button onClick={onClose}>{t('remove.close')}</Button>}
      >
        <p className="seg-catalog__form-error" role="alert">
          {impact.isReferenced ? t('remove.noCandidates') : t('remove.requiredMin')}
        </p>
      </Dialog>
    )
  }

  const activeStrategy: Strategy = strategy ?? (canReplace ? 'replace' : 'clear')

  const submit = () => {
    setActionError(null)
    setReplacementError(null)
    if (activeStrategy === 'clear') {
      removeMutation.mutate({
        replacementId: null,
        clearReferences: true,
        exchangeRate: null,
      })
      return
    }
    if (replacementId === '') {
      setReplacementError(t('remove.replacementRequired'))
      return
    }
    removeMutation.mutate({
      replacementId: Number(replacementId),
      clearReferences: false,
      exchangeRate: null,
    })
  }

  return (
    <Dialog
      width={480}
      title={t('remove.referencedTitle', { name: row.name })}
      description={t('remove.referencedDescription')}
      onClose={onClose}
      closeLabel={t('remove.cancel')}
      footer={
        <>
          {cancelFooterButton(t('remove.cancel'))}
          <Button variant="danger" disabled={removeMutation.isPending} onClick={submit}>
            {removeMutation.isPending ? t('remove.deleting') : t('remove.confirm')}
          </Button>
        </>
      }
    >
      {actionError != null && (
        <p className="seg-catalog__form-error" role="alert">
          {actionError}
        </p>
      )}
      <fieldset className="seg-catalog__strategy">
        <legend>{t('remove.strategyLabel')}</legend>
        {canReplace && (
          <label className="seg-catalog__strategy-option">
            <input
              type="radio"
              name="seg-catalog-strategy"
              value="replace"
              checked={activeStrategy === 'replace'}
              onChange={() => setStrategy('replace')}
            />
            <span>{t('remove.replaceOption')}</span>
          </label>
        )}
        {canClear && (
          <label className="seg-catalog__strategy-option">
            <input
              type="radio"
              name="seg-catalog-strategy"
              value="clear"
              checked={activeStrategy === 'clear'}
              onChange={() => setStrategy('clear')}
            />
            <span>{t('remove.clearOption')}</span>
          </label>
        )}
      </fieldset>

      {activeStrategy === 'replace' && canReplace && (
        <div className="seg-catalog__field">
          <label className="seg-catalog__field-control">
            <span className="seg-catalog__field-label">
              {t('remove.replacementLabel')}
            </span>
            <Select
              value={replacementId}
              aria-invalid={replacementError != null}
              onChange={(event) => {
                setReplacementId(event.target.value)
                setReplacementError(null)
              }}
              options={[
                { value: '', label: t('remove.replacementPlaceholder') },
                ...candidates.map((candidate) => ({
                  value: String(candidate.id),
                  label: descriptor.hasCode
                    ? `${candidate.name} (${candidate.code})`
                    : descriptor.hasPlatform && candidate.platform != null
                      ? `${candidate.name} (${t(`games:platform.${candidate.platform}`)})`
                      : candidate.name,
                })),
              ]}
            />
          </label>
          {replacementError != null && (
            <span className="seg-catalog__field-error" role="alert">
              {replacementError}
            </span>
          )}
        </div>
      )}
    </Dialog>
  )
}

/**
 * Parses a user-entered exchange rate, accepting only a positive decimal with at
 * most eight fractional digits — the same bound the server enforces. Returns the
 * numeric value, or `null` when the input is empty, non-numeric, non-positive, or
 * too precise.
 */
function parseExchangeRate(value: string): number | null {
  const trimmed = value.trim()
  if (!/^\d*\.?\d+$/.test(trimmed)) {
    return null
  }
  const fractional = trimmed.includes('.') ? trimmed.split('.')[1].length : 0
  if (fractional > 8) {
    return null
  }
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}
