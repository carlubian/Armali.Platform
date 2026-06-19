import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  assetPageSizes,
  assetsApi,
  type Asset,
  type AssetPageSize,
} from '@/app/api/assets'
import { isApiError } from '@/app/api/errors'
import { launcherKeys } from '@/app/api/launcher'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Select, Spinner, Toast } from '@/components/ui'

import { AssetDialog } from './AssetDialog'
import { AssetsFilters } from './AssetsFilters'
import { AssetsTable } from './AssetsTable'
import { activeAssetFilterCount, useAssetsState } from './assetsState'
import { assetsKeys } from './queries'

import './AssetsPage.css'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

export function AssetsPage() {
  const { t, i18n } = useTranslation('assets')
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
  } = useAssetsState(currentUserId)

  const invalidateAssets = (assetId?: number) => {
    void queryClient.invalidateQueries({ queryKey: assetsKeys.assets() })
    if (assetId != null) {
      void queryClient.invalidateQueries({ queryKey: assetsKeys.asset(assetId) })
      void queryClient.invalidateQueries({
        queryKey: assetsKeys.assetAttachments(assetId),
      })
    }
    void queryClient.invalidateQueries({ queryKey: launcherKeys.attention() })
  }

  const handleSaved = (asset: Asset, mode: 'create' | 'edit') => {
    queryClient.setQueryData(assetsKeys.asset(asset.id), asset)
    invalidateAssets(asset.id)
    setToast({ kind: mode === 'create' ? 'created' : 'updated', name: asset.name })
    closeDialog()
  }

  const handleDeleted = (asset: Asset) => {
    invalidateAssets()
    setToast({ kind: 'deleted', name: asset.name })
    closeDialog()
  }

  const assetsQuery = useQuery({
    queryKey: assetsKeys.assetList(listQuery),
    queryFn: ({ signal }) => assetsApi.listAssets(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = assetsQuery.data
  const assets = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeAssetFilterCount(state) > 0

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (assetsQuery.isError) {
    const error = assetsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void assetsQuery.refetch()} />
    }
  }

  const showInitialLoading = assetsQuery.isPending
  const isRefetching = assetsQuery.isFetching && !assetsQuery.isPending

  return (
    <main className="seg-assets armali-aurora">
      <section className="seg-assets__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <section className="seg-assets__panel-head">
        <Badge tone="neutral">{t('assets.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreateDialog}>
          {t('assets.newAsset')}
        </Button>
      </section>

      <AssetsFilters state={state} onChange={setFilters} onClear={clearFilters} />

      {showInitialLoading ? (
        <div className="seg-assets__loading">
          <Spinner />
        </div>
      ) : assetsQuery.isError ? (
        <p className="seg-assets__error" role="alert">
          {t('assets.states.loadError')}
        </p>
      ) : assets.length === 0 ? (
        <p className="seg-assets__empty">
          {hasFilters ? t('assets.states.emptyFiltered') : t('assets.states.empty')}
        </p>
      ) : (
        <AssetsTable
          assets={assets}
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
        fetching={assetsQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
        label={t('assets.pagination.label')}
      />

      {dialog.mode !== 'closed' && (
        <AssetDialog
          mode={dialog.mode}
          assetId={dialog.mode === 'edit' ? dialog.assetId : undefined}
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {toast != null && (
        <div className="seg-assets__toast">
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
  pageSize: AssetPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: AssetPageSize) => void
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
  const { t } = useTranslation('assets')
  return (
    <nav className="seg-assets__pager" aria-label={label}>
      <label className="seg-assets__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) => onPageSize(Number(event.target.value) as AssetPageSize)}
          options={assetPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-assets__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-assets__page" aria-live="polite">
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
