import { z } from 'zod'

import type { HealthVisibility, Medicine, MedicineRequest } from '@/app/api/health'

export interface MedicineFormValues {
  name: string
  categoryId: string
  posology: string
  requiresPrescription: boolean
  inventoryItemId: number | null
  inventoryItemName: string
  notes: string
  visibility: HealthVisibility
}

export interface MedicineValidationMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  posologyTooLong: string
  notesTooLong: string
}

export function createMedicineFormSchema(messages: MedicineValidationMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().trim().min(1, messages.categoryRequired),
    posology: z.string().trim().max(2000, messages.posologyTooLong),
    requiresPrescription: z.boolean(),
    inventoryItemId: z.number().int().positive().nullable(),
    inventoryItemName: z.string(),
    notes: z.string().trim().max(2000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildMedicineDefaults(categoryId: string): MedicineFormValues {
  return {
    name: '',
    categoryId,
    posology: '',
    requiresPrescription: false,
    inventoryItemId: null,
    inventoryItemName: '',
    notes: '',
    visibility: 'Public',
  }
}

export function fromMedicine(medicine: Medicine): MedicineFormValues {
  return {
    name: medicine.name,
    categoryId: String(medicine.categoryId),
    posology: medicine.posology ?? '',
    requiresPrescription: medicine.requiresPrescription,
    inventoryItemId: medicine.inventoryItemId,
    inventoryItemName: medicine.inventoryItemName ?? '',
    notes: medicine.notes ?? '',
    visibility: medicine.visibility,
  }
}

export function toMedicineRequest(values: MedicineFormValues): MedicineRequest {
  const textOrNull = (value: string): string | null => {
    const text = value.trim()
    return text === '' ? null : text
  }

  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    posology: textOrNull(values.posology),
    requiresPrescription: values.requiresPrescription,
    inventoryItemId: values.inventoryItemId,
    notes: textOrNull(values.notes),
    visibility: values.visibility,
  }
}
