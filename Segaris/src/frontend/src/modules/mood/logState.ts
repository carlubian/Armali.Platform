import type { MoodEntryRangeQuery } from '@/app/api/mood'

/** The household's local date (Europe/Madrid) as a `yyyy-mm-dd` string. */
export function householdToday(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Madrid',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date())
}

function toUtcDate(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1, day))
}

function fromUtcDate(date: Date): string {
  return date.toISOString().slice(0, 10)
}

/** True when `value` is a real `yyyy-mm-dd` civil date that round-trips. */
export function isIsoDate(value: string | null | undefined): value is string {
  if (value == null || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return false
  return fromUtcDate(toUtcDate(value)) === value
}

export function addDays(iso: string, days: number): string {
  const date = toUtcDate(iso)
  date.setUTCDate(date.getUTCDate() + days)
  return fromUtcDate(date)
}

/** The Monday (Europe/Madrid civil week start) of the week containing `iso`. */
export function mondayOf(iso: string): string {
  const date = toUtcDate(iso)
  const day = date.getUTCDay() // 0 = Sunday .. 6 = Saturday
  const offset = day === 0 ? -6 : 1 - day
  return addDays(iso, offset)
}

export function addWeeks(monday: string, delta: number): string {
  return addDays(monday, delta * 7)
}

/** The seven Monday-to-Sunday civil dates of the week starting at `monday`. */
export function weekDates(monday: string): string[] {
  return Array.from({ length: 7 }, (_, index) => addDays(monday, index))
}

export function weekEnd(monday: string): string {
  return addDays(monday, 6)
}

export function weekRangeQuery(monday: string): MoodEntryRangeQuery {
  return { from: monday, to: weekEnd(monday) }
}

/**
 * Resolves the selected log week (a Monday) from the `week` URL parameter,
 * defaulting to the week containing `today` when it is missing or malformed.
 */
export function parseSelectedWeek(params: URLSearchParams, today: string): string {
  const week = params.get('week')
  return mondayOf(isIsoDate(week) ? week : today)
}

export type MoodEntryDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; entryId: number }

/** Parses the entry dialog state from the `newEntry` and `entryId` URL parameters. */
export function parseEntryDialogState(params: URLSearchParams): MoodEntryDialogState {
  if (params.get('newEntry') === 'true') return { mode: 'create' }

  const entryId = Number(params.get('entryId'))
  if (Number.isInteger(entryId) && entryId > 0) return { mode: 'edit', entryId }

  return { mode: 'closed' }
}
