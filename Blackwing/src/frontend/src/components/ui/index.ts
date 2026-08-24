/**
 * Project Armali design-system primitives, ported as typed React components.
 *
 * These are generic, business-agnostic UI building blocks. Platform and product
 * modules import them from this single public entry point. Only the primitives
 * the current milestone needs are ported; the rest of the kit (avatar, checkbox,
 * segmented control, switch, tabs, tooltip) lands with the screens that require
 * them.
 */

export { Badge } from './Badge'
export type { BadgeProps, BadgeTone } from './Badge'

export { Button } from './Button'
export type { ButtonProps, ButtonSize, ButtonVariant } from './Button'

export { Card } from './Card'
export type { CardProps } from './Card'

export { Dialog } from './Dialog'
export type { DialogProps } from './Dialog'

export { IconButton } from './IconButton'
export type { IconButtonProps, IconButtonSize, IconButtonVariant } from './IconButton'

export { Input } from './Input'
export type { InputProps } from './Input'

export { Select } from './Select'
export type { SelectOption, SelectProps } from './Select'

export { Spinner } from './Spinner'
export type { SpinnerProps } from './Spinner'

export { Toast, ToastProvider, useToast } from './Toast'
export type { ToastProps, ToastProviderProps, ToastRequest, ToastTone } from './Toast'
