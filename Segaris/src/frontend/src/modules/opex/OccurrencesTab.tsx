import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { opexApi } from '@/app/api/opex'
import { formatCurrency, formatDate } from '@/app/i18n/formatters'
import { Badge, Button, Spinner } from '@/components/ui'

import { OccurrenceDialog } from './OccurrenceDialog'
import { opexKeys } from './queries'

const OCCURRENCE_PAGE_SIZE = 10

type OccurrenceDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; occurrenceId: number }

export interface OccurrencesTabProps {
  contractId: number
  currencyCode: string
}

export function OccurrencesTab({ contractId, currencyCode }: OccurrencesTabProps) {
  const { t, i18n } = useTranslation('opex')
  const [page, setPage] = useState(1)
  const [dialog, setDialog] = useState<OccurrenceDialogState>({ mode: 'closed' })

  const query = useQuery({
    queryKey: opexKeys.occurrenceList(contractId, {
      page,
      pageSize: OCCURRENCE_PAGE_SIZE,
    }),
    queryFn: ({ signal }) =>
      opexApi.listOccurrences(
        contractId,
        { page, pageSize: OCCURRENCE_PAGE_SIZE },
        signal,
      ),
    placeholderData: keepPreviousData,
  })

  const data = query.data
  const occurrences = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / OCCURRENCE_PAGE_SIZE))

  const handleSaved = () => {
    setDialog({ mode: 'closed' })
  }

  const handleDeleted = () => {
    setDialog({ mode: 'closed' })
  }

  return (
    <div className="seg-opex-occurrences">
      <div className="seg-opex-occurrences__head">
        <Badge tone="neutral">
          {t('occurrences.count', { count: totalCount })}
        </Badge>
        <Button
          size="sm"
          iconLeft={<Plus size={15} />}
          onClick={() => setDialog({ mode: 'create' })}
        >
          {t('occurrences.newOccurrence')}
        </Button>
      </div>

      {query.isPending ? (
        <div className="seg-opex-occurrences__status">
          <Spinner />
        </div>
      ) : query.isError ? (
        <p className="seg-opex-occurrences__message" role="alert">
          {t('occurrences.states.loadError')}
        </p>
      ) : occurrences.length === 0 ? (
        <p className="seg-opex-occurrences__message">
          {t('occurrences.states.empty')}
        </p>
      ) : (
        <table className="seg-opex-occurrences__table" aria-busy={query.isFetching}>
          <thead>
            <tr>
              <th scope="col">{t('occurrences.columns.date')}</th>
              <th scope="col" className="seg-opex-occurrences__amount-col">
                {t('occurrences.columns.amount')}
              </th>
              <th scope="col">{t('occurrences.columns.description')}</th>
            </tr>
          </thead>
          <tbody>
            {occurrences.map((occurrence) => (
              <tr
                key={occurrence.id}
                className="seg-opex-occurrences__row"
                onClick={() => setDialog({ mode: 'edit', occurrenceId: occurrence.id })}
                aria-label={t('occurrences.openRow', {
                  date: formatDate(occurrence.effectiveDate, i18n.language),
                })}
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    setDialog({ mode: 'edit', occurrenceId: occurrence.id })
                  }
                }}
                role="button"
              >
                <td>{formatDate(occurrence.effectiveDate, i18n.language)}</td>
                <td className="seg-opex-occurrences__amount-col">
                  {formatCurrency(occurrence.actualAmount, currencyCode, i18n.language)}
                </td>
                <td className="seg-opex-occurrences__description">
                  {occurrence.description ?? (
                    <span className="seg-opex-occurrences__none">{t('contracts.none')}</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {totalPages > 1 && (
        <nav
          className="seg-opex-occurrences__pager"
          aria-label={t('occurrences.pagination.label')}
        >
          <Button
            variant="ghost"
            size="sm"
            iconLeft={<ChevronLeft size={15} />}
            disabled={page <= 1 || query.isFetching}
            onClick={() => setPage(Math.max(1, page - 1))}
          >
            {t('occurrences.pagination.previous')}
          </Button>
          <span aria-live="polite">
            {t('occurrences.pagination.status', { page, pages: totalPages })}
          </span>
          <Button
            variant="ghost"
            size="sm"
            iconRight={<ChevronRight size={15} />}
            disabled={page >= totalPages || query.isFetching}
            onClick={() => setPage(Math.min(totalPages, page + 1))}
          >
            {t('occurrences.pagination.next')}
          </Button>
        </nav>
      )}

      {dialog.mode !== 'closed' && (
        <OccurrenceDialog
          mode={dialog.mode}
          contractId={contractId}
          occurrenceId={dialog.mode === 'edit' ? dialog.occurrenceId : undefined}
          currencyCode={currencyCode}
          onClose={() => setDialog({ mode: 'closed' })}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}
    </div>
  )
}
