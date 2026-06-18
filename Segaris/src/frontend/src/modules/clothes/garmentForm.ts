import { z } from 'zod'

import type {
  ClothesDryCleaningCare,
  ClothesDryingCare,
  ClothesGarment,
  ClothesGarmentStatus,
  ClothesIroningCare,
  ClothesVisibility,
  ClothesWashingCare,
  CreateClothesGarmentRequest,
} from '@/app/api/clothes'

export interface GarmentFormValues {
  name: string
  categoryId: string
  status: ClothesGarmentStatus
  size: string
  colorIds: number[]
  washingCare: ClothesWashingCare | ''
  dryingCare: ClothesDryingCare | ''
  ironingCare: ClothesIroningCare | ''
  dryCleaningCare: ClothesDryCleaningCare | ''
  notes: string
  visibility: ClothesVisibility
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  sizeTooLong: string
  notesTooLong: string
}

export const washingCareValues: ClothesWashingCare[] = [
  'Any',
  'Wash30',
  'Wash30Delicate',
  'Wash40',
  'Wash40Delicate',
  'Wash50',
  'Wash50Delicate',
  'Wash60',
  'Wash60Delicate',
  'HandWash',
  'DoNotWash',
]

export const dryingCareValues: ClothesDryingCare[] = ['Any', 'Delicate', 'VeryDelicate']

export const ironingCareValues: ClothesIroningCare[] = [
  'Any',
  'Low',
  'Medium',
  'DoNotIron',
]

export const dryCleaningCareValues: ClothesDryCleaningCare[] = ['Any', 'DoNotDryClean']

export function createGarmentSchema(messages: SchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().min(1, messages.categoryRequired),
    status: z.enum(['Active', 'Unavailable', 'Deprecated']),
    size: z.string().max(50, messages.sizeTooLong),
    colorIds: z.array(z.number().int().positive()),
    washingCare: z.enum([
      '',
      'Any',
      'Wash30',
      'Wash30Delicate',
      'Wash40',
      'Wash40Delicate',
      'Wash50',
      'Wash50Delicate',
      'Wash60',
      'Wash60Delicate',
      'HandWash',
      'DoNotWash',
    ]),
    dryingCare: z.enum(['', 'Any', 'Delicate', 'VeryDelicate']),
    ironingCare: z.enum(['', 'Any', 'Low', 'Medium', 'DoNotIron']),
    dryCleaningCare: z.enum(['', 'Any', 'DoNotDryClean']),
    notes: z.string().max(4000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(categoryId: string): GarmentFormValues {
  return {
    name: '',
    categoryId,
    status: 'Active',
    size: '',
    colorIds: [],
    washingCare: '',
    dryingCare: '',
    ironingCare: '',
    dryCleaningCare: '',
    notes: '',
    visibility: 'Public',
  }
}

export function fromGarment(garment: ClothesGarment): GarmentFormValues {
  return {
    name: garment.name,
    categoryId: String(garment.categoryId),
    status: garment.status,
    size: garment.size ?? '',
    colorIds: garment.colors.map((color) => color.id),
    washingCare: garment.washingCare ?? '',
    dryingCare: garment.dryingCare ?? '',
    ironingCare: garment.ironingCare ?? '',
    dryCleaningCare: garment.dryCleaningCare ?? '',
    notes: garment.notes ?? '',
    visibility: garment.visibility,
  }
}

const emptyToNull = <T extends string>(value: T | ''): T | null =>
  value === '' ? null : value

export function toRequest(values: GarmentFormValues): CreateClothesGarmentRequest {
  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    status: values.status,
    size: values.size.trim() === '' ? null : values.size.trim(),
    colorIds: [...new Set(values.colorIds)],
    washingCare: emptyToNull(values.washingCare),
    dryingCare: emptyToNull(values.dryingCare),
    ironingCare: emptyToNull(values.ironingCare),
    dryCleaningCare: emptyToNull(values.dryCleaningCare),
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
    visibility: values.visibility,
  }
}
