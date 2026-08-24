import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

import { platform } from './resources'

/**
 * The interface ships in Spanish, but it goes through i18next from day one so a
 * second language never requires touching components. `platform` is the only
 * namespace at this stage; product modules add their own under
 * `src/modules/<module>/i18n/resources.ts` as they land.
 */
export const fallbackLanguage = 'es-ES'

void i18n.use(initReactI18next).init({
  lng: fallbackLanguage,
  fallbackLng: fallbackLanguage,
  supportedLngs: [fallbackLanguage],
  defaultNS: 'platform',
  ns: ['platform'],
  resources: {
    [fallbackLanguage]: {
      platform,
    },
  },
  interpolation: { escapeValue: false },
  returnNull: false,
})

export { i18n }
