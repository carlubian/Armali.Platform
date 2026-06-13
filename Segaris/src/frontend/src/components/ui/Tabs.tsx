import { useState } from 'react'
import type { ComponentPropsWithRef, ReactNode } from 'react'

import './Tabs.css'

export type TabsVariant = 'pill' | 'line'

export interface TabItem {
  value: string
  label: ReactNode
  icon?: ReactNode
  /** Optional count pill rendered after the label. */
  count?: number
}

export interface TabsProps extends Omit<
  ComponentPropsWithRef<'div'>,
  'onChange' | 'defaultValue'
> {
  tabs?: ReadonlyArray<TabItem | string>
  /** Controlled active value. */
  value?: string
  /** Initial value when uncontrolled. */
  defaultValue?: string
  onChange?: (value: string) => void
  variant?: TabsVariant
}

/**
 * Project Armali segmented tabs (pill or underline).
 *
 * Ported from the design-system reference (`components/navigation/Tabs.jsx`).
 * Works controlled (via `value` + `onChange`) or uncontrolled (via
 * `defaultValue`).
 */
export function Tabs({
  tabs = [],
  value,
  defaultValue,
  onChange,
  variant = 'pill',
  className = '',
  ...rest
}: TabsProps) {
  const norm: TabItem[] = tabs.map((t) =>
    typeof t === 'string' ? { value: t, label: t } : t,
  )
  const [internal, setInternal] = useState<string | undefined>(
    defaultValue ?? norm[0]?.value,
  )
  const active = value !== undefined ? value : internal

  const select = (v: string) => {
    if (value === undefined) setInternal(v)
    onChange?.(v)
  }

  return (
    <div
      role="tablist"
      className={['arm-tabs', variant === 'line' ? 'arm-tabs--line' : '', className]
        .filter(Boolean)
        .join(' ')}
      {...rest}
    >
      {norm.map((t) => (
        <button
          key={t.value}
          type="button"
          role="tab"
          aria-selected={active === t.value}
          className={['arm-tab', active === t.value ? 'arm-tab--active' : '']
            .filter(Boolean)
            .join(' ')}
          onClick={() => {
            select(t.value)
          }}
        >
          {t.icon}
          {t.label}
          {t.count != null && <span className="arm-tab__count">{t.count}</span>}
        </button>
      ))}
    </div>
  )
}
