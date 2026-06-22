import { z } from 'zod'

import type {
  CreateDestinationRequest,
  Destination,
  DestinationVisibility,
} from '@/app/api/destinations'

export interface DestinationFormValues {
  name: string
  categoryId: string
  country: string
  entryRequirements: string
  isSchengenArea: boolean
  notes: string
  visibility: DestinationVisibility
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  countryTooLong: string
  entryRequirementsTooLong: string
  notesTooLong: string
}

export function createDestinationSchema(messages: SchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().min(1, messages.categoryRequired),
    country: z.string().trim().max(200, messages.countryTooLong),
    entryRequirements: z.string().max(2000, messages.entryRequirementsTooLong),
    isSchengenArea: z.boolean(),
    notes: z.string().max(2000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(categoryId: string): DestinationFormValues {
  return {
    name: '',
    categoryId,
    country: '',
    entryRequirements: '',
    isSchengenArea: false,
    notes: '',
    visibility: 'Public',
  }
}

export function fromDestination(destination: Destination): DestinationFormValues {
  return {
    name: destination.name,
    categoryId: String(destination.categoryId),
    country: destination.country ?? '',
    entryRequirements: destination.entryRequirements ?? '',
    isSchengenArea: destination.isSchengenArea,
    notes: destination.notes ?? '',
    visibility: destination.visibility,
  }
}

function nullableText(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

export function toRequest(values: DestinationFormValues): CreateDestinationRequest {
  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    country: nullableText(values.country),
    entryRequirements: nullableText(values.entryRequirements),
    isSchengenArea: values.isSchengenArea,
    notes: nullableText(values.notes),
    visibility: values.visibility,
  }
}
