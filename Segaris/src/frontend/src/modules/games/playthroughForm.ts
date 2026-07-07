import { z } from 'zod'

import type {
  GamesVisibility,
  Playthrough,
  PlaythroughRequest,
  PlaythroughStatus,
} from '@/app/api/games'

/**
 * Form-shaped playthrough values. `gameId` is chosen through the floating game
 * selector and `tags` through the tag input, so both are managed outside the DOM
 * and written back with React Hook Form's `setValue`. Month and year stay as the
 * string values the native `<select>` controls emit and are widened to numbers in
 * {@link toPlaythroughRequest}.
 */
export interface PlaythroughFormValues {
  name: string
  gameId: number | null
  startMonth: string
  startYear: string
  status: PlaythroughStatus
  tags: string[]
  visibility: GamesVisibility
}

export interface PlaythroughSchemaMessages {
  nameRequired: string
  nameTooLong: string
  gameRequired: string
  startMonthRequired: string
  startYearRequired: string
  tagTooLong: string
}

function isMonth(value: string): boolean {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed >= 1 && parsed <= 12
}

function isYear(value: string): boolean {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed >= 1 && parsed <= 9999
}

export function createPlaythroughSchema(messages: PlaythroughSchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    gameId: z
      .number()
      .int()
      .positive()
      .nullable()
      .refine((value): value is number => value != null, {
        message: messages.gameRequired,
      }),
    startMonth: z.string().refine(isMonth, { message: messages.startMonthRequired }),
    startYear: z.string().refine(isYear, { message: messages.startYearRequired }),
    status: z.enum(['Planning', 'Active', 'Completed']),
    tags: z.array(z.string().trim().max(100, messages.tagTooLong)),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildPlaythroughDefaults(currentYear: number): PlaythroughFormValues {
  return {
    name: '',
    gameId: null,
    startMonth: String(new Date().getMonth() + 1),
    startYear: String(currentYear),
    status: 'Planning',
    tags: [],
    visibility: 'Public',
  }
}

export function fromPlaythrough(playthrough: Playthrough): PlaythroughFormValues {
  return {
    name: playthrough.name,
    gameId: playthrough.gameId,
    startMonth: String(playthrough.startMonth),
    startYear: String(playthrough.startYear),
    status: playthrough.status,
    tags: [...playthrough.tags],
    visibility: playthrough.visibility,
  }
}

/**
 * Trims and de-duplicates tags case-insensitively, preserving first-seen order,
 * mirroring the backend normalization so the card list and filters stay stable.
 */
export function normalizeTags(tags: string[]): string[] {
  const seen = new Set<string>()
  const result: string[] = []
  for (const raw of tags) {
    const trimmed = raw.trim()
    if (trimmed === '') continue
    const key = trimmed.toLowerCase()
    if (seen.has(key)) continue
    seen.add(key)
    result.push(trimmed)
  }
  return result
}

export function toPlaythroughRequest(
  values: PlaythroughFormValues,
): PlaythroughRequest {
  return {
    name: values.name.trim(),
    gameId: values.gameId as number,
    startYear: Number(values.startYear),
    startMonth: Number(values.startMonth),
    status: values.status,
    tags: normalizeTags(values.tags),
    visibility: values.visibility,
  }
}
