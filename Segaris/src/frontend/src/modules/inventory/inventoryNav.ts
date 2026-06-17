import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

export type InventoryView = 'items' | 'orders'

// Dialog and view parameters cleared whenever the active view changes, so a
// stale dialog never lingers over the other table.
const DIALOG_PARAMS = ['newItem', 'itemId', 'newOrder', 'orderId'] as const

export interface UseInventoryView {
  view: InventoryView
  setView: (view: InventoryView) => void
}

export function useInventoryView(): UseInventoryView {
  const [searchParams, setSearchParams] = useSearchParams()
  const view: InventoryView = searchParams.get('view') === 'orders' ? 'orders' : 'items'

  const setView = useCallback(
    (next: InventoryView) => {
      setSearchParams((current) => {
        const params = new URLSearchParams(current)
        for (const key of DIALOG_PARAMS) params.delete(key)
        if (next === 'orders') params.set('view', 'orders')
        else params.delete('view')
        return params
      })
    },
    [setSearchParams],
  )

  return { view, setView }
}

export type ItemDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; itemId: number }

export interface UseItemDialog {
  dialog: ItemDialogState
  openCreate: () => void
  openItem: (itemId: number) => void
  close: () => void
}

export function useItemDialog(): UseItemDialog {
  const [searchParams, setSearchParams] = useSearchParams()

  const dialog = useMemo<ItemDialogState>(() => {
    if (searchParams.get('newItem') === 'true') return { mode: 'create' }
    const itemId = Number.parseInt(searchParams.get('itemId') ?? '', 10)
    if (Number.isFinite(itemId) && itemId > 0) return { mode: 'edit', itemId }
    return { mode: 'closed' }
  }, [searchParams])

  const openCreate = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.delete('view')
      next.delete('itemId')
      next.set('newItem', 'true')
      return next
    })
  }, [setSearchParams])

  const openItem = useCallback(
    (itemId: number) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        next.delete('view')
        next.delete('newItem')
        next.set('itemId', String(itemId))
        return next
      })
    },
    [setSearchParams],
  )

  const close = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.delete('newItem')
      next.delete('itemId')
      return next
    })
  }, [setSearchParams])

  return { dialog, openCreate, openItem, close }
}

export type OrderDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; orderId: number }

export interface UseOrderDialog {
  dialog: OrderDialogState
  openCreate: () => void
  openOrder: (orderId: number) => void
  close: () => void
}

export function useOrderDialog(): UseOrderDialog {
  const [searchParams, setSearchParams] = useSearchParams()

  const dialog = useMemo<OrderDialogState>(() => {
    if (searchParams.get('newOrder') === 'true') return { mode: 'create' }
    const orderId = Number.parseInt(searchParams.get('orderId') ?? '', 10)
    if (Number.isFinite(orderId) && orderId > 0) return { mode: 'edit', orderId }
    return { mode: 'closed' }
  }, [searchParams])

  const openCreate = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.set('view', 'orders')
      next.delete('orderId')
      next.set('newOrder', 'true')
      return next
    })
  }, [setSearchParams])

  const openOrder = useCallback(
    (orderId: number) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        next.set('view', 'orders')
        next.delete('newOrder')
        next.set('orderId', String(orderId))
        return next
      })
    },
    [setSearchParams],
  )

  const close = useCallback(() => {
    setSearchParams((current) => {
      const next = new URLSearchParams(current)
      next.delete('newOrder')
      next.delete('orderId')
      return next
    })
  }, [setSearchParams])

  return { dialog, openCreate, openOrder, close }
}
