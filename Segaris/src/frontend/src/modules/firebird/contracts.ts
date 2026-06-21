import { z } from 'zod'

import {
  firebirdPersonStatuses,
  firebirdVisibilities,
  type CreatePersonRequest,
  type InteractionRequest,
  type PersonListQuery,
  type UsernameRequest,
} from '@/app/api/firebird'

export const firebirdKeys = {
  all: ['firebird'] as const,
  categories: () => [...firebirdKeys.all, 'categories'] as const,
  platforms: () => [...firebirdKeys.all, 'platforms'] as const,
  people: () => [...firebirdKeys.all, 'people'] as const,
  personList: (query: PersonListQuery) =>
    [...firebirdKeys.people(), 'list', query] as const,
  person: (personId: number) => [...firebirdKeys.people(), personId] as const,
  avatar: (personId: number) => [...firebirdKeys.person(personId), 'avatar'] as const,
  usernames: (personId: number) =>
    [...firebirdKeys.person(personId), 'usernames'] as const,
  interactions: (personId: number) =>
    [...firebirdKeys.person(personId), 'interactions'] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

const birthdayDayForMonth = (month: number): number =>
  month === 2 ? 29 : [4, 6, 9, 11].includes(month) ? 30 : 31

export const firebirdPersonRequestSchema = z
  .object({
    name: z.string().trim().min(1).max(200),
    categoryId: z.number().int().positive(),
    status: z.enum(firebirdPersonStatuses),
    birthdayMonth: z.number().int().min(1).max(12).nullable(),
    birthdayDay: z.number().int().min(1).max(31).nullable(),
    notes: optionalText(2000),
    visibility: z.enum(firebirdVisibilities),
  })
  .refine(
    (value) =>
      (value.birthdayMonth == null && value.birthdayDay == null) ||
      (value.birthdayMonth != null &&
        value.birthdayDay != null &&
        value.birthdayDay <= birthdayDayForMonth(value.birthdayMonth)),
    { path: ['birthdayDay'] },
  ) satisfies z.ZodType<CreatePersonRequest>

export const firebirdUsernameRequestSchema = z.object({
  platformId: z.number().int().positive(),
  handle: z.string().trim().min(1).max(200),
  notes: optionalText(1000),
}) satisfies z.ZodType<UsernameRequest>

export const firebirdInteractionRequestSchema = z.object({
  date: z.iso.date(),
  description: z.string().trim().min(1).max(2000),
}) satisfies z.ZodType<InteractionRequest>

export const firebirdCatalogRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
})
