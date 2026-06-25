import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

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

import { platform } from './resources'

export const fallbackLanguage = 'en-GB'

void i18n.use(initReactI18next).init({
  lng: fallbackLanguage,
  fallbackLng: fallbackLanguage,
  supportedLngs: [fallbackLanguage],
  defaultNS: 'platform',
  ns: [
    'platform',
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
  ],
  resources: {
    [fallbackLanguage]: {
      platform,
      assets,
      calendar,
      capex,
      clothes,
      configuration,
      destinations,
      firebird,
      health,
      inventory,
      maintenance,
      mood,
      opex,
      processes,
      projects,
      recipes,
      travel,
    },
  },
  interpolation: { escapeValue: false },
  returnNull: false,
})

export { i18n }
