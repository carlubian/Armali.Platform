import { useMutation, useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { CatalogReplacementRequest } from '@/app/api/catalogs'
import { isApiError } from '@/app/api/errors'
import { Button, Dialog, Select, Spinner } from '@/components/ui'

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
 * replace/clear for referenced ones. A referenced currency is cleanly blocked
 * until the exchange-rate conversion path ships.
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

  // A referenced currency needs the exchange-rate conversion path (Wave 5).
  if (descriptor.isCurrency && impact.requiresExchangeRate) {
    return (
      <Dialog
        width={460}
        title={t('remove.currencyBlockedTitle')}
        description={t('remove.currencyBlockedDescription')}
        onClose={onClose}
        closeLabel={t('remove.close')}
        footer={<Button onClick={onClose}>{t('remove.close')}</Button>}
      />
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
