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
})
