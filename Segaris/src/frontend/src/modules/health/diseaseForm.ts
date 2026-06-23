import { z } from 'zod'

import type { Disease, DiseaseRequest, HealthVisibility } from '@/app/api/health'

export interface DiseaseFormValues {
  name: string
  categoryId: string
  symptoms: string
  averageDurationDays: string
  notes: string
  visibility: HealthVisibility
}

export interface DiseaseValidationMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  durationRange: string
  symptomsTooLong: string
  notesTooLong: string
}

function optionalDuration(message: string) {
  return z
    .string()
    .trim()
    .refine(
      (value) =>
        value === '' ||
        (/^\d+$/.test(value) && Number(value) >= 1 && Number(value) <= 100_000),
      message,
    )
}

export function createDiseaseFormSchema(messages: DiseaseValidationMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().trim().min(1, messages.categoryRequired),
    symptoms: z.string().trim().max(2000, messages.symptomsTooLong),
    averageDurationDays: optionalDuration(messages.durationRange),
    notes: z.string().trim().max(2000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(categoryId: string): DiseaseFormValues {
  return {
    name: '',
    categoryId,
    symptoms: '',
    averageDurationDays: '',
    notes: '',
    visibility: 'Public',
  }
}

export function fromDisease(disease: Disease): DiseaseFormValues {
  return {
    name: disease.name,
    categoryId: String(disease.categoryId),
    symptoms: disease.symptoms ?? '',
    averageDurationDays:
      disease.averageDurationDays == null ? '' : String(disease.averageDurationDays),
    notes: disease.notes ?? '',
    visibility: disease.visibility,
  }
}

export function toRequest(values: DiseaseFormValues): DiseaseRequest {
  const textOrNull = (value: string): string | null => {
    const text = value.trim()
    return text === '' ? null : text
  }
  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    symptoms: textOrNull(values.symptoms),
    averageDurationDays:
      values.averageDurationDays.trim() === ''
        ? null
        : Number(values.averageDurationDays),
    notes: textOrNull(values.notes),
    visibility: values.visibility,
  }
}
