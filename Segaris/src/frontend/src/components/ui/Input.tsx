import { useId } from 'react'
import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Input.css'

export interface InputProps extends Omit<ComponentPropsWithRef<'input'>, 'children'> {
  label?: ReactNode
  hint?: ReactNode
  /** When set, the field renders in its error state and shows this message. */
  error?: ReactNode
  required?: boolean
  iconLeft?: ReactNode
  iconRight?: ReactNode
}

/**
 * Project Armali labelled text field.
 *
 * Ported from the design-system reference (`components/forms/Input.jsx`). The
 * input `ref` is forwarded so React Hook Form's `register` can attach to it.
 * The hint/error message is associated with the input via `aria-describedby`.
 */
export function Input({
  label,
  hint,
  error,
  required = false,
  iconLeft = null,
  iconRight = null,
  disabled = false,
  className = '',
  id,
  ...rest
}: InputProps) {
  const generatedId = useId()
  const fieldId = id ?? generatedId
  const msg = error ?? hint
  const msgId = msg != null ? `${fieldId}-msg` : undefined

  return (
    <div className={['arm-field', className].filter(Boolean).join(' ')}>
      {label != null && (
        <label
          className={['arm-field__label', required ? 'arm-field__label--req' : '']
            .filter(Boolean)
            .join(' ')}
          htmlFor={fieldId}
        >
          {label}
        </label>
      )}
      <div
        className={[
          'arm-input-wrap',
          error != null ? 'arm-input-wrap--error' : '',
          disabled ? 'arm-input-wrap--disabled' : '',
        ]
          .filter(Boolean)
          .join(' ')}
      >
        {iconLeft != null && <span className="arm-icon">{iconLeft}</span>}
        <input
          id={fieldId}
          className="arm-input"
          disabled={disabled}
          required={required}
          aria-required={required || undefined}
          aria-invalid={error != null}
          aria-describedby={msgId}
          {...rest}
        />
        {iconRight != null && <span className="arm-icon">{iconRight}</span>}
      </div>
      {msg != null && (
        <span
          id={msgId}
          className={['arm-field__hint', error != null ? 'arm-field__hint--error' : '']
            .filter(Boolean)
            .join(' ')}
        >
          {msg}
        </span>
      )}
    </div>
  )
}
