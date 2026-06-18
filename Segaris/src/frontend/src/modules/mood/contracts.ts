import { z } from 'zod'

import type {
  CreateMoodEntryRequest,
  MoodDashboardQuery,
  MoodDerivedEmotionQuery,
  MoodEntryRangeQuery,
} from '@/app/api/mood'
import { moodNotesMaxLength, moodScoreMax, moodScoreMin } from '@/app/api/mood'

export const moodKeys = {
  all: ['mood'] as const,
  options: () => [...moodKeys.all, 'options'] as const,
  derivedEmotion: (query: MoodDerivedEmotionQuery | null) =>
    [...moodKeys.all, 'derivedEmotion', query] as const,
  entries: () => [...moodKeys.all, 'entries'] as const,
  entryRange: (query: MoodEntryRangeQuery) =>
    [...moodKeys.entries(), 'range', query] as const,
  entry: (entryId: number) => [...moodKeys.entries(), entryId] as const,
  dashboard: () => [...moodKeys.all, 'dashboard'] as const,
  dashboardPeriod: (query: MoodDashboardQuery) =>
    [...moodKeys.dashboard(), query] as const,
}

const optionalNotes = z
  .string()
  .trim()
  .max(moodNotesMaxLength)
  .transform((value) => (value.length === 0 ? null : value))
  .nullable()

export const moodEntryRequestSchema = z.object({
  entryDate: z.iso.date(),
  score: z.number().int().min(moodScoreMin).max(moodScoreMax),
  energy: z.enum(['Low', 'Medium', 'High']),
  alignment: z.enum(['Negative', 'Medium', 'Positive']),
  direction: z.enum(['Harmony', 'Defensive', 'Offensive', 'Stability']),
  source: z.enum(['Internal', 'External']),
  notes: optionalNotes,
}) satisfies z.ZodType<CreateMoodEntryRequest>

export type MoodEntryFormInput = z.input<typeof moodEntryRequestSchema>
export type MoodEntryFormValues = z.output<typeof moodEntryRequestSchema>
