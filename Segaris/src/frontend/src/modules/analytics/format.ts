import { fallbackLanguage } from '@/app/i18n/i18n'

/**
 * Analytics formatting and palette helpers.
 *
 * Amounts are always EUR (the backend normalizes every projection to EUR at
 * read time), so currency formatting is fixed to EUR and only the locale
 * varies. Recharts paints raw SVG, so chart colours must be concrete hex values
 * rather than `var(--token)` references, which do not resolve inside SVG fills.
 */

/** Whole-euro amount, e.g. `€1,235`. Used in summaries, tables, and tooltips. */
export function formatEur(value: number, language = fallbackLanguage): string {
  return new Intl.NumberFormat(language, {
    style: 'currency',
    currency: 'EUR',
    maximumFractionDigits: 0,
  }).format(Math.round(value || 0))
}

/** Compact euro amount for dense chart axes, e.g. `€1.2k`, `€12k`, `€450`. */
export function formatEurCompact(value: number): string {
  const magnitude = Math.abs(value)
  if (magnitude >= 1000) {
    const sign = value < 0 ? '-' : ''
    const thousands = (magnitude / 1000)
      .toFixed(magnitude >= 10000 ? 0 : 1)
      .replace(/\.0$/, '')
    return `${sign}€${thousands}k`
  }
  return `€${Math.round(value)}`
}

/** Share of a total as a percentage, e.g. `6%` or `4.2%` for small fractions. */
export function formatPercent(fraction: number): string {
  return `${(fraction * 100).toFixed(fraction < 0.1 ? 1 : 0)}%`
}

/** Signed year-over-year delta, e.g. `+12%`, `-3.4%`, `0%`. */
export function formatDelta(fraction: number): string {
  const sign = fraction > 0 ? '+' : ''
  return `${sign}${(fraction * 100).toFixed(Math.abs(fraction) < 0.1 ? 1 : 0)}%`
}

/** Year-over-year ratio guarding against a zero previous-year denominator. */
export function yearOverYear(current: number, previous: number): number | null {
  if (previous === 0) return null
  return (current - previous) / previous
}

/** Short month label (`Jan` … `Dec`) for a 1-based month number. */
export function monthShortLabel(month: number, language = fallbackLanguage): string {
  return new Intl.DateTimeFormat(language, {
    month: 'short',
    timeZone: 'UTC',
  }).format(new Date(Date.UTC(2000, month - 1, 1)))
}

/**
 * Concrete chart colours composed from Project Armali palette hex values.
 * Expenses read in Mediterranean azure, income and positive net in sea green,
 * negative net in terracotta, with warm-ink grid and axis hues.
 */
export const analyticsPalette = {
  expense: '#3a7ca5', // azure-500
  income: '#519b79', // sea-500
  netPositive: '#519b79', // sea-500
  netNegative: '#cb5742', // terracotta-500
  grid: 'rgba(124, 110, 86, 0.18)',
  axis: '#9a9081', // ink-400
  ink: '#2c2823', // ink-900
} as const

export type AnalyticsSeriesTone = 'expense' | 'income'
