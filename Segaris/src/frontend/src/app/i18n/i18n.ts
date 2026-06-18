import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

import { capex } from '@/modules/capex/i18n/resources'
import { configuration } from '@/modules/configuration/i18n/resources'
import { inventory } from '@/modules/inventory/i18n/resources'
import { mood } from '@/modules/mood/i18n/resources'
import { opex } from '@/modules/opex/i18n/resources'
import { travel } from '@/modules/travel/i18n/resources'

import { platform } from './resources'

export const fallbackLanguage = 'en-GB'

void i18n.use(initReactI18next).init({
  lng: fallbackLanguage,
  fallbackLng: fallbackLanguage,
  supportedLngs: [fallbackLanguage],
  defaultNS: 'platform',
  ns: ['platform', 'capex', 'configuration', 'inventory', 'mood', 'opex', 'travel'],
  resources: {
    [fallbackLanguage]: {
      platform,
      capex,
      configuration,
      inventory,
      mood,
      opex,
      travel,
    },
  },
  interpolation: { escapeValue: false },
  returnNull: false,
})

export { i18n }
