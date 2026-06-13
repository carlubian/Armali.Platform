import type { ComponentPropsWithRef } from 'react'

import './Avatar.css'

export type AvatarSize = 'sm' | 'md' | 'lg'
export type AvatarStatus = 'online' | 'away' | 'busy'

export interface AvatarProps extends Omit<ComponentPropsWithRef<'span'>, 'children'> {
  /** Display name used for the initials fallback and the accessible name. */
  name?: string
  /** Image source. When omitted, an initials fallback is shown. */
  src?: string
  size?: AvatarSize
  /** Optional presence indicator dot. */
  status?: AvatarStatus
}

const GRADS = ['', 'arm-avatar--gold', 'arm-avatar--sea'] as const

/** Deterministically pick a fallback gradient from the name. */
function pick(str = ''): string {
  let h = 0
  for (let i = 0; i < str.length; i++) {
    h = (h * 31 + str.charCodeAt(i)) >>> 0
  }
  return GRADS[h % GRADS.length]
}

/**
 * Project Armali avatar with an image or initials fallback.
 *
 * Ported from the design-system reference (`components/surfaces/Avatar.jsx`).
 * The initials fallback exposes the name through `role="img"` + `aria-label`.
 */
export function Avatar({
  name = '',
  src,
  size = 'md',
  status,
  className = '',
  ...rest
}: AvatarProps) {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0])
    .join('')
    .toUpperCase()

  const cls = [
    'arm-avatar',
    size !== 'md' ? `arm-avatar--${size}` : '',
    src ? '' : pick(name),
    className,
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <span
      className={cls}
      role={src ? undefined : 'img'}
      aria-label={src ? undefined : name || undefined}
      {...rest}
    >
      {src ? <img src={src} alt={name} /> : initials || '?'}
      {status != null && (
        <span className={`arm-avatar__status arm-avatar__status--${status}`} />
      )}
    </span>
  )
}
