import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  maintenanceApi,
  maintenancePageSizes,
  type MaintenancePageSize,
  type MaintenanceTask,
} from '@/app/api/maintenance'
import { isApiError } from '@/app/api/errors'
import { launcherKeys } from '@/app/api/launcher'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Select, Spinner, Toast } from '@/components/ui'

import { MaintenanceDialog } from './MaintenanceDialog'
import { MaintenanceFilters } from './MaintenanceFilters'
import { MaintenanceTable } from './MaintenanceTable'
import { activeMaintenanceFilterCount, useMaintenanceState } from './maintenanceState'
import { maintenanceKeys } from './queries'

import './MaintenancePage.css'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

export function MaintenancePage() {
  const { t, i18n } = useTranslation('maintenance')
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
  } = useMaintenanceState(currentUserId)

  const invalidateTasks = (taskId?: number) => {
    void queryClient.invalidateQueries({ queryKey: maintenanceKeys.tasks() })
    if (taskId != null) {
      void queryClient.invalidateQueries({ queryKey: maintenanceKeys.task(taskId) })
      void queryClient.invalidateQueries({
        queryKey: maintenanceKeys.taskAttachments(taskId),
      })
    }
    void queryClient.invalidateQueries({ queryKey: launcherKeys.attention() })
  }

  const handleSaved = (task: MaintenanceTask, mode: 'create' | 'edit') => {
    queryClient.setQueryData(maintenanceKeys.task(task.id), task)
    invalidateTasks(task.id)
    setToast({ kind: mode === 'create' ? 'created' : 'updated', name: task.title })
    closeDialog()
  }

  const handleDeleted = (task: MaintenanceTask) => {
    invalidateTasks()
    setToast({ kind: 'deleted', name: task.title })
    closeDialog()
  }

  const tasksQuery = useQuery({
    queryKey: maintenanceKeys.taskList(listQuery),
    queryFn: ({ signal }) => maintenanceApi.listTasks(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = tasksQuery.data
  const tasks = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeMaintenanceFilterCount(state) > 0

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (tasksQuery.isError) {
    const error = tasksQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void tasksQuery.refetch()} />
    }
  }

  const showInitialLoading = tasksQuery.isPending
  const isRefetching = tasksQuery.isFetching && !tasksQuery.isPending

  return (
    <main className="seg-maint armali-aurora">
      <section className="seg-maint__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <section className="seg-maint__panel-head">
        <Badge tone="neutral">{t('tasks.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
          {t('tasks.newTask')}
        </Button>
      </section>

      <MaintenanceFilters state={state} onChange={setFilters} onClear={clearFilters} />

      {showInitialLoading ? (
        <div className="seg-maint__loading">
          <Spinner />
        </div>
      ) : tasksQuery.isError ? (
        <p className="seg-maint__error" role="alert">
          {t('tasks.states.loadError')}
        </p>
      ) : tasks.length === 0 ? (
        <p className="seg-maint__empty">
          {hasFilters ? t('tasks.states.emptyFiltered') : t('tasks.states.empty')}
        </p>
      ) : (
        <MaintenanceTable
          tasks={tasks}
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
        fetching={tasksQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
        label={t('tasks.pagination.label')}
      />

      {dialog.mode !== 'closed' && (
        <MaintenanceDialog
          mode={dialog.mode}
          taskId={dialog.mode === 'edit' ? dialog.taskId : undefined}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {toast != null && (
        <div className="seg-maint__toast">
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
  pageSize: MaintenancePageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: MaintenancePageSize) => void
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
  const { t } = useTranslation('maintenance')
  return (
    <nav className="seg-maint__pager" aria-label={label}>
      <label className="seg-maint__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) =>
            onPageSize(Number(event.target.value) as MaintenancePageSize)
          }
          options={maintenancePageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-maint__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-maint__page" aria-live="polite">
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
