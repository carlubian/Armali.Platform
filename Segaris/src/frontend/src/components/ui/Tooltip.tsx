import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Tooltip.css'

export type TooltipSide = 'top' | 'bottom'

export interface TooltipProps extends ComponentPropsWithRef<'span'> {
  /** Tooltip text shown on hover or keyboard focus of the children. */
  label: ReactNode
  side?: TooltipSide
  children: ReactNode
}

/**
 * Project Armali tooltip.
 *
 * Ported from the design-system reference (`components/feedback/Tooltip.jsx`).
 * The bubble reveals on hover and `:focus-within`, so a focusable trigger
 * exposes it to keyboard users.
 */
export function Tooltip({
  label,
  side = 'top',
  children,
  className = '',
  ...rest
}: TooltipProps) {
  return (
    <span
      className={[
        'arm-tooltip',
        side === 'bottom' ? 'arm-tooltip--bottom' : '',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...rest}
    >
      {children}
      <span className="arm-tooltip__bubble" role="tooltip">
        {label}
      </span>
    </span>
  )
}
