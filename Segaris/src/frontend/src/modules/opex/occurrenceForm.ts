import { z } from 'zod'

import type {
  CreateOpexOccurrenceRequest,
  OpexOccurrence,
} from '@/app/api/opex'

export interface OccurrenceFormValues {
  effectiveDate: string
  actualAmount: string
  description: string
  notes: string
}

interface SchemaMessages {
  dateRequired: string
  amountInvalid: string
  descriptionTooLong: string
  notesTooLong: string
}

const twoDecimals = /^\d+(\.\d{1,2})?$/

export function parseOccurrenceAmount(value: string): number | null {
  const trimmed = value.trim()
  if (!twoDecimals.test(trimmed)) return null
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : null
}

export function createOccurrenceSchema(messages: SchemaMessages) {
  return z.object({
    effectiveDate: z.string().min(1, messages.dateRequired),
    actualAmount: z.string().refine(
      (value) => parseOccurrenceAmount(value) != null,
      messages.amountInvalid,
    ),
    description: z.string().max(300, messages.descriptionTooLong),
    notes: z.string().max(4000, messages.notesTooLong),
  })
}

export function buildOccurrenceDefaults(): OccurrenceFormValues {
  return {
    effectiveDate: new Date().toISOString().slice(0, 10),
    actualAmount: '',
    description: '',
    notes: '',
  }
}

export function fromOccurrence(occurrence: OpexOccurrence): OccurrenceFormValues {
  return {
    effectiveDate: occurrence.effectiveDate.slice(0, 10),
    actualAmount: String(occurrence.actualAmount),
    description: occurrence.description ?? '',
    notes: occurrence.notes ?? '',
  }
}

export function toOccurrenceRequest(
  values: OccurrenceFormValues,
): CreateOpexOccurrenceRequest {
  return {
    effectiveDate: values.effectiveDate,
    actualAmount: parseOccurrenceAmount(values.actualAmount) ?? 0,
    description: values.description.trim() === '' ? null : values.description.trim(),
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
  }
}
