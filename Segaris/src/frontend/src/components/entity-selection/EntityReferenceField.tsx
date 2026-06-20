import type { ReactNode } from 'react'

import { Button, IconButton, Spinner } from '@/components/ui'

import './EntityReferenceField.css'

/**
 * The resolved selection shown by an {@link EntityReferenceField}. The field is
 * business-agnostic: a domain adapter maps its entity (an Asset, a Capex entry,
 * …) onto these display slots and keeps the field free of entity-specific code.
 */
export interface EntityReference {
  /** Primary line, typically the entity name. */
  primary: ReactNode
  /** Optional secondary metadata line, such as a code or category. */
  secondary?: ReactNode
  /** Leading icon for the selected state. */
  icon?: ReactNode
  /**
   * Marks a link that exists but could not be fully resolved — for example the
   * current user cannot read it, or it was removed. The field renders a warning
   * treatment while still allowing Change and Clear.
   */
  unavailable?: boolean
}

export interface EntityReferenceFieldProps {
  /** The current selection. `null` or `undefined` renders the empty state. */
  value?: EntityReference | null
  /** Opens the selector. Wired to both Browse (empty) and Change (selected). */
  onBrowse: () => void
  /**
   * Clears the selection. When omitted, the Clear action is not rendered, which
   * models a required reference.
   */
  onClear?: () => void

  /** Empty-state leading icon. */
  icon?: ReactNode
  /** Empty-state primary text. */
  placeholder: ReactNode
  /** Empty-state helper text shown under the placeholder. */
  helperText?: ReactNode

  /** Visible text and accessible name for the Browse action (empty state). */
  browseLabel: string
  /** Visible text and accessible name for the Change action (selected state). */
  changeLabel: string
  /** Accessible name for the icon-only Clear action (selected state). */
  clearLabel: string

  /** Disables every action and dims the control. */
  disabled?: boolean
  /**
   * Shows a busy indicator and disables the actions, e.g. while an existing
   * link is being resolved. The body keeps showing the provided value/empty
   * content.
   */
  busy?: boolean
  /** Accessible status label announced while {@link busy}. */
  busyLabel?: string

  /** Associates the control with an external label through `aria-labelledby`. */
  'aria-labelledby'?: string
  className?: string
}

function SearchIcon() {
  return (
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-3.2-3.2" />
    </svg>
  )
}

function ChangeIcon() {
  return (
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="m17 2 4 4-4 4" />
      <path d="M3 11V9a4 4 0 0 1 4-4h14" />
      <path d="m7 22-4-4 4-4" />
      <path d="M21 13v2a4 4 0 0 1-4 4H3" />
    </svg>
  )
}

function ClearIcon() {
  return (
    <svg
      width="15"
      height="15"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.2"
      strokeLinecap="round"
      aria-hidden="true"
    >
      <path d="M18 6 6 18M6 6l12 12" />
    </svg>
  )
}

/**
 * A file-selector-style control that shows an empty or selected entity link and
 * opens an entity selector. It is fully controlled by props and never owns the
 * selected entity; the caller decides what `value` to show and reacts to the
 * Browse, Change, and Clear actions.
 *
 * Adapted from the design-system entity-selector reference
 * (`docs/ui-design/segaris/screens-entity-selector.jsx`, `RefControl`).
 */
export function EntityReferenceField({
  value,
  onBrowse,
  onClear,
  icon,
  placeholder,
  helperText,
  browseLabel,
  changeLabel,
  clearLabel,
  disabled = false,
  busy = false,
  busyLabel = 'Loading',
  'aria-labelledby': ariaLabelledby,
  className = '',
}: EntityReferenceFieldProps) {
  const actionsDisabled = disabled || busy
  const filled = value != null
  const unavailable = filled && value.unavailable === true

  const cls = [
    'seg-ref',
    filled ? 'seg-ref--filled' : 'seg-ref--empty',
    unavailable ? 'seg-ref--unavailable' : '',
    disabled ? 'seg-ref--disabled' : '',
    busy ? 'seg-ref--busy' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div
      className={cls}
      role="group"
      aria-labelledby={ariaLabelledby}
      aria-busy={busy || undefined}
    >
      {(icon != null || (filled && value.icon != null)) && (
        <div className="seg-ref__icon" aria-hidden="true">
          {filled ? value.icon : icon}
        </div>
      )}

      <div className="seg-ref__body">
        {filled ? (
          <>
            <span className="seg-ref__name">{value.primary}</span>
            {value.secondary != null && (
              <span className="seg-ref__meta">{value.secondary}</span>
            )}
          </>
        ) : (
          <>
            <span className="seg-ref__placeholder">{placeholder}</span>
            {helperText != null && <span className="seg-ref__hint">{helperText}</span>}
          </>
        )}
      </div>

      <div className="seg-ref__actions">
        {busy && <Spinner size={18} label={busyLabel} className="seg-ref__spinner" />}
        {filled ? (
          <>
            {onClear != null && (
              <IconButton
                size="sm"
                variant="bare"
                label={clearLabel}
                icon={<ClearIcon />}
                onClick={onClear}
                disabled={actionsDisabled}
              />
            )}
            <Button
              variant="outline"
              size="sm"
              iconLeft={<ChangeIcon />}
              onClick={onBrowse}
              disabled={actionsDisabled}
            >
              {changeLabel}
            </Button>
          </>
        ) : (
          <Button
            variant="outline"
            size="sm"
            iconLeft={<SearchIcon />}
            onClick={onBrowse}
            disabled={actionsDisabled}
          >
            {browseLabel}
          </Button>
        )}
      </div>
    </div>
  )
}
