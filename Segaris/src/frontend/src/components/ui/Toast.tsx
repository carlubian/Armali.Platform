import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Toast.css'

export type ToastTone = 'info' | 'success' | 'danger' | 'gold'

export interface ToastProps extends Omit<ComponentPropsWithRef<'div'>, 'title'> {
  title?: ReactNode
  tone?: ToastTone
  /** Custom leading icon; falls back to a tone-appropriate glyph. */
  icon?: ReactNode
  /** When provided, renders a dismiss button that invokes this handler. */
  onClose?: () => void
  /** Accessible label for the dismiss button. */
  closeLabel?: string
}

const ICONS: Record<ToastTone, string> = {
  info: 'M12 8h.01M11 12h1v4h1',
  success: 'm5 13 4 4L19 7',
  danger: 'M12 9v4m0 4h.01',
  gold: 'M12 9v4m0 4h.01',
}

/**
 * Project Armali glass toast with a tone rail and optional dismiss.
 *
 * Ported from the design-system reference (`components/feedback/Toast.jsx`).
 * This is the presentational toast only; the toast queue/portal manager is
 * wired up in a later wave per docs/architecture/user-experience.md.
 */
export function Toast({
  title,
  children,
  tone = 'info',
  icon,
  onClose,
  closeLabel = 'Dismiss',
  className = '',
  ...rest
}: ToastProps) {
  const toneCls = tone !== 'info' ? `arm-toast--${tone}` : ''
  return (
    <div
      className={['arm-toast', 'arm-toast--enter', toneCls, className]
        .filter(Boolean)
        .join(' ')}
      role="status"
      {...rest}
    >
      <span className="arm-toast__icon">
        {icon ?? (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <circle cx="12" cy="12" r="9" opacity="0.35" />
            <path d={ICONS[tone]} />
          </svg>
        )}
      </span>
      <div className="arm-toast__body">
        {title != null && <div className="arm-toast__title">{title}</div>}
        {children != null && <div className="arm-toast__msg">{children}</div>}
      </div>
      {onClose && (
        <button
          type="button"
          className="arm-toast__close"
          aria-label={closeLabel}
          onClick={onClose}
        >
          <svg
            width="15"
            height="15"
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
    </div>
  )
}
