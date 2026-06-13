import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Checkbox.css'

export interface CheckboxProps extends Omit<
  ComponentPropsWithRef<'input'>,
  'type' | 'children'
> {
  label?: ReactNode
}

/**
 * Project Armali checkbox with an animated tick.
 *
 * Ported from the design-system reference (`components/forms/Checkbox.jsx`). The
 * visually hidden native input keeps full keyboard and screen-reader behavior;
 * its `ref` is forwarded for React Hook Form's `register`.
 */
export function Checkbox({
  label,
  disabled = false,
  className = '',
  ...rest
}: CheckboxProps) {
  return (
    <label
      className={['arm-check', disabled ? 'arm-check--disabled' : '', className]
        .filter(Boolean)
        .join(' ')}
    >
      <input type="checkbox" disabled={disabled} {...rest} />
      <span className="arm-check__box">
        <svg viewBox="0 0 16 16" aria-hidden="true">
          <path d="M3 8.5 L6.5 12 L13 4" />
        </svg>
      </span>
      {label != null && <span className="arm-check__label">{label}</span>}
    </label>
  )
}
