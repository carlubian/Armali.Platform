import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/

function toIsoDate(date: Date): string {
  const year = date.getFullYear().toString().padStart(4, '0')
  const month = (date.getMonth() + 1).toString().padStart(2, '0')
  const day = date.getDate().toString().padStart(2, '0')
  return `${year}-${month}-${day}`
}

/** Normalises any civil date to the Monday that anchors its ISO week. */
export function mondayOf(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  const date = new Date(year, month - 1, day)
  const dayOfWeek = date.getDay() // 0 = Sunday .. 6 = Saturday
  const offset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
  date.setDate(date.getDate() + offset)
  return toIsoDate(date)
}

export function addWeeks(isoMonday: string, weeks: number): string {
  const [year, month, day] = isoMonday.split('-').map(Number)
  const date = new Date(year, month - 1, day)
  date.setDate(date.getDate() + weeks * 7)
  return toIsoDate(date)
}

export function currentWeekMonday(today: Date = new Date()): string {
  return mondayOf(toIsoDate(today))
}

export type MenuDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; menuId: number }

function intOrNull(value: string | null): number | null {
  if (value == null) return null
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

export function parseWeek(params: URLSearchParams, today: Date = new Date()): string {
  const raw = params.get('week')
  return raw != null && ISO_DATE.test(raw) ? mondayOf(raw) : currentWeekMonday(today)
}

export function parseMenuDialogState(params: URLSearchParams): MenuDialogState {
  if (params.get('newMenu') === 'true') return { mode: 'create' }
  const menuId = intOrNull(params.get('menuId'))
  return menuId == null ? { mode: 'closed' } : { mode: 'edit', menuId }
}

export interface UseMenusState {
  week: string
  dialog: MenuDialogState
  goToWeek: (week: string) => void
  goToPreviousWeek: () => void
  goToNextWeek: () => void
  goToCurrentWeek: () => void
  openCreateDialog: () => void
  openEditDialog: (menuId: number) => void
  closeDialog: () => void
}

export function useMenusState(): UseMenusState {
  const [searchParams, setSearchParams] = useSearchParams()
  const week = useMemo(() => parseWeek(searchParams), [searchParams])
  const dialog = useMemo(() => parseMenuDialogState(searchParams), [searchParams])

  const goToWeek = useCallback(
    (target: string) => {
      const monday = mondayOf(target)
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        next.set('week', monday)
        return next
      })
    },
    [setSearchParams],
  )

  const setDialogParams = useCallback(
    (patch: { newMenu?: string | null; menuId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newMenu == null) next.delete('newMenu')
        else next.set('newMenu', patch.newMenu)
        if (patch.menuId == null) next.delete('menuId')
        else next.set('menuId', patch.menuId)
        return next
      })
    },
    [setSearchParams],
  )

  return {
    week,
    dialog,
    goToWeek,
    goToPreviousWeek: () => goToWeek(addWeeks(week, -1)),
    goToNextWeek: () => goToWeek(addWeeks(week, 1)),
    goToCurrentWeek: () => goToWeek(currentWeekMonday()),
    openCreateDialog: () => setDialogParams({ newMenu: 'true', menuId: null }),
    openEditDialog: (menuId: number) =>
      setDialogParams({ newMenu: null, menuId: String(menuId) }),
    closeDialog: () => setDialogParams({ newMenu: null, menuId: null }),
  }
}
