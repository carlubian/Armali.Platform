import { describe, expect, it } from 'vitest'

import { capex } from '@/modules/capex/i18n/resources'
import { configuration } from '@/modules/configuration/i18n/resources'
import { inventory } from '@/modules/inventory/i18n/resources'
import { opex } from '@/modules/opex/i18n/resources'
import { travel } from '@/modules/travel/i18n/resources'

import { i18n } from './i18n'
import { platform } from './resources'

// Namespaces a bare `t('...')` literal may resolve against. Keys prefixed with a
// namespace (for example `capex:launcher.title`) resolve directly via i18next.
const namespaces = [
  'platform',
  'capex',
  'configuration',
  'inventory',
  'opex',
  'travel',
] as const

function leafKeys(value: object, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, child]) => {
    const path = prefix === '' ? key : `${prefix}.${key}`
    return typeof child === 'string' ? [path] : leafKeys(child as object, path)
  })
}

describe('platform translations', () => {
  it('registers every platform resource key', () => {
    for (const key of leafKeys(platform)) {
      expect(i18n.exists(key, { ns: 'platform', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every capex resource key', () => {
    for (const key of leafKeys(capex)) {
      expect(i18n.exists(key, { ns: 'capex', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every configuration resource key', () => {
    for (const key of leafKeys(configuration)) {
      expect(i18n.exists(key, { ns: 'configuration', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every inventory resource key', () => {
    for (const key of leafKeys(inventory)) {
      expect(i18n.exists(key, { ns: 'inventory', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every opex resource key', () => {
    for (const key of leafKeys(opex)) {
      expect(i18n.exists(key, { ns: 'opex', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every travel resource key', () => {
    for (const key of leafKeys(travel)) {
      expect(i18n.exists(key, { ns: 'travel', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('falls back to en-GB for unsupported languages', async () => {
    await i18n.changeLanguage('es-ES')
    expect(i18n.t('common.tryAgain', { ns: 'platform' })).toBe('Try again')
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

    const existsInAny = (key: string) =>
      namespaces.some((ns) => i18n.exists(key, { ns, lng: 'en-GB' }))

    for (const key of usedKeys) {
      const exists = existsInAny(key)
      const hasPluralForms = existsInAny(`${key}_one`) && existsInAny(`${key}_other`)
      expect(exists || hasPluralForms, key).toBe(true)
    }
  })
})
