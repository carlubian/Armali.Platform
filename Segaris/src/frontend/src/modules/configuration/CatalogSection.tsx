import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { CatalogMoveDirection } from '@/app/api/catalogs'
import { isApiError } from '@/app/api/errors'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Button, Spinner } from '@/components/ui'

import { CatalogFormDialog } from './CatalogFormDialog'
import { CatalogTable, type FocusRequest } from './CatalogTable'
import { DeleteCatalogDialog } from './DeleteCatalogDialog'
import type { CatalogDescriptor, CatalogRow } from './catalogs'
import { invalidateCatalog } from './queries'

export type CatalogToastKind = 'created' | 'updated' | 'removed'

export interface CatalogSectionProps {
  descriptor: CatalogDescriptor
  onToast: (kind: CatalogToastKind, name: string) => void
}

type FormState = { mode: 'create' | 'edit'; row?: CatalogRow }

/**
 * One catalog's full management surface: the deterministic table plus the
 * creation, editing, reordering, and removal workflows. It reads through the same
 * cache key the business forms use and invalidates it (and the known Capex
 * queries when relevant) after every successful mutation.
 */
export function CatalogSection({ descriptor, onToast }: CatalogSectionProps) {
  const { t } = useTranslation('configuration')
  const queryClient = useQueryClient()
  const labels = `catalogs.${descriptor.key}` as const

  const [formState, setFormState] = useState<FormState | null>(null)
  const [deleteRow, setDeleteRow] = useState<CatalogRow | null>(null)
  const [focusRequest, setFocusRequest] = useState<FocusRequest | null>(null)
  const [moveError, setMoveError] = useState<string | null>(null)

  const listQuery = useQuery({
    queryKey: descriptor.queryKey,
    queryFn: ({ signal }) => descriptor.read(signal),
  })

  const moveMutation = useMutation({
    mutationFn: ({
      row,
      direction,
    }: {
      row: CatalogRow
      direction: CatalogMoveDirection
    }) => descriptor.management.move(row.id, direction),
    onSuccess: () =>
      invalidateCatalog(queryClient, descriptor, { affectsEntries: false }),
    onError: () => setMoveError(t('table.moveError')),
  })

  const rows = listQuery.data ?? []

  if (listQuery.isError) {
    const error = listQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void listQuery.refetch()} />
    }
  }

  const handleMove = (row: CatalogRow, direction: CatalogMoveDirection) => {
    if (moveMutation.isPending) return
    setMoveError(null)
    setFocusRequest({ id: row.id, direction })
    moveMutation.mutate({ row, direction })
  }

  const handleSaved = (saved: CatalogRow, mode: 'create' | 'edit') => {
    void invalidateCatalog(queryClient, descriptor, {
      affectsEntries: mode === 'edit' || descriptor.hasWellnessCategory === true,
    })
    setFormState(null)
    onToast(mode === 'create' ? 'created' : 'updated', saved.name)
  }

  const handleDeleted = (removed: CatalogRow) => {
    void invalidateCatalog(queryClient, descriptor, { affectsEntries: true })
    setDeleteRow(null)
    onToast('removed', removed.name)
  }

  return (
    <section className="seg-catalog" aria-label={t(`${labels}.title`)}>
      <header className="seg-catalog__head">
        <div>
          <h2>{t(`${labels}.title`)}</h2>
          <p>{t(`${labels}.description`)}</p>
        </div>
        <Button
          iconLeft={<Plus size={16} />}
          onClick={() => setFormState({ mode: 'create' })}
        >
          {t(`${labels}.addAction`)}
        </Button>
      </header>

      {moveError != null && (
        <p className="seg-catalog__form-error" role="alert">
          {moveError}
        </p>
      )}

      {listQuery.isPending ? (
        <div className="seg-catalog__loading">
          <Spinner />
        </div>
      ) : listQuery.isError ? (
        <div className="seg-catalog__error" role="alert">
          <p>{t('table.loadError')}</p>
          <Button variant="outline" size="sm" onClick={() => void listQuery.refetch()}>
            {t('table.retry')}
          </Button>
        </div>
      ) : rows.length === 0 ? (
        <p className="seg-catalog__empty">{t(`${labels}.empty`)}</p>
      ) : (
        <CatalogTable
          descriptor={descriptor}
          rows={rows}
          busy={moveMutation.isPending}
          focusRequest={focusRequest}
          onFocusHandled={() => setFocusRequest(null)}
          onEdit={(row) => setFormState({ mode: 'edit', row })}
          onDelete={(row) => setDeleteRow(row)}
          onMove={handleMove}
        />
      )}

      {formState != null && (
        <CatalogFormDialog
          descriptor={descriptor}
          mode={formState.mode}
          row={formState.row}
          onClose={() => setFormState(null)}
          onSaved={handleSaved}
        />
      )}

      {deleteRow != null && (
        <DeleteCatalogDialog
          descriptor={descriptor}
          row={deleteRow}
          rows={rows}
          onClose={() => setDeleteRow(null)}
          onDeleted={handleDeleted}
        />
      )}
    </section>
  )
}
