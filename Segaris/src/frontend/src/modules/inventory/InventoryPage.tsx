import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { launcherKeys } from '@/app/api/launcher'
import {
  inventoryApi,
  inventoryPageSizes,
  type InventoryItem,
  type InventoryItemSummary,
  type InventoryOrder,
  type InventoryPageSize,
} from '@/app/api/inventory'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Button, Select, Spinner, Tabs, Toast } from '@/components/ui'

import { ItemDialog } from './ItemDialog'
import { ItemsFilters } from './ItemsFilters'
import { ItemsTable } from './ItemsTable'
import { OrderDialog } from './OrderDialog'
import { OrdersFilters } from './OrdersFilters'
import { OrdersTable } from './OrdersTable'
import { StockAdjustmentDialog } from './StockAdjustmentDialog'
import { activeItemFilterCount, useItemsState } from './itemsState'
import { activeOrderFilterCount, useOrdersState } from './ordersState'
import { useInventoryView, useItemDialog, useOrderDialog } from './inventoryNav'
import { inventoryKeys } from './queries'

import './InventoryPage.css'

type ToastKind =
  | 'itemCreated'
  | 'itemUpdated'
  | 'itemDeleted'
  | 'itemAdjusted'
  | 'orderCreated'
  | 'orderUpdated'
  | 'orderDeleted'
  | 'orderReceived'

interface ToastState {
  kind: ToastKind
  name: string
}

