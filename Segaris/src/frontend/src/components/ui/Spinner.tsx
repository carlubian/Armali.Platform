import type { ComponentPropsWithRef, CSSProperties } from 'react'

import './Spinner.css'

export interface SpinnerProps extends Omit<ComponentPropsWithRef<'span'>, 'children'> {
  /** Diameter in pixels, or any CSS length string. */
  size?: number | string
  /** Accessible status label announced to assistive technology. */
  label?: string
}

/**
 * Project Armali loading spinner.
 *
 * Ported from the design-system reference (`components/feedback/Spinner.jsx`).
 */
export function Spinner({
  size = 28,
  label = 'Loading',
  className = '',
  style,
  ...rest
}: SpinnerProps) {
  const sizeValue = typeof size === 'number' ? `${size}px` : size
  return (
    <span
      className={['arm-spinner', className].filter(Boolean).join(' ')}
      role="status"
      aria-label={label}
      style={{ ...style, '--_s': sizeValue } as CSSProperties}
      {...rest}
    >
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <circle className="arm-spinner__track" cx="12" cy="12" r="9" />
        <circle className="arm-spinner__head" cx="12" cy="12" r="9" />
      </svg>
    </span>
  )
}
