/**
 * Civil-date helpers for the Processes table, evaluated in the household time
 * zone so "overdue" and "due soon" match the backend launcher-attention window
 * (today through today plus 7 natural days in Europe/Madrid).
 */
const householdTimeZone = 'Europe/Madrid'

const MS_PER_DAY = 86_400_000

export type DueUrgency = 'overdue' | 'soon' | 'later' | 'none'

/** Today's civil date in the household time zone, as `yyyy-MM-dd`. */
export function todayCivil(now: Date = new Date()): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: householdTimeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(now)
  // en-CA renders ISO-like `yyyy-MM-dd`.
  return parts
}

function toUtcDays(civil: string): number | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(civil)) return null
  const [year, month, day] = civil.split('-').map(Number)
  return Math.floor(Date.UTC(year, month - 1, day) / MS_PER_DAY)
}

/** Whole days from today to the given civil date; negative when in the past. */
export function daysUntil(civil: string | null, now: Date = new Date()): number | null {
  if (civil == null) return null
  const target = toUtcDays(civil)
  const today = toUtcDays(todayCivil(now))
  if (target == null || today == null) return null
  return target - today
}

/** Urgency band against the inclusive today-to-today-plus-7 window. */
export function dueUrgency(civil: string | null, now: Date = new Date()): DueUrgency {
  const days = daysUntil(civil, now)
  if (days == null) return 'none'
  if (days < 0) return 'overdue'
  if (days <= 7) return 'soon'
  return 'later'
}
