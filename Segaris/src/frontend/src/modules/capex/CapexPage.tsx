import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'

import { capexApi, capexPageSizes, type CapexPageSize } from '@/app/api/capex'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Select, Spinner } from '@/components/ui'

import { EntriesFilters } from './EntriesFilters'
import { EntriesTable } from './EntriesTable'
import { activeFilterCount, useEntriesState } from './entriesState'
import { capexKeys } from './queries'

import './CapexPage.css'

export function CapexPage() {
  const { t, i18n } = useTranslation('capex')
  const { session } = useSession()
  const currentUserId = session?.userId ?? null

  const { state, listQuery, setFilters, setSort, setPage, setPageSize, clearFilters } =
    useEntriesState(currentUserId)

  const entriesQuery = useQuery({
    queryKey: capexKeys.entryList(listQuery),
    queryFn: ({ signal }) => capexApi.listEntries(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = entriesQuery.data
  const entries = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeFilterCount(state) > 0

  // Correct an out-of-range page after deletion or filtering without reloading.
  useEffect(() => {
    if (data != null && state.page > totalPages) {
      setPage(totalPages)
    }
  }, [data, state.page, totalPages, setPage])

  if (entriesQuery.isError) {
    const error = entriesQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void entriesQuery.refetch()} />
    }
  }

  const showInitialLoading = entriesQuery.isPending
  const isRefetching = entriesQuery.isFetching && !entriesQuery.isPending

  return (
    <main className="seg-capex armali-aurora">
      <section className="seg-capex__head">
        <div>
          <div className="armali-eyebrow">{t('entries.eyebrow')}</div>
          <h1>{t('entries.title')}</h1>
          <p>{t('entries.description')}</p>
        </div>
        <Badge tone="neutral">{t('entries.count', { count: totalCount })}</Badge>
      </section>

      <EntriesFilters
        state={state}
        onChange={setFilters}
        onClear={clearFilters}
        language={i18n.language}
      />

      {showInitialLoading ? (
        <div className="seg-capex__loading">
          <Spinner />
        </div>
      ) : entriesQuery.isError ? (
        <p className="seg-capex__error" role="alert">
          {t('entries.states.loadError')}
        </p>
      ) : entries.length === 0 ? (
        <p className="seg-capex__empty">
          {hasFilters ? t('entries.states.emptyFiltered') : t('entries.states.empty')}
        </p>
      ) : (
        <EntriesTable
          entries={entries}
          state={state}
          language={i18n.language}
          onSort={setSort}
          busy={isRefetching}
        />
      )}

      <nav className="seg-capex__pager" aria-label={t('entries.pagination.label')}>
        <label className="seg-capex__rows">
          <span>{t('entries.pagination.rowsPerPage')}</span>
          <Select
            value={String(state.pageSize)}
            onChange={(event) =>
              setPageSize(Number(event.target.value) as CapexPageSize)
            }
            options={capexPageSizes.map((size) => ({
              value: String(size),
              label: String(size),
            }))}
          />
        </label>
        <div className="seg-capex__pager-nav">
          <Button
            variant="ghost"
            size="sm"
            iconLeft={<ChevronLeft size={16} />}
            disabled={state.page <= 1 || entriesQuery.isFetching}
            onClick={() => setPage(Math.max(1, state.page - 1))}
          >
            {t('entries.pagination.previous')}
          </Button>
          <span className="seg-capex__page" aria-live="polite">
            {t('entries.pagination.status', { page: state.page, pages: totalPages })}
          </span>
          <Button
            variant="ghost"
            size="sm"
            iconRight={<ChevronRight size={16} />}
            disabled={state.page >= totalPages || entriesQuery.isFetching}
            onClick={() => setPage(Math.min(totalPages, state.page + 1))}
          >
            {t('entries.pagination.next')}
          </Button>
        </div>
      </nav>
    </main>
  )
}
