import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { isApiError } from '@/app/api/errors'
import { launcherKeys } from '@/app/api/launcher'
import {
  processesApi,
  processPageSizes,
  type Process,
  type ProcessPageSize,
} from '@/app/api/processes'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Select, Spinner, Toast } from '@/components/ui'

import { ProcessDialog } from './ProcessDialog'
import { ProcessStepsDialog } from './ProcessStepsDialog'
import { ProcessesFilters } from './ProcessesFilters'
import { ProcessesTable } from './ProcessesTable'
import { processesKeys } from './contracts'
import { activeProcessesFilterCount, useProcessesState } from './processesState'

import './ProcessesPage.css'

type ToastKind = 'created' | 'updated' | 'deleted' | 'cancelled' | 'reopened'

interface ToastState {
  kind: ToastKind
  name: string
}

export function ProcessesPage() {
  const { t, i18n } = useTranslation('processes')
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
    openStepsDialog,
    openRestructureDialog,
    closeDialog,
  } = useProcessesState(currentUserId)

  const invalidateProcesses = (processId?: number) => {
    void queryClient.invalidateQueries({ queryKey: processesKeys.all })
    if (processId != null) {
      void queryClient.invalidateQueries({ queryKey: processesKeys.process(processId) })
    }
    void queryClient.invalidateQueries({ queryKey: launcherKeys.attention() })
  }

  const handleSaved = (process: Process, mode: 'create' | 'edit') => {
    queryClient.setQueryData(processesKeys.process(process.id), process)
    invalidateProcesses(process.id)
    setToast({ kind: mode === 'create' ? 'created' : 'updated', name: process.name })
    closeDialog()
  }

  const handleDeleted = (process: Process) => {
    invalidateProcesses()
    setToast({ kind: 'deleted', name: process.name })
    closeDialog()
  }

  const handleLifecycle = (process: Process, kind: 'cancelled' | 'reopened') => {
    queryClient.setQueryData(processesKeys.process(process.id), process)
    invalidateProcesses(process.id)
    setToast({ kind, name: process.name })
  }

  const processesQuery = useQuery({
    queryKey: processesKeys.list(listQuery),
    queryFn: ({ signal }) => processesApi.listProcesses(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = processesQuery.data
  const processes = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeProcessesFilterCount(state) > 0

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (processesQuery.isError) {
    const error = processesQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void processesQuery.refetch()} />
    }
  }

  const showInitialLoading = processesQuery.isPending
  const isRefetching = processesQuery.isFetching && !processesQuery.isPending

  return (
    <main className="seg-proc armali-aurora">
      <section className="seg-proc__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <section className="seg-proc__panel-head">
        <Badge tone="neutral">{t('list.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
          {t('list.newProcess')}
        </Button>
      </section>

      <ProcessesFilters state={state} onChange={setFilters} onClear={clearFilters} />

      {showInitialLoading ? (
        <div className="seg-proc__loading">
          <Spinner />
        </div>
      ) : processesQuery.isError ? (
        <p className="seg-proc__error" role="alert">
          {t('list.states.loadError')}
        </p>
      ) : processes.length === 0 ? (
        <p className="seg-proc__empty">
          {hasFilters ? t('list.states.emptyFiltered') : t('list.states.empty')}
        </p>
      ) : (
        <ProcessesTable
          processes={processes}
          state={state}
          language={i18n.language}
          busy={isRefetching}
          onSort={setSort}
          onOpen={openEditDialog}
          onSteps={openStepsDialog}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={processesQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
      />

      {(dialog.mode === 'create' || dialog.mode === 'edit') && (
        <ProcessDialog
          mode={dialog.mode}
          processId={dialog.mode === 'edit' ? dialog.processId : undefined}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
          onLifecycle={handleLifecycle}
          onManageSteps={openStepsDialog}
        />
      )}

      {(dialog.mode === 'steps' || dialog.mode === 'restructure') && (
        <ProcessStepsDialog
          processId={dialog.processId}
          language={i18n.language}
          mode={dialog.mode === 'steps' ? 'timeline' : 'restructure'}
          onClose={closeDialog}
          onRestructure={() => openRestructureDialog(dialog.processId)}
          onBackToTimeline={() => openStepsDialog(dialog.processId)}
        />
      )}

      {toast != null && (
        <div className="seg-proc__toast">
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
  pageSize: ProcessPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: ProcessPageSize) => void
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
}: PagerProps) {
  const { t } = useTranslation('processes')
  return (
    <nav className="seg-proc__pager" aria-label={t('pagination.label')}>
      <label className="seg-proc__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) =>
            onPageSize(Number(event.target.value) as ProcessPageSize)
          }
          options={processPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-proc__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-proc__page" aria-live="polite">
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
