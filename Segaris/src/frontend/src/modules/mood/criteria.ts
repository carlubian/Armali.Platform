import type {
  MoodAlignment,
  MoodDirection,
  MoodEnergy,
  MoodSource,
} from '@/app/api/mood'

/**
 * Presentational metadata for the fixed Mood criteria and the 1–5 score. The
 * derived-emotion code is translated through the `mood` i18next namespace; the
 * tone colours below are purely visual and never carry meaning the colour-blind
 * user would miss, because every pill and chip also shows its text label.
 */

export type MoodTone = 'aqua' | 'gold' | 'azure' | 'sea' | 'rose' | 'neutral'

/** A `[soft background, ink]` pair of design-token CSS variables for a tone. */
export const moodToneVars: Record<MoodTone, readonly [string, string]> = {
  aqua: ['var(--aqua-100)', 'var(--aqua-700)'],
  gold: ['var(--gold-100)', 'var(--gold-600)'],
  azure: ['var(--azure-100)', 'var(--azure-600)'],
  sea: ['var(--sea-100)', 'var(--sea-600)'],
  rose: ['var(--terracotta-100)', 'var(--terracotta-600)'],
  neutral: ['var(--surface-sunken)', 'var(--text-secondary)'],
}

export const energyTone: Record<MoodEnergy, MoodTone> = {
  Low: 'azure',
  Medium: 'aqua',
  High: 'gold',
}

export const alignmentTone: Record<MoodAlignment, MoodTone> = {
  Negative: 'rose',
  Medium: 'gold',
  Positive: 'sea',
}

export const directionTone: Record<MoodDirection, MoodTone> = {
  Harmony: 'aqua',
  Defensive: 'azure',
  Offensive: 'rose',
  Stability: 'gold',
}

export const sourceTone: Record<MoodSource, MoodTone> = {
  Internal: 'aqua',
  External: 'azure',
}

/** Maps a 1–5 score (rounded) to a tone, from terracotta (low) to sea (high). */
export function scoreTone(score: number): MoodTone {
  const rounded = Math.round(score)
  if (rounded <= 2) return 'rose'
  if (rounded === 3) return 'gold'
  return 'sea'
}

/** The ink colour of a rounded score, used by the weekly chart bars. */
export function scoreColor(score: number): string {
  return moodToneVars[scoreTone(score)][1]
}

/** Arithmetic mean of the scores, or `null` for an empty list (a missing day). */
export function averageScore(scores: readonly number[]): number | null {
  if (scores.length === 0) return null
  return scores.reduce((total, score) => total + score, 0) / scores.length
}
