import type { MoodDashboardScale } from '@/app/api/mood'
import { moodDashboardScales } from '@/app/api/mood'

export const defaultDashboardScale: MoodDashboardScale = 'year'

const slotsPerYear: Record<MoodDashboardScale, number> = {
  year: 1,
  semester: 2,
  quarter: 4,
  month: 12,
}

interface PeriodParts {
  year: number
  /** 1-based slot within the year; always 1 for the `year` scale. */
  index: number
}

export function parseScale(
  value: string | null | undefined,
): MoodDashboardScale | null {
  return value != null && (moodDashboardScales as readonly string[]).includes(value)
    ? (value as MoodDashboardScale)
    : null
}

function isValidYear(year: number): boolean {
  return Number.isInteger(year) && year >= 1 && year <= 9999
}

function pad4(year: number): string {
  return year.toString().padStart(4, '0')
}

function formatToken(scale: MoodDashboardScale, parts: PeriodParts): string {
  switch (scale) {
    case 'year':
      return pad4(parts.year)
    case 'semester':
      return `${pad4(parts.year)}-S${parts.index}`
    case 'quarter':
      return `${pad4(parts.year)}-Q${parts.index}`
    case 'month':
      return `${pad4(parts.year)}-${parts.index.toString().padStart(2, '0')}`
  }
}

function tryParseParts(scale: MoodDashboardScale, token: string): PeriodParts | null {
  if (scale === 'year') {
    if (!/^\d{4}$/.test(token)) return null
    const year = Number(token)
    return isValidYear(year) ? { year, index: 1 } : null
  }

  if (scale === 'month') {
    const match = /^(\d{4})-(\d{2})$/.exec(token)
    if (match == null) return null
    const year = Number(match[1])
    const index = Number(match[2])
    return isValidYear(year) && index >= 1 && index <= 12 ? { year, index } : null
  }

  const marker = scale === 'semester' ? 'S' : 'Q'
  const max = slotsPerYear[scale]
  const match = new RegExp(`^(\\d{4})-${marker}(\\d)$`).exec(token)
  if (match == null) return null
  const year = Number(match[1])
  const index = Number(match[2])
  return isValidYear(year) && index >= 1 && index <= max ? { year, index } : null
}

function currentParts(scale: MoodDashboardScale, today: string): PeriodParts {
  const [year, month] = today.split('-').map(Number)
  switch (scale) {
    case 'year':
      return { year, index: 1 }
    case 'semester':
      return { year, index: Math.floor((month - 1) / 6) + 1 }
    case 'quarter':
      return { year, index: Math.floor((month - 1) / 3) + 1 }
    case 'month':
      return { year, index: month }
  }
}

function shift(
  scale: MoodDashboardScale,
  parts: PeriodParts,
  delta: number,
): PeriodParts {
  const slots = slotsPerYear[scale]
  const zeroBased = parts.index - 1 + delta
  const yearDelta = Math.floor(zeroBased / slots)
  const index = zeroBased - yearDelta * slots + 1
  return { year: parts.year + yearDelta, index }
}

/** The strict period token of `scale` containing `today` (a `yyyy-mm-dd` date). */
export function currentPeriod(scale: MoodDashboardScale, today: string): string {
  return formatToken(scale, currentParts(scale, today))
}

/** Normalizes a period token for `scale`, returning `null` when malformed. */
export function parsePeriod(
  scale: MoodDashboardScale,
  token: string | null,
): string | null {
  if (token == null) return null
  const parts = tryParseParts(scale, token.trim())
  return parts == null ? null : formatToken(scale, parts)
}

/** The same-scale period token shifted by `delta`, rolling over year boundaries. */
export function shiftPeriod(
  scale: MoodDashboardScale,
  token: string,
  delta: number,
): string | null {
  const parts = tryParseParts(scale, token)
  return parts == null ? null : formatToken(scale, shift(scale, parts, delta))
}

export function previousPeriod(
  scale: MoodDashboardScale,
  token: string,
): string | null {
  return shiftPeriod(scale, token, -1)
}

export function nextPeriod(scale: MoodDashboardScale, token: string): string | null {
  return shiftPeriod(scale, token, 1)
}

export interface DashboardState {
  scale: MoodDashboardScale
  period: string
}

/**
 * Resolves the dashboard scale and period from the URL, defaulting to the current
 * year and falling back to the current period when the token does not match the
 * selected scale.
 */
export function parseDashboardState(
  params: URLSearchParams,
  today: string,
): DashboardState {
  const scale = parseScale(params.get('scale')) ?? defaultDashboardScale
  const period = parsePeriod(scale, params.get('period')) ?? currentPeriod(scale, today)
  return { scale, period }
}
