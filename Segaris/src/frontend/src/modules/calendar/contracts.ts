import { z } from 'zod'

import type {
  CalendarDailyNoteRequest,
  CalendarEntriesQuery,
  CalendarNotesQuery,
} from '@/app/api/calendar'

export const calendarKeys = {
  all: ['calendar'] as const,
  entries: () => [...calendarKeys.all, 'entries'] as const,
  entriesRange: (query: CalendarEntriesQuery) =>
    [...calendarKeys.entries(), query] as const,
  notes: () => [...calendarKeys.all, 'notes'] as const,
  notesRange: (query: CalendarNotesQuery) => [...calendarKeys.notes(), query] as const,
  note: (noteId: number) => [...calendarKeys.notes(), noteId] as const,
}

const civilDate = z.string().regex(/^\d{4}-\d{2}-\d{2}$/)

export const calendarDailyNoteRequestSchema = z.object({
  date: civilDate,
  title: z
    .string()
    .trim()
    .max(200)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable(),
  body: z.string().trim().min(1).max(4000),
  visibility: z.enum(['Public', 'Private']).default('Private'),
}) satisfies z.ZodType<CalendarDailyNoteRequest>
