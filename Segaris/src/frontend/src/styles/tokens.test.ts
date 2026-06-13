/// <reference types="node" />
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

import { describe, expect, it } from 'vitest'

// Vitest runs with `css: false`, so importing the stylesheets (even via `?raw`)
// yields empty modules. Read the token sources from disk instead to assert on
// their real content.
function readStyle(relative: string): string {
  return readFileSync(fileURLToPath(new URL(relative, import.meta.url)), 'utf8')
}

describe('design tokens', () => {
  it('defines the core semantic color aliases', () => {
    const colors = readStyle('./tokens/colors.css')
    for (const token of [
      '--surface-app',
      '--surface-card-solid',
      '--text-primary',
      '--accent',
      '--accent-secondary',
      '--action',
      '--success',
      '--danger',
      '--border-default',
      '--ring-focus',
    ]) {
      expect(colors).toContain(`${token}:`)
    }
  })

  it('defines the typography, spacing, and effect scales', () => {
    expect(readStyle('./tokens/typography.css')).toContain('--font-display:')
    expect(readStyle('./tokens/typography.css')).toContain('--text-base:')
    expect(readStyle('./tokens/spacing.css')).toContain('--space-4:')
    expect(readStyle('./tokens/effects.css')).toContain('--radius-control:')
    expect(readStyle('./tokens/effects.css')).toContain('--glow-card:')
  })

  it('self-hosts the League Spartan and Nunito web fonts', () => {
    const fonts = readStyle('./tokens/fonts.css')
    expect(fonts).toContain('League Spartan')
    expect(fonts).toContain('Nunito')
    expect(fonts).toContain('LeagueSpartan-600.woff2')
    expect(fonts).toContain('Nunito-700.woff2')
  })

  it('drops the unused --sidebar-w token', () => {
    expect(readStyle('./tokens/spacing.css')).not.toContain('--sidebar-w:')
    expect(readStyle('./tokens/spacing.css')).toContain('--topbar-h:')
  })

  it('provides the aurora and glass surface helpers', () => {
    const base = readStyle('./tokens/base.css')
    expect(base).toContain('.armali-aurora')
    expect(base).toContain('.armali-glass')
  })

  it('global stylesheet imports every token layer in order', () => {
    const global = readStyle('./global.css')
    const order = [
      './tokens/fonts.css',
      './tokens/colors.css',
      './tokens/typography.css',
      './tokens/spacing.css',
      './tokens/effects.css',
      './tokens/base.css',
    ]
    const positions = order.map((path) => global.indexOf(`@import '${path}'`))
    expect(positions.every((position) => position >= 0)).toBe(true)
    const sorted = [...positions].sort((a, b) => a - b)
    expect(positions).toEqual(sorted)
  })
})
