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
}

/**
 * Project Armali frosted modal dialog.
 *
 * Ported from the design-system reference (`components/overlay/Dialog.jsx`),
 * extended with Escape-to-close, initial focus on the panel, and focus
 * restoration when it unmounts.
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
  children,
  ...rest
}: DialogProps) {
  const panelRef = useRef<HTMLDivElement>(null)
  const labelId = useId()

  useEffect(() => {
    if (!open) return
    const previouslyFocused = document.activeElement as HTMLElement | null
    panelRef.current?.focus()

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose?.()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      previouslyFocused?.focus?.()
    }
  }, [open, onClose])

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
        className={['arm-dialog', className].filter(Boolean).join(' ')}
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
