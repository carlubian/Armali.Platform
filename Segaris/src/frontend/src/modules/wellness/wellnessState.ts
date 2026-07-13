import type { WellnessDaysQuery } from '@/app/api/wellness'

/**
 * The Wellness today surface has no URL-backed state of its own: it renders the
 * current day's tasks and score and needs no historical navigation. The only
 * derived query is the inclusive days range the Mood weekly log consumes through
 * `GET /api/wellness/days`, built here from the visible week's civil-date bounds so
 * the seam Mood depends on is frozen in one place.
 */
export function toWellnessDaysQuery(
  from: string,
  to: string,
): WellnessDaysQuery | null {
  if (from === '' || to === '' || to < from) {
    return null
  }

  return { from, to }
}
