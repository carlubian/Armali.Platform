import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Switch.css'

export interface SwitchProps extends Omit<
  ComponentPropsWithRef<'input'>,
  'type' | 'children'
> {
  label?: ReactNode
  /** Adds an ambient breathing glow while checked, for "live" toggles. */
  live?: boolean
}

/**
 * Project Armali toggle switch.
 *
 * Ported from the design-system reference (`components/forms/Switch.jsx`). Built
 * on a visually hidden checkbox so it keeps native keyboard and screen-reader
 * semantics; the `ref` is forwarded for React Hook Form's `register`.
 */
export function Switch({
  label,
  disabled = false,
  live = false,
  className = '',
  ...rest
}: SwitchProps) {
  return (
    <label
      className={[
        'arm-switch',
        disabled ? 'arm-switch--disabled' : '',
        live ? 'arm-switch--live' : '',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <input type="checkbox" disabled={disabled} {...rest} />
      <span className="arm-switch__track">
        <span className="arm-switch__thumb" />
      </span>
      {label != null && <span className="arm-switch__label">{label}</span>}
    </label>
  )
}
