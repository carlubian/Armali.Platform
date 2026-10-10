import { z } from 'zod'

import type { MoodAlignment, MoodEnergy, MoodEntry, MoodIntent } from '@/app/api/mood'
import { moodNotesMaxLength, moodScoreMax, moodScoreMin } from '@/app/api/mood'

/**
 * React Hook Form values for the entry dialog. Score and the three criteria start
 * as `null` because a new entry has no default selection until the user chooses
 * one; notes default to an empty string and become `null` in the request.
 */
export interface MoodEntryFormValues {
  entryDate: string
  score: number | null
  energy: MoodEnergy | null
  alignment: MoodAlignment | null
  intent: MoodIntent | null
  notes: string
}

export interface MoodEntrySchemaMessages {
  dateRequired: string
  scoreRequired: string
  energyRequired: string
  alignmentRequired: string
  intentRequired: string
  notesTooLong: string
}

/**
 * Wraps a value schema so its *input* accepts the form's `null` placeholder while
 * its *output* is the chosen value. This lets the dialog default score/criteria to
 * `null` yet still resolve to a validated `CreateMoodEntryRequest` on submit.
 */
function chosen<S extends z.ZodTypeAny>(inner: S, message: string) {
  return inner
    .nullable()
    .refine((value) => value !== null, { message })
    .transform((value) => value as NonNullable<z.output<S>>)
}

/**
 * Builds the entry schema. `z.input` matches {@link MoodEntryFormValues}; `z.output`
 * is a {@link CreateMoodEntryRequest} (notes trimmed, blank notes become `null`).
 */
export function createMoodEntrySchema(messages: MoodEntrySchemaMessages) {
  return z.object({
    entryDate: z.string().min(1, messages.dateRequired),
    score: chosen(
      z.number().int().min(moodScoreMin).max(moodScoreMax),
      messages.scoreRequired,
    ),
    energy: chosen(z.enum(['Low', 'Medium', 'High']), messages.energyRequired),
    alignment: chosen(
      z.enum(['Negative', 'Medium', 'Positive']),
      messages.alignmentRequired,
    ),
    intent: chosen(
      z.enum(['Stay', 'Defend', 'Attack', 'Rebuild', 'Explore']),
      messages.intentRequired,
    ),
    notes: z
      .string()
      .max(moodNotesMaxLength, messages.notesTooLong)
      .transform((value) => {
        const trimmed = value.trim()
        return trimmed.length === 0 ? null : trimmed
      }),
  })
}

export type MoodEntrySchema = ReturnType<typeof createMoodEntrySchema>

/** The household's local date (Europe/Madrid) as a `yyyy-mm-dd` string. */
export function householdToday(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Madrid',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date())
}

/**
 * A blank entry. `EntryDate` defaults to today in Europe/Madrid regardless of the
 * week the Log is currently showing, matching the requirements' creation defaults.
 */
export function buildDefaults(today: string): MoodEntryFormValues {
  return {
    entryDate: today,
    score: null,
    energy: null,
    alignment: null,
    intent: null,
    notes: '',
  }
}

export function fromEntry(entry: MoodEntry): MoodEntryFormValues {
  return {
    entryDate: entry.entryDate,
    score: entry.score,
    energy: entry.energy,
    alignment: entry.alignment,
    intent: entry.intent,
    notes: entry.notes ?? '',
  }
}
