import { describe, expect, it } from 'vitest'

import { analytics } from '@/modules/analytics/i18n/resources'
import { assets } from '@/modules/assets/i18n/resources'
import { calendar } from '@/modules/calendar/i18n/resources'
import { capex } from '@/modules/capex/i18n/resources'
import { clothes } from '@/modules/clothes/i18n/resources'
import { configuration } from '@/modules/configuration/i18n/resources'
import { destinations } from '@/modules/destinations/i18n/resources'
import { firebird } from '@/modules/firebird/i18n/resources'
import { health } from '@/modules/health/i18n/resources'
import { inventory } from '@/modules/inventory/i18n/resources'
import { maintenance } from '@/modules/maintenance/i18n/resources'
import { mood } from '@/modules/mood/i18n/resources'
import { opex } from '@/modules/opex/i18n/resources'
import { processes } from '@/modules/processes/i18n/resources'
import { projects } from '@/modules/projects/i18n/resources'
import { recipes } from '@/modules/recipes/i18n/resources'
import { travel } from '@/modules/travel/i18n/resources'

import { i18n } from './i18n'
import { platform } from './resources'

// Namespaces a bare `t('...')` literal may resolve against. Keys prefixed with a
// namespace (for example `capex:launcher.title`) resolve directly via i18next.
const namespaces = [
  'platform',
  'analytics',
  'assets',
  'calendar',
  'capex',
  'clothes',
  'configuration',
  'destinations',
  'firebird',
  'health',
  'inventory',
  'maintenance',
  'mood',
  'opex',
  'processes',
  'projects',
  'recipes',
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

  it('registers every analytics resource key', () => {
    for (const key of leafKeys(analytics)) {
      expect(i18n.exists(key, { ns: 'analytics', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every assets resource key', () => {
    for (const key of leafKeys(assets)) {
      expect(i18n.exists(key, { ns: 'assets', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every calendar resource key', () => {
    for (const key of leafKeys(calendar)) {
      expect(i18n.exists(key, { ns: 'calendar', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every capex resource key', () => {
    for (const key of leafKeys(capex)) {
      expect(i18n.exists(key, { ns: 'capex', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every clothes resource key', () => {
    for (const key of leafKeys(clothes)) {
      expect(i18n.exists(key, { ns: 'clothes', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every configuration resource key', () => {
    for (const key of leafKeys(configuration)) {
      expect(i18n.exists(key, { ns: 'configuration', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every destinations resource key', () => {
    for (const key of leafKeys(destinations)) {
      expect(i18n.exists(key, { ns: 'destinations', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every firebird resource key', () => {
    for (const key of leafKeys(firebird)) {
      expect(i18n.exists(key, { ns: 'firebird', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every health resource key', () => {
    for (const key of leafKeys(health)) {
      expect(i18n.exists(key, { ns: 'health', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every inventory resource key', () => {
    for (const key of leafKeys(inventory)) {
      expect(i18n.exists(key, { ns: 'inventory', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every maintenance resource key', () => {
    for (const key of leafKeys(maintenance)) {
      expect(i18n.exists(key, { ns: 'maintenance', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every mood resource key', () => {
    for (const key of leafKeys(mood)) {
      expect(i18n.exists(key, { ns: 'mood', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every opex resource key', () => {
    for (const key of leafKeys(opex)) {
      expect(i18n.exists(key, { ns: 'opex', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every processes resource key', () => {
    for (const key of leafKeys(processes)) {
      expect(i18n.exists(key, { ns: 'processes', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every projects resource key', () => {
    for (const key of leafKeys(projects)) {
      expect(i18n.exists(key, { ns: 'projects', lng: 'en-GB' }), key).toBe(true)
    }
  })

  it('registers every recipes resource key', () => {
    for (const key of leafKeys(recipes)) {
      expect(i18n.exists(key, { ns: 'recipes', lng: 'en-GB' }), key).toBe(true)
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
