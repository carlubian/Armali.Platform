import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Card.css'

export interface CardProps extends Omit<ComponentPropsWithRef<'div'>, 'title'> {
  title?: ReactNode
  subtitle?: ReactNode
  /** Top-right header slot, typically an action control. */
  action?: ReactNode
  footer?: ReactNode
  /** Frosted glass surface instead of solid bone. */
  glass?: boolean
  /** Accent-tinted border. */
  accent?: boolean
  /** Lift-and-glow hover affordance for clickable cards. */
  interactive?: boolean
}

/**
 * Project Armali surface card with optional header, body, and footer.
 *
 * Ported from the design-system reference (`components/surfaces/Card.jsx`).
 * When `interactive` is used for a clickable card, the caller is responsible
 * for providing an accessible interactive element and keyboard handling.
 */
export function Card({
  title,
  subtitle,
  action = null,
  footer = null,
  glass = false,
  accent = false,
  interactive = false,
  className = '',
  children,
  ...rest
}: CardProps) {
  const cls = [
    'arm-card',
    glass ? 'arm-card--glass' : '',
    accent ? 'arm-card--accent' : '',
    interactive ? 'arm-card--interactive' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  const hasHeader = title != null || subtitle != null || action != null

  return (
    <div className={cls} {...rest}>
      {hasHeader && (
        <div className="arm-card__header">
          <div>
            {title != null && <div className="arm-card__title">{title}</div>}
            {subtitle != null && <div className="arm-card__subtitle">{subtitle}</div>}
          </div>
          {action}
        </div>
      )}
      {children != null && <div className="arm-card__body">{children}</div>}
      {footer != null && <div className="arm-card__footer">{footer}</div>}
    </div>
  )
}
