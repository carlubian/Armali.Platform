import { z } from 'zod'

import type {
  CreateProcessRequest,
  Process,
  ProcessVisibility,
} from '@/app/api/processes'

export interface ProcessFormValues {
  name: string
  categoryId: string
  dueDate: string
  notes: string
  visibility: ProcessVisibility
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  dateInvalid: string
  notesTooLong: string
}

const dateOnly = /^\d{4}-\d{2}-\d{2}$/

export function createProcessSchema(messages: SchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().min(1, messages.categoryRequired),
    dueDate: z
      .string()
      .trim()
      .refine((value) => value.length === 0 || dateOnly.test(value), {
        message: messages.dateInvalid,
      }),
    notes: z.string().max(4000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(categoryId: string): ProcessFormValues {
  return {
    name: '',
    categoryId,
    dueDate: '',
    notes: '',
    visibility: 'Public',
  }
}

export function fromProcess(process: Process): ProcessFormValues {
  return {
    name: process.name,
    categoryId: String(process.categoryId),
    dueDate: process.dueDate ?? '',
    notes: process.notes ?? '',
    visibility: process.visibility,
  }
}

function nullableText(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

export function toRequest(values: ProcessFormValues): CreateProcessRequest {
  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    dueDate: nullableText(values.dueDate),
    notes: nullableText(values.notes),
    visibility: values.visibility,
  }
}
