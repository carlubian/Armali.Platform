import { useEffect, useId, useRef } from 'react'
import type { ComponentPropsWithRef, CSSProperties, ReactNode } from 'react'
import { createPortal } from 'react-dom'

import './Dialog.css'

export interface DialogProps extends Omit<ComponentPropsWithRef<'div'>, 'title'> {
  /** Controls visibility. When false, nothing is rendered. */
  open?: boolean
  title?: ReactNode
  description?: ReactNode
  /** Invoked on the close button, backdrop click, and the Escape key. */
  onClose?: () => void
  footer?: ReactNode
  /** Max width as a number of pixels or any CSS length. */
  width?: number | string
  /** Accessible label for the close button. */
  closeLabel?: string
  /**
   * Caps the panel height to the viewport and scrolls the body, keeping the
   * title and footer pinned. Used by large editors such as the Capex entry
   * dialog; short confirmation dialogs leave it off.
   */
  scrollable?: boolean
}

/**
 * Open dialogs in mount order. Only the top of the stack reacts to Escape and
 * traps focus, so a selector opened from inside an editor closes by itself first
 * and keeps keyboard focus to itself instead of leaking into the editor behind.
 */
const dialogStack: string[] = []

/** Tabbable controls inside a dialog panel, in document order. */
const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',')

function focusableWithin(panel: HTMLElement): HTMLElement[] {
  // The dialogs in this app hide controls by not rendering them, so every match
  // is genuinely reachable; no extra visibility filtering is needed (and none
  // would survive jsdom, where layout metrics are unavailable).
  return Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
}

/**
 * Project Armali frosted modal dialog.
 *
 * Ported from the design-system reference (`components/overlay/Dialog.jsx`),
 * extended with Escape-to-close, initial focus on the panel, a Tab focus trap,
 * and focus restoration when it unmounts. Stacked dialogs are supported: only
 * the topmost one reacts to Escape and traps focus.
 */
export function Dialog({
  open = true,
  title,
  description,
  onClose,
  footer = null,
  width,
  className = '',
  closeLabel = 'Close',
  scrollable = false,
  children,
  ...rest
}: DialogProps) {
  const panelRef = useRef<HTMLDivElement>(null)
  const onCloseRef = useRef(onClose)
  const labelId = useId()

  useEffect(() => {
    onCloseRef.current = onClose
  }, [onClose])

  useEffect(() => {
    if (!open) return
    const previouslyFocused = document.activeElement as HTMLElement | null
    panelRef.current?.focus()

    dialogStack.push(labelId)
    const onKeyDown = (event: KeyboardEvent) => {
      // Only the topmost dialog reacts, so a stacked selector both dismisses and
      // traps focus before the editor underneath it does.
      if (dialogStack[dialogStack.length - 1] !== labelId) return

      if (event.key === 'Escape') {
        onCloseRef.current?.()
        return
      }

      if (event.key !== 'Tab') return
      const panel = panelRef.current
      if (panel == null) return

      // Keep Tab focus inside the dialog. With nothing else focusable, park it on
      // the panel; otherwise wrap around the first/last control so focus never
      // escapes to the page or a stacked dialog behind this one.
      const focusables = focusableWithin(panel)
      if (focusables.length === 0) {
        event.preventDefault()
        panel.focus()
        return
      }
      const first = focusables[0]
      const last = focusables[focusables.length - 1]
      const active = document.activeElement
      const outside = active == null || (active !== panel && !panel.contains(active))
      if (event.shiftKey) {
        if (active === first || active === panel || outside) {
          event.preventDefault()
          last.focus()
        }
      } else if (active === last || outside) {
        event.preventDefault()
        first.focus()
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      const index = dialogStack.lastIndexOf(labelId)
      if (index !== -1) dialogStack.splice(index, 1)
      previouslyFocused?.focus?.()
    }
  }, [open, labelId])

  if (!open) return null

  const onScrim = (event: React.MouseEvent) => {
    if (event.target === event.currentTarget) onClose?.()
  }

  return createPortal(
    // The backdrop close is a convenience; keyboard users dismiss via the close
    // button or the Escape key handled above.
    // eslint-disable-next-line jsx-a11y/no-static-element-interactions, jsx-a11y/click-events-have-key-events
    <div className="arm-dialog__scrim" onClick={onScrim}>
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title != null ? labelId : undefined}
        tabIndex={-1}
        className={['arm-dialog', scrollable ? 'arm-dialog--scrollable' : '', className]
          .filter(Boolean)
          .join(' ')}
        style={
          width != null
            ? ({
                '--_w': typeof width === 'number' ? `${width}px` : width,
              } as CSSProperties)
            : undefined
        }
        {...rest}
      >
        {onClose && (
          <button
            type="button"
            className="arm-dialog__close"
            aria-label={closeLabel}
            onClick={onClose}
          >
            <svg
              width="16"
              height="16"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.2"
              strokeLinecap="round"
              aria-hidden="true"
            >
              <path d="M18 6 6 18M6 6l12 12" />
            </svg>
          </button>
        )}
        {title != null && (
          <div id={labelId} className="arm-dialog__title">
            {title}
          </div>
        )}
        {description != null && <div className="arm-dialog__desc">{description}</div>}
        {children != null && <div className="arm-dialog__body">{children}</div>}
        {footer != null && <div className="arm-dialog__footer">{footer}</div>}
      </div>
    </div>,
    document.body,
  )
}
