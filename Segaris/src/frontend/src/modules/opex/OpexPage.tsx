import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import {
  opexApi,
  opexPageSizes,
  type OpexContract,
  type OpexPageSize,
} from '@/app/api/opex'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Select, Spinner, Toast } from '@/components/ui'

import { ContractDialog } from './ContractDialog'
import { ContractsFilters } from './ContractsFilters'
import { ContractsTable } from './ContractsTable'
import {
  activeFilterCount,
  useContractDialog,
  useContractsState,
} from './contractsState'
import { opexKeys } from './queries'

import './OpexPage.css'

type SavedToastMode = 'create' | 'edit' | 'delete'

interface SavedToast {
  mode: SavedToastMode
  name: string
}

const toastKeys: Record<SavedToastMode, { title: string; body: string }> = {
  create: { title: 'editor.toast.created', body: 'editor.toast.createdBody' },
  edit: { title: 'editor.toast.updated', body: 'editor.toast.updatedBody' },
  delete: { title: 'editor.toast.deleted', body: 'editor.toast.deletedBody' },
}

export function OpexPage() {
  const { t, i18n } = useTranslation('opex')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const currentUserId = session?.userId ?? null

  const { state, listQuery, setFilters, setSort, setPage, setPageSize, clearFilters } =
    useContractsState(currentUserId)
  const { dialog, openCreate, openContract, close } = useContractDialog()
  const [savedToast, setSavedToast] = useState<SavedToast | null>(null)

  const handleSaved = (contract: OpexContract, mode: 'create' | 'edit') => {
    void queryClient.invalidateQueries({ queryKey: opexKeys.contracts() })
    void queryClient.invalidateQueries({ queryKey: opexKeys.contract(contract.id) })
    setSavedToast({ mode, name: contract.name })
    close()
  }

  const handleDeleted = (contract: OpexContract) => {
    void queryClient.invalidateQueries({ queryKey: opexKeys.contracts() })
    setSavedToast({ mode: 'delete', name: contract.name })
    close()
  }

  const contractsQuery = useQuery({
    queryKey: opexKeys.contractList(listQuery),
    queryFn: ({ signal }) => opexApi.listContracts(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = contractsQuery.data
  const contracts = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeFilterCount(state) > 0

  useEffect(() => {
    if (data != null && state.page > totalPages) {
      setPage(totalPages)
    }
  }, [data, state.page, totalPages, setPage])

  if (contractsQuery.isError) {
    const error = contractsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void contractsQuery.refetch()} />
    }
  }

  const showInitialLoading = contractsQuery.isPending
  const isRefetching = contractsQuery.isFetching && !contractsQuery.isPending

  return (
    <main className="seg-opex armali-aurora">
      <section className="seg-opex__head">
        <div>
          <div className="armali-eyebrow">{t('contracts.eyebrow')}</div>
          <h1>{t('contracts.title')}</h1>
          <p>{t('contracts.description')}</p>
        </div>
        <div className="seg-opex__head-actions">
          <Badge tone="neutral">{t('contracts.count', { count: totalCount })}</Badge>
          <Button iconLeft={<Plus size={16} />} onClick={openCreate}>
            {t('contracts.newContract')}
          </Button>
        </div>
      </section>

      <ContractsFilters
        state={state}
        onChange={setFilters}
        onClear={clearFilters}
      />

      {showInitialLoading ? (
        <div className="seg-opex__loading">
          <Spinner />
        </div>
      ) : contractsQuery.isError ? (
        <p className="seg-opex__error" role="alert">
          {t('contracts.states.loadError')}
        </p>
      ) : contracts.length === 0 ? (
        <p className="seg-opex__empty">
          {hasFilters ? t('contracts.states.emptyFiltered') : t('contracts.states.empty')}
        </p>
      ) : (
        <ContractsTable
          contracts={contracts}
          state={state}
          language={i18n.language}
          onSort={setSort}
          onOpen={openContract}
          busy={isRefetching}
        />
      )}

      <nav className="seg-opex__pager" aria-label={t('contracts.pagination.label')}>
        <label className="seg-opex__rows">
          <span>{t('contracts.pagination.rowsPerPage')}</span>
          <Select
            value={String(state.pageSize)}
            onChange={(event) =>
              setPageSize(Number(event.target.value) as OpexPageSize)
            }
            options={opexPageSizes.map((size) => ({
              value: String(size),
              label: String(size),
            }))}
          />
        </label>
        <div className="seg-opex__pager-nav">
          <Button
            variant="ghost"
            size="sm"
            iconLeft={<ChevronLeft size={16} />}
            disabled={state.page <= 1 || contractsQuery.isFetching}
            onClick={() => setPage(Math.max(1, state.page - 1))}
          >
            {t('contracts.pagination.previous')}
          </Button>
          <span className="seg-opex__page" aria-live="polite">
            {t('contracts.pagination.status', { page: state.page, pages: totalPages })}
          </span>
          <Button
            variant="ghost"
            size="sm"
            iconRight={<ChevronRight size={16} />}
            disabled={state.page >= totalPages || contractsQuery.isFetching}
            onClick={() => setPage(Math.min(totalPages, state.page + 1))}
          >
            {t('contracts.pagination.next')}
          </Button>
        </div>
      </nav>

      {dialog.mode !== 'closed' && (
        <ContractDialog
          mode={dialog.mode}
          contractId={dialog.mode === 'edit' ? dialog.contractId : undefined}
          currentUserId={currentUserId}
          onClose={close}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {savedToast != null && (
        <div className="seg-opex__toast">
          <Toast
            tone="success"
            title={t(toastKeys[savedToast.mode].title)}
            onClose={() => setSavedToast(null)}
            closeLabel={t('editor.actions.cancel')}
          >
            {t(toastKeys[savedToast.mode].body, { name: savedToast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}
