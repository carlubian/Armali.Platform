import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Select.css'

export interface SelectOption {
  value: string
  label: ReactNode
}

export interface SelectProps extends Omit<ComponentPropsWithRef<'select'>, 'children'> {
  /** Options as `{ value, label }` objects or bare strings. */
  options?: ReadonlyArray<SelectOption | string>
  /** Disabled placeholder rendered as an empty-valued first option. */
  placeholder?: string
  children?: ReactNode
}

/**
 * Project Armali select control with a custom chevron.
 *
 * Ported from the design-system reference (`components/forms/Select.jsx`). The
 * native `<select>` ref is forwarded so React Hook Form's `register` can attach
 * to it. Provide a real label via the surrounding field or `aria-label`.
 */
export function Select({
  options = [],
  placeholder,
  disabled = false,
  className = '',
  children,
  ...rest
}: SelectProps) {
  return (
    <div
      className={[
        'arm-select-wrap',
        disabled ? 'arm-select-wrap--disabled' : '',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <select className="arm-select" disabled={disabled} {...rest}>
        {placeholder != null && (
          <option value="" disabled>
            {placeholder}
          </option>
        )}
        {options.map((o) => {
          const opt: SelectOption = typeof o === 'string' ? { value: o, label: o } : o
          return (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          )
        })}
        {children}
      </select>
      <span className="arm-select__chev">
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.2"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="m6 9 6 6 6-6" />
        </svg>
      </span>
    </div>
  )
}
