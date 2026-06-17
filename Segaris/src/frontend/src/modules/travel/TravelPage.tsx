import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { launcherKeys } from '@/app/api/launcher'
import {
  travelApi,
  travelPageSizes,
  type TravelPageSize,
  type TravelTrip,
} from '@/app/api/travel'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Select, Spinner, Toast } from '@/components/ui'

import { TripDialog } from './TripDialog'
import { TripsFilters } from './TripsFilters'
import { TripsTable } from './TripsTable'
import { travelKeys } from './queries'
import { activeTripFilterCount, useTripsState } from './tripsState'

import './TravelPage.css'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

export function TravelPage() {
  const { t, i18n } = useTranslation('travel')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const currentUserId = session?.userId ?? null

  const [toast, setToast] = useState<ToastState | null>(null)

  const {
    state,
    dialog,
    listQuery,
    setFilters,
    setSort,
    setPage,
    setPageSize,
    clearFilters,
    openCreateDialog,
    openEditDialog,
    closeDialog,
  } = useTripsState(currentUserId)

  const invalidateTrips = (tripId?: number) => {
    void queryClient.invalidateQueries({ queryKey: travelKeys.trips() })
    if (tripId != null)
      void queryClient.invalidateQueries({ queryKey: travelKeys.trip(tripId) })
    void queryClient.invalidateQueries({ queryKey: launcherKeys.attention() })
  }

  const handleCreated = (trip: TravelTrip) => {
    invalidateTrips(trip.id)
    setToast({ kind: 'created', name: trip.name })
    // Keep the dialog open in edit mode so the user can immediately add
    // attachments and expenses to the freshly created trip.
    openEditDialog(trip.id)
  }

  const handleSaved = (trip: TravelTrip) => {
    invalidateTrips(trip.id)
    setToast({ kind: 'updated', name: trip.name })
    closeDialog()
  }

  const handleDeleted = (trip: TravelTrip) => {
    invalidateTrips()
    setToast({ kind: 'deleted', name: trip.name })
    closeDialog()
  }

  const tripsQuery = useQuery({
    queryKey: travelKeys.tripList(listQuery),
    queryFn: ({ signal }) => travelApi.listTrips(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = tripsQuery.data
  const trips = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeTripFilterCount(state) > 0

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (tripsQuery.isError) {
    const error = tripsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void tripsQuery.refetch()} />
    }
  }

  const showInitialLoading = tripsQuery.isPending
  const isRefetching = tripsQuery.isFetching && !tripsQuery.isPending

  return (
    <main className="seg-trv armali-aurora">
      <section className="seg-trv__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <section className="seg-trv__panel-head">
        <Badge tone="neutral">{t('trips.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
          {t('trips.newTrip')}
        </Button>
      </section>

      <TripsFilters state={state} onChange={setFilters} onClear={clearFilters} />

      {showInitialLoading ? (
        <div className="seg-trv__loading">
          <Spinner />
        </div>
      ) : tripsQuery.isError ? (
        <p className="seg-trv__error" role="alert">
          {t('trips.states.loadError')}
        </p>
      ) : trips.length === 0 ? (
        <p className="seg-trv__empty">
          {hasFilters ? t('trips.states.emptyFiltered') : t('trips.states.empty')}
        </p>
      ) : (
        <TripsTable
          trips={trips}
          state={state}
          language={i18n.language}
          onSort={setSort}
          onOpen={openEditDialog}
          busy={isRefetching}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={tripsQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
        label={t('trips.pagination.label')}
      />

      {dialog.mode !== 'closed' && (
        <TripDialog
          mode={dialog.mode}
          tripId={dialog.mode === 'edit' ? dialog.tripId : undefined}
          currentUserId={currentUserId}
          language={i18n.language}
          onClose={closeDialog}
          onCreated={handleCreated}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {toast != null && (
        <div className="seg-trv__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            onClose={() => setToast(null)}
            closeLabel={t('editor.actions.cancel')}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

interface PagerProps {
  page: number
  pageSize: TravelPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: TravelPageSize) => void
  label: string
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
  label,
}: PagerProps) {
  const { t } = useTranslation('travel')
  return (
    <nav className="seg-trv__pager" aria-label={label}>
      <label className="seg-trv__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) => onPageSize(Number(event.target.value) as TravelPageSize)}
          options={travelPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-trv__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-trv__page" aria-live="polite">
          {t('pagination.status', { page, pages: totalPages })}
        </span>
        <Button
          variant="ghost"
          size="sm"
          iconRight={<ChevronRight size={16} />}
          disabled={page >= totalPages || fetching}
          onClick={() => onPage(Math.min(totalPages, page + 1))}
        >
          {t('pagination.next')}
        </Button>
      </div>
    </nav>
  )
}
