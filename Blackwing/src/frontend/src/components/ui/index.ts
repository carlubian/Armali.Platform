/**
 * Project Armali design-system primitives, ported as typed React components.
 *
 * These are generic, business-agnostic UI building blocks. Platform and product
 * modules import them from this single public entry point. Only the primitives
 * the current milestone needs are ported; the rest of the kit (inputs, dialogs,
 * toasts, tooltips, tabs) lands with the screens that require them.
 */

export { Button } from './Button'
export type { ButtonProps, ButtonSize, ButtonVariant } from './Button'

export { Card } from './Card'
export type { CardProps } from './Card'

export { IconButton } from './IconButton'
export type { IconButtonProps, IconButtonSize, IconButtonVariant } from './IconButton'

export { Spinner } from './Spinner'
export type { SpinnerProps } from './Spinner'
