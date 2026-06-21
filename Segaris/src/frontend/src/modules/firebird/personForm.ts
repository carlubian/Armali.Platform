import { z } from 'zod'

import type {
  CreatePersonRequest,
  FirebirdPersonStatus,
  FirebirdVisibility,
  Person,
} from '@/app/api/firebird'

export interface PersonFormValues {
  name: string
  categoryId: string
  status: FirebirdPersonStatus
  hasBirthday: boolean
  birthdayMonth: number
  birthdayDay: number
  notes: string
  visibility: FirebirdVisibility
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  notesTooLong: string
}

/** Days in a year-less month; February allows 29 because birthdays store no year. */
export function daysInMonth(month: number): number {
  if (month === 2) return 29
  return [4, 6, 9, 11].includes(month) ? 30 : 31
}

export function createPersonSchema(messages: SchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().min(1, messages.categoryRequired),
    status: z.enum(['Unknown', 'Active', 'Unavailable', 'Blocked']),
    hasBirthday: z.boolean(),
    birthdayMonth: z.number().int().min(1).max(12),
    birthdayDay: z.number().int().min(1).max(31),
    notes: z.string().max(2000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(categoryId: string): PersonFormValues {
  return {
    name: '',
    categoryId,
    status: 'Unknown',
    hasBirthday: false,
    birthdayMonth: 1,
    birthdayDay: 1,
    notes: '',
    visibility: 'Public',
  }
}

export function fromPerson(person: Person): PersonFormValues {
  const hasBirthday = person.birthdayMonth != null && person.birthdayDay != null
  return {
    name: person.name,
    categoryId: String(person.categoryId),
    status: person.status,
    hasBirthday,
    birthdayMonth: person.birthdayMonth ?? 1,
    birthdayDay: person.birthdayDay ?? 1,
    notes: person.notes ?? '',
    visibility: person.visibility,
  }
}

export function toRequest(values: PersonFormValues): CreatePersonRequest {
  const month = values.hasBirthday ? values.birthdayMonth : null
  const day = values.hasBirthday
    ? Math.min(values.birthdayDay, daysInMonth(values.birthdayMonth))
    : null
  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    status: values.status,
    birthdayMonth: month,
    birthdayDay: day,
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
    visibility: values.visibility,
  }
}
