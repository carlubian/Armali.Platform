import type { ComponentPropsWithRef, ReactNode } from 'react'

import './SegmentedControl.css'

/** Active-fill colour for the selected segment. */
export type SegmentTone = 'accent' | 'success' | 'neutral' | 'danger'

export interface SegmentOption {
  value: string
  label: ReactNode
  /** Optional leading icon, sized by the caller. */
  icon?: ReactNode
  /** Colour of this segment while selected. Defaults to `accent`. */
  tone?: SegmentTone
}

export interface SegmentedControlProps extends Omit<
  ComponentPropsWithRef<'input'>,
  'type' | 'children' | 'value' | 'defaultValue'
> {
  /** The two-or-more mutually exclusive choices. */
  options: ReadonlyArray<SegmentOption>
  /** Controlled selected value. */
  value?: string
  /** Selected value for uncontrolled use; omit when wiring via RHF `register`. */
  defaultValue?: string
}

/**
 * Project Armali segmented control: a row of radio "buttons" for a small,
 * fixed set of mutually exclusive choices (e.g. Income/Expense). More glanceable
 * than a `<select>` when every option fits on screen.
 *
 * Built on real `<input type="radio">` elements so it keeps native keyboard and
 * screen-reader semantics. The registration props (`name`, `onChange`, `onBlur`,
 * `ref`) are spread across every option, so `{...register('field')}` from React
 * Hook Form attaches directly — leave `value`/`defaultValue` unset in that case
 * and let the form own the selection. For standalone use, pass `value` +
 * `onChange` (controlled) or `defaultValue` (uncontrolled). Always provide a
 * group name via `aria-label` or `aria-labelledby`.
 */
export function SegmentedControl({
  options,
  value,
  defaultValue,
  disabled = false,
  className = '',
  'aria-label': ariaLabel,
  'aria-labelledby': ariaLabelledby,
  ...rest
}: SegmentedControlProps) {
  const controlled = value !== undefined
  return (
    <div
      role="radiogroup"
      aria-label={ariaLabel}
      aria-labelledby={ariaLabelledby}
      className={['arm-segmented', disabled ? 'arm-segmented--disabled' : '', className]
        .filter(Boolean)
        .join(' ')}
    >
      {options.map((option) => {
        const selection = controlled
          ? { checked: option.value === value }
          : defaultValue !== undefined
            ? { defaultChecked: option.value === defaultValue }
            : {}
        return (
          <label key={option.value} className="arm-segmented__option">
            <input
              type="radio"
              className="arm-segmented__input"
              disabled={disabled}
              {...rest}
              {...selection}
              value={option.value}
            />
            <span className="arm-segmented__face" data-tone={option.tone ?? 'accent'}>
              {option.icon != null && (
                <span className="arm-segmented__icon" aria-hidden="true">
                  {option.icon}
                </span>
              )}
              {option.label}
            </span>
          </label>
        )
      })}
    </div>
  )
}
