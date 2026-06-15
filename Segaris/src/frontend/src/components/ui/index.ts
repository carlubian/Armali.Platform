/**
 * Project Armali design-system primitives, ported as typed React components.
 *
 * These are generic, business-agnostic UI building blocks. Platform and domain
 * modules import them from this single public entry point.
 */

export { Avatar } from './Avatar'
export type { AvatarProps, AvatarSize, AvatarStatus } from './Avatar'

export { Badge } from './Badge'
export type { BadgeProps, BadgeTone } from './Badge'

export { Button } from './Button'
export type { ButtonProps, ButtonSize, ButtonVariant } from './Button'

export { Card } from './Card'
export type { CardProps } from './Card'

export { Checkbox } from './Checkbox'
export type { CheckboxProps } from './Checkbox'

export { Dialog } from './Dialog'
export type { DialogProps } from './Dialog'

export { IconButton } from './IconButton'
export type { IconButtonProps, IconButtonSize, IconButtonVariant } from './IconButton'

export { Input } from './Input'
export type { InputProps } from './Input'

export { SegmentedControl } from './SegmentedControl'
export type {
  SegmentedControlProps,
  SegmentOption,
  SegmentTone,
} from './SegmentedControl'

export { Select } from './Select'
export type { SelectOption, SelectProps } from './Select'

export { Spinner } from './Spinner'
export type { SpinnerProps } from './Spinner'

export { Switch } from './Switch'
export type { SwitchProps } from './Switch'

export { Tabs } from './Tabs'
export type { TabItem, TabsProps, TabsVariant } from './Tabs'

export { Toast } from './Toast'
export type { ToastProps, ToastTone } from './Toast'

export { Tooltip } from './Tooltip'
export type { TooltipProps, TooltipSide } from './Tooltip'
