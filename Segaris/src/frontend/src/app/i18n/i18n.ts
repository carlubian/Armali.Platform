import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

import { capex } from '@/modules/capex/i18n/resources'

import { platform } from './resources'

export const fallbackLanguage = 'en-GB'

void i18n.use(initReactI18next).init({
  lng: fallbackLanguage,
  fallbackLng: fallbackLanguage,
  supportedLngs: [fallbackLanguage],
  defaultNS: 'platform',
  ns: ['platform', 'capex'],
  resources: {
    [fallbackLanguage]: { platform, capex },
  },
  interpolation: { escapeValue: false },
  returnNull: false,
})

export { i18n }
