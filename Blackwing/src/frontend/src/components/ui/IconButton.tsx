import type { ComponentPropsWithRef, ReactNode } from 'react'

import './IconButton.css'

export type IconButtonSize = 'sm' | 'md' | 'lg'
export type IconButtonVariant = 'frost' | 'solid' | 'bare'

export interface IconButtonProps extends Omit<
  ComponentPropsWithRef<'button'>,
  'children'
> {
  /** The icon element to render. */
  icon?: ReactNode
  children?: ReactNode
  size?: IconButtonSize
  variant?: IconButtonVariant
  /**
   * Accessible name for the icon-only control. Required because the button has
   * no visible text label; it is exposed as both `aria-label` and `title`.
   */
  label: string
}

/**
 * Project Armali icon-only button.
 *
 * Ported from the design-system reference (`components/buttons/IconButton.jsx`).
 * Unlike the prototype, `label` is mandatory so the control always has an
 * accessible name.
 */
export function IconButton({
  children,
  icon,
  size = 'md',
  variant = 'frost',
  label,
  className = '',
  type = 'button',
  ...rest
}: IconButtonProps) {
  const cls = [
    'arm-iconbtn',
    size !== 'md' ? `arm-iconbtn--${size}` : '',
    variant !== 'frost' ? `arm-iconbtn--${variant}` : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <button className={cls} aria-label={label} title={label} type={type} {...rest}>
      {icon ?? children}
    </button>
  )
}