export function InventoryPage() {
  const { t } = useTranslation('inventory')
  const { view, setView } = useInventoryView()
  const [toast, setToast] = useState<ToastState | null>(null)

  return (
    <main className="seg-inv armali-aurora">
      <section className="seg-inv__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      <Tabs
        variant="line"
        aria-label={t('views.label')}
        value={view}
        onChange={(next) => setView(next as 'items' | 'orders')}
        tabs={[
          { value: 'items', label: t('views.items') },
          { value: 'orders', label: t('views.orders') },
        ]}
      />

      {view === 'items' ? (
        <ItemsPanel onToast={(kind, name) => setToast({ kind, name })} />
      ) : (
        <OrdersPanel onToast={(kind, name) => setToast({ kind, name })} />
      )}

      {toast != null && (
        <div className="seg-inv__toast">
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

interface PanelProps {
  onToast: (kind: ToastKind, name: string) => void
}

function ItemsPanel({ onToast }: PanelProps) {
  const { t, i18n } = useTranslation('inventory')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const currentUserId = session?.userId ?? null

  const { state, listQuery, setFilters, setSort, setPage, setPageSize, clearFilters } =
    useItemsState(currentUserId)
  const { dialog, openCreate, openItem, close } = useItemDialog()
  const [adjusting, setAdjusting] = useState<InventoryItemSummary | null>(null)

  const invalidateItems = (itemId?: number) => {
    void queryClient.invalidateQueries({ queryKey: inventoryKeys.items() })
    if (itemId != null)
      void queryClient.invalidateQueries({ queryKey: inventoryKeys.item(itemId) })
    void queryClient.invalidateQueries({ queryKey: launcherKeys.attention() })
  }

  const handleSaved = (item: InventoryItem, mode: 'create' | 'edit') => {
    queryClient.setQueryData(inventoryKeys.item(item.id), item)
    invalidateItems(item.id)
    onToast(mode === 'create' ? 'itemCreated' : 'itemUpdated', item.name)
    close()
  }

  const handleDeleted = (item: InventoryItem) => {
    invalidateItems()
    onToast('itemDeleted', item.name)
    close()
  }

  const handleAdjusted = (item: InventoryItem) => {
    queryClient.setQueryData(inventoryKeys.item(item.id), item)
    invalidateItems(item.id)
    onToast('itemAdjusted', item.name)
    setAdjusting(null)
  }

  const itemsQuery = useQuery({
    queryKey: inventoryKeys.itemList(listQuery),
    queryFn: ({ signal }) => inventoryApi.listItems(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = itemsQuery.data
  const items = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeItemFilterCount(state) > 0

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (itemsQuery.isError) {
    const error = itemsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void itemsQuery.refetch()} />
    }
  }

  const showInitialLoading = itemsQuery.isPending
  const isRefetching = itemsQuery.isFetching && !itemsQuery.isPending

  return (
    <>
      <section className="seg-inv__panel-head">
        <Badge tone="neutral">{t('items.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreate}>
          {t('items.newItem')}
        </Button>
      </section>

      <ItemsFilters state={state} onChange={setFilters} onClear={clearFilters} />

      {showInitialLoading ? (
        <div className="seg-inv__loading">
          <Spinner />
        </div>
      ) : itemsQuery.isError ? (
        <p className="seg-inv__error" role="alert">
          {t('items.states.loadError')}
        </p>
      ) : items.length === 0 ? (
        <p className="seg-inv__empty">
          {hasFilters ? t('items.states.emptyFiltered') : t('items.states.empty')}
        </p>
      ) : (
        <ItemsTable
          items={items}
          state={state}
          language={i18n.language}
          onSort={setSort}
          onOpen={openItem}
          onAdjust={setAdjusting}
          busy={isRefetching}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={itemsQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
        label={t('items.pagination.label')}
      />

      {dialog.mode !== 'closed' && (
        <ItemDialog
          mode={dialog.mode}
          itemId={dialog.mode === 'edit' ? dialog.itemId : undefined}
          currentUserId={currentUserId}
          onClose={close}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
        />
      )}

      {adjusting != null && (
        <StockAdjustmentDialog
          item={adjusting}
          onClose={() => setAdjusting(null)}
          onAdjusted={handleAdjusted}
        />
      )}
    </>
  )
}

function OrdersPanel({ onToast }: PanelProps) {
  const { t, i18n } = useTranslation('inventory')
  const { session } = useSession()
  const queryClient = useQueryClient()
  const currentUserId = session?.userId ?? null

  const { state, listQuery, setFilters, setSort, setPage, setPageSize, clearFilters } =
    useOrdersState(currentUserId)
  const { dialog, openCreate, openOrder, close } = useOrderDialog()

  const invalidateOrders = (orderId?: number, alsoItems = false) => {
    void queryClient.invalidateQueries({ queryKey: inventoryKeys.orders() })
    if (orderId != null)
      void queryClient.invalidateQueries({ queryKey: inventoryKeys.order(orderId) })
    if (alsoItems) {
      void queryClient.invalidateQueries({ queryKey: inventoryKeys.items() })
      void queryClient.invalidateQueries({ queryKey: launcherKeys.attention() })
    }
  }

  const handleSaved = (order: InventoryOrder, mode: 'create' | 'edit') => {
    queryClient.setQueryData(inventoryKeys.order(order.id), order)
    invalidateOrders(order.id)
    onToast(mode === 'create' ? 'orderCreated' : 'orderUpdated', order.supplierName)
    close()
  }

  const handleDeleted = (order: InventoryOrder) => {
    invalidateOrders()
    onToast('orderDeleted', order.supplierName)
    close()
  }

  const handleReceived = (order: InventoryOrder) => {
    queryClient.setQueryData(inventoryKeys.order(order.id), order)
    invalidateOrders(order.id, true)
    onToast('orderReceived', order.supplierName)
    close()
  }

  const ordersQuery = useQuery({
    queryKey: inventoryKeys.orderList(listQuery),
    queryFn: ({ signal }) => inventoryApi.listOrders(listQuery, signal),
    placeholderData: keepPreviousData,
  })

  const data = ordersQuery.data
  const orders = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const hasFilters = activeOrderFilterCount(state) > 0

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (ordersQuery.isError) {
    const error = ordersQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void ordersQuery.refetch()} />
    }
  }

  const showInitialLoading = ordersQuery.isPending
  const isRefetching = ordersQuery.isFetching && !ordersQuery.isPending

  return (
    <>
      <section className="seg-inv__panel-head">
        <Badge tone="neutral">{t('orders.count', { count: totalCount })}</Badge>
        <Button iconLeft={<Plus size={16} />} onClick={openCreate}>
          {t('orders.newOrder')}
        </Button>
      </section>

      <OrdersFilters state={state} onChange={setFilters} onClear={clearFilters} />

      {showInitialLoading ? (
        <div className="seg-inv__loading">
          <Spinner />
        </div>
      ) : ordersQuery.isError ? (
        <p className="seg-inv__error" role="alert">
          {t('orders.states.loadError')}
        </p>
      ) : orders.length === 0 ? (
        <p className="seg-inv__empty">
          {hasFilters ? t('orders.states.emptyFiltered') : t('orders.states.empty')}
        </p>
      ) : (
        <OrdersTable
          orders={orders}
          state={state}
          language={i18n.language}
          onSort={setSort}
          onOpen={openOrder}
          busy={isRefetching}
        />
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={ordersQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
        label={t('orders.pagination.label')}
      />

      {dialog.mode !== 'closed' && (
        <OrderDialog
          mode={dialog.mode}
          orderId={dialog.mode === 'edit' ? dialog.orderId : undefined}
          currentUserId={currentUserId}
          onClose={close}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
          onReceived={handleReceived}
        />
      )}
    </>
  )
}

interface PagerProps {
  page: number
  pageSize: InventoryPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: InventoryPageSize) => void
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
  const { t } = useTranslation('inventory')
  return (
    <nav className="seg-inv__pager" aria-label={label}>
      <label className="seg-inv__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) =>
            onPageSize(Number(event.target.value) as InventoryPageSize)
          }
          options={inventoryPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-inv__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-inv__page" aria-live="polite">
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
