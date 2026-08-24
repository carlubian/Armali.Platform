import type { ComponentPropsWithRef } from 'react'

import './Badge.css'

export type BadgeTone =
  | 'aqua'
  | 'gold'
  | 'azure'
  | 'success'
  | 'danger'
  | 'neutral'
  | 'solid'

export interface BadgeProps extends ComponentPropsWithRef<'span'> {
  tone?: BadgeTone
  /** Render a leading status dot. */
  dot?: boolean
  /** Animate the status dot with a soft pulse (implies `dot`). */
  pulse?: boolean
}

/**
 * Project Armali pill badge.
 *
 * Ported from the design-system reference (`components/surfaces/Badge.jsx`).
 */
export function Badge({
  children,
  tone = 'aqua',
  dot = false,
  pulse = false,
  className = '',
  ...rest
}: BadgeProps) {
  const toneCls = tone === 'aqua' ? '' : `arm-badge--${tone}`
  return (
    <span
      className={['arm-badge', toneCls, className].filter(Boolean).join(' ')}
      {...rest}
    >
      {(dot || pulse) && (
        <span
          className={['arm-badge__dot', pulse ? 'arm-badge__dot--pulse' : '']
            .filter(Boolean)
            .join(' ')}
        />
      )}
      {children}
    </span>
  )
}
