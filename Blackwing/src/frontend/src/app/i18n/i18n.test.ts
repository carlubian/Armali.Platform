import { describe, expect, it } from 'vitest'

import { fallbackLanguage, i18n } from './i18n'
import { platform } from './resources'

const namespaces = ['platform'] as const

function leafKeys(value: object, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, child]) => {
    const path = prefix === '' ? key : `${prefix}.${key}`
    return typeof child === 'string' ? [path] : leafKeys(child as object, path)
  })
}

describe('i18next configuration', () => {
  it('runs in Spanish and supports only Spanish for now', () => {
    expect(fallbackLanguage).toBe('es-ES')
    expect(i18n.options.supportedLngs).toContain('es-ES')
    expect(i18n.options.defaultNS).toBe('platform')
  })

  it('registers every platform resource key', () => {
    for (const key of leafKeys(platform)) {
      expect(i18n.exists(key, { ns: 'platform', lng: fallbackLanguage }), key).toBe(
        true,
      )
    }
  })

  it('registers a label, eyebrow, and title for every navigation entry', () => {
    for (const entry of ['gallery', 'upload', 'review', 'tags', 'settings']) {
      for (const part of ['label', 'eyebrow', 'title']) {
        const key = `shell.nav.${entry}.${part}`
        expect(i18n.exists(key, { ns: 'platform', lng: fallbackLanguage }), key).toBe(
          true,
        )
      }
    }
  })

  it('contains every literal translation key used by the application', () => {
    const sourceFiles = import.meta.glob<string>('../../**/*.{ts,tsx}', {
      eager: true,
      import: 'default',
      query: '?raw',
    })
    const usedKeys = new Set<string>()
    const keyPattern = /(?:\bt|i18n\.t)\(\s*['"]([^'"]+)['"]/g

    for (const [path, source] of Object.entries(sourceFiles)) {
      if (path.endsWith('.test.ts') || path.endsWith('.test.tsx')) continue
      for (const match of source.matchAll(keyPattern)) usedKeys.add(match[1])
    }

    // The scan must actually find something; an empty set would pass vacuously.
    expect(usedKeys.size).toBeGreaterThan(0)

    const existsInAny = (key: string) =>
      namespaces.some((ns) => i18n.exists(key, { ns, lng: fallbackLanguage }))

    for (const key of usedKeys) {
      const exists = existsInAny(key)
      const hasPluralForms = existsInAny(`${key}_one`) && existsInAny(`${key}_other`)
      expect(exists || hasPluralForms, key).toBe(true)
    }
  })
})
