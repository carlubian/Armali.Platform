import { describe, expect, it } from 'vitest'

import { i18n } from './i18n'
import { platform } from './resources'

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

  it('falls back to en-GB for unsupported languages', async () => {
    await i18n.changeLanguage('es-ES')
    expect(i18n.t('common.tryAgain', { ns: 'platform' })).toBe('Try again')
  })

  it('contains every literal platform key used by the application', () => {
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

    for (const key of usedKeys) {
      const exists = i18n.exists(key, { ns: 'platform', lng: 'en-GB' })
      const hasPluralForms =
        i18n.exists(`${key}_one`, { ns: 'platform', lng: 'en-GB' }) &&
        i18n.exists(`${key}_other`, { ns: 'platform', lng: 'en-GB' })
      expect(exists || hasPluralForms, key).toBe(true)
    }
  })
})
