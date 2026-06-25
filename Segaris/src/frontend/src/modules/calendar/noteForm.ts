import { z } from 'zod'

import type { CalendarDailyNote, CalendarVisibility } from '@/app/api/calendar'

/**
 * React Hook Form values for the daily-note editor. `title` and `body` start as
 * empty strings (RHF text inputs are never `null`); the schema trims them and
 * turns a blank title into `null` on the way to a {@link CalendarDailyNoteRequest}.
 */
export interface CalendarNoteFormValues {
  date: string
  title: string
  body: string
  visibility: CalendarVisibility
}

export interface CalendarNoteSchemaMessages {
  dateRequired: string
  titleTooLong: string
  bodyRequired: string
  bodyTooLong: string
}

const datePattern = /^\d{4}-\d{2}-\d{2}$/

/** Mirrors the backend bounds in `CalendarDefaults`. */
export const calendarNoteTitleMaxLength = 200
export const calendarNoteBodyMaxLength = 4000

/**
 * Builds the note schema with localized messages. `z.input` matches
 * {@link CalendarNoteFormValues}; `z.output` is a {@link CalendarDailyNoteRequest}
 * (title trimmed to `null` when blank, body trimmed and required).
 */
export function createCalendarNoteSchema(messages: CalendarNoteSchemaMessages) {
  return z.object({
    date: z.string().regex(datePattern, messages.dateRequired),
    title: z
      .string()
      .trim()
      .max(calendarNoteTitleMaxLength, messages.titleTooLong)
      .transform((value) => (value.length === 0 ? null : value)),
    body: z
      .string()
      .trim()
      .min(1, messages.bodyRequired)
      .max(calendarNoteBodyMaxLength, messages.bodyTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export type CalendarNoteSchema = ReturnType<typeof createCalendarNoteSchema>

/** A blank note pinned to `date`, defaulting to `Private` visibility. */
export function buildNoteDefaults(date: string): CalendarNoteFormValues {
  return {
    date,
    title: '',
    body: '',
    visibility: 'Private',
  }
}

export function fromNote(note: CalendarDailyNote): CalendarNoteFormValues {
  return {
    date: note.date,
    title: note.title ?? '',
    body: note.body,
    visibility: note.visibility,
  }
}
