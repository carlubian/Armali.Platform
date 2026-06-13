import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Button.css'

export type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'action'
  | 'danger'
  | 'outline'
  | 'ghost'

export type ButtonSize = 'sm' | 'md' | 'lg'

export interface ButtonProps extends ComponentPropsWithRef<'button'> {
  /** Visual emphasis. Defaults to the primary (aqua) treatment. */
  variant?: ButtonVariant
  size?: ButtonSize
  /** Stretch to the full width of the container. */
  block?: boolean
  iconLeft?: ReactNode
  iconRight?: ReactNode
}

/**
 * Project Armali button.
 *
 * Ported from the design-system reference (`components/buttons/Button.jsx`).
 * The `type` defaults to `button` so a button inside a form does not submit it
 * unless the caller opts in with `type="submit"`.
 */
export function Button({
  children,
  variant = 'primary',
  size = 'md',
  block = false,
  iconLeft = null,
  iconRight = null,
  className = '',
  type = 'button',
  ...rest
}: ButtonProps) {
  const cls = [
    'arm-btn',
    variant !== 'primary' ? `arm-btn--${variant}` : '',
    size !== 'md' ? `arm-btn--${size}` : '',
    block ? 'arm-btn--block' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <button className={cls} type={type} {...rest}>
      {iconLeft}
      {children != null && <span>{children}</span>}
      {iconRight}
    </button>
  )
}
