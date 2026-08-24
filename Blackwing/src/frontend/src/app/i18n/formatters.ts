import { fallbackLanguage } from './i18n'

/**
 * Locale-aware formatting helpers.
 *
 * Blackwing is a personal photo library: dates come from image capture metadata
 * and counts from the gallery, so both are formatted for the interface language
 * rather than the browser locale.
 */
const libraryTimeZone = 'Europe/Madrid'

export function formatDate(value: Date | string, language = fallbackLanguage): string {
  return new Intl.DateTimeFormat(language, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    timeZone: libraryTimeZone,
  }).format(typeof value === 'string' ? new Date(value) : value)
}

export function formatDateTime(
  value: Date | string,
  language = fallbackLanguage,
): string {
  return new Intl.DateTimeFormat(language, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: libraryTimeZone,
  }).format(typeof value === 'string' ? new Date(value) : value)
}

export function formatNumber(value: number, language = fallbackLanguage): string {
  return new Intl.NumberFormat(language).format(value)
}
