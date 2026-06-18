import { ArrowDown, ArrowUp, Pencil, Trash2 } from 'lucide-react'
import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'

import type { CatalogDescriptor, CatalogRow } from './catalogs'
import type { CatalogMoveDirection } from '@/app/api/catalogs'

export interface FocusRequest {
  id: number
  direction: CatalogMoveDirection
}

export interface CatalogTableProps {
  descriptor: CatalogDescriptor
  rows: CatalogRow[]
  /** Disables interactions while a mutation is in flight. */
  busy: boolean
  /** A move control to refocus once the reordered rows have rendered. */
  focusRequest: FocusRequest | null
  onFocusHandled: () => void
  onEdit: (row: CatalogRow) => void
  onDelete: (row: CatalogRow) => void
  onMove: (row: CatalogRow, direction: CatalogMoveDirection) => void
}

const moveButtonKey = (id: number, direction: CatalogMoveDirection) =>
  `${id}-${direction}`

/**
 * Deterministic catalog table. Rows arrive already ordered by `sortOrder`, then
 * `id`. Move controls reorder through the server and, on success, return focus to
 * the same control (or its sibling when the moved row reaches a boundary) so
 * keyboard reordering never drops focus.
 */
export function CatalogTable({
  descriptor,
  rows,
  busy,
  focusRequest,
  onFocusHandled,
  onEdit,
  onDelete,
  onMove,
}: CatalogTableProps) {
  const { t } = useTranslation('configuration')
  const moveButtons = useRef(new Map<string, HTMLButtonElement | null>())

  // After a reorder the rows re-render; restore focus to the control the user was
  // operating. If that direction is now a disabled boundary, fall back to the
  // row's other move control so focus stays on the moved row.
  useEffect(() => {
    if (focusRequest == null) return
    const sameDirection = moveButtons.current.get(
      moveButtonKey(focusRequest.id, focusRequest.direction),
    )
    const other: CatalogMoveDirection = focusRequest.direction === 'up' ? 'down' : 'up'
    const fallback = moveButtons.current.get(moveButtonKey(focusRequest.id, other))
    const target =
      sameDirection != null && !sameDirection.disabled ? sameDirection : fallback
    target?.focus()
    onFocusHandled()
  }, [focusRequest, rows, onFocusHandled])

  return (
    <div className="seg-catalog__table-wrap">
      <table className="seg-catalog__table">
        <thead>
          <tr>
            <th scope="col" className="seg-catalog__col-order">
              {t('table.columns.order')}
            </th>
            <th scope="col">{t('table.columns.name')}</th>
            {descriptor.hasCode && (
              <th scope="col" className="seg-catalog__col-code">
                {t('table.columns.code')}
              </th>
            )}
            {descriptor.hasColorValue && (
              <th scope="col" className="seg-catalog__col-color">
                {t('table.columns.color')}
              </th>
            )}
            <th scope="col" className="seg-catalog__col-actions">
              {t('table.columns.actions')}
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => {
            const isFirst = index === 0
            const isLast = index === rows.length - 1
            return (
              <tr key={row.id}>
                <td className="seg-catalog__col-order">
                  <div className="seg-catalog__move">
                    <button
                      type="button"
                      className="seg-catalog__icon"
                      ref={(node) => {
                        moveButtons.current.set(moveButtonKey(row.id, 'up'), node)
                      }}
                      disabled={isFirst || busy}
                      aria-label={t('table.moveUp', { name: row.name })}
                      onClick={() => onMove(row, 'up')}
                    >
                      <ArrowUp size={16} aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      className="seg-catalog__icon"
                      ref={(node) => {
                        moveButtons.current.set(moveButtonKey(row.id, 'down'), node)
                      }}
                      disabled={isLast || busy}
                      aria-label={t('table.moveDown', { name: row.name })}
                      onClick={() => onMove(row, 'down')}
                    >
                      <ArrowDown size={16} aria-hidden="true" />
                    </button>
                  </div>
                </td>
                <td className="seg-catalog__name">{row.name}</td>
                {descriptor.hasCode && (
                  <td className="seg-catalog__code">{row.code}</td>
                )}
                {descriptor.hasColorValue && (
                  <td className="seg-catalog__color">
                    <span className="seg-catalog__swatch">
                      <span
                        className="seg-catalog__swatch-chip"
                        style={{ backgroundColor: row.colorValue }}
                        aria-hidden="true"
                      />
                      <span className="seg-catalog__swatch-value">
                        {row.colorValue}
                      </span>
                    </span>
                  </td>
                )}
                <td className="seg-catalog__col-actions">
                  <div className="seg-catalog__row-actions">
                    <button
                      type="button"
                      className="seg-catalog__icon"
                      disabled={busy}
                      aria-label={t('table.edit', { name: row.name })}
                      onClick={() => onEdit(row)}
                    >
                      <Pencil size={16} aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      className="seg-catalog__icon seg-catalog__icon--danger"
                      disabled={busy}
                      aria-label={t('table.delete', { name: row.name })}
                      onClick={() => onDelete(row)}
                    >
                      <Trash2 size={16} aria-hidden="true" />
                    </button>
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
