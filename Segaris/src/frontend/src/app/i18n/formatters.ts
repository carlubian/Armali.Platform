import { fallbackLanguage } from './i18n'

const householdTimeZone = 'Europe/Madrid'

export function formatDate(value: Date | string, language = fallbackLanguage): string {
  return new Intl.DateTimeFormat(language, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    timeZone: householdTimeZone,
  }).format(typeof value === 'string' ? new Date(value) : value)
}

export function formatNumber(value: number, language = fallbackLanguage): string {
  return new Intl.NumberFormat(language).format(value)
}

export function formatCurrency(
  value: number,
  currency = 'EUR',
  language = fallbackLanguage,
): string {
  return new Intl.NumberFormat(language, { style: 'currency', currency }).format(value)
}
