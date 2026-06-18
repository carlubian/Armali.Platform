import { z } from 'zod'

import type {
  ClothesGarmentListQuery,
  CreateClothesGarmentRequest,
} from '@/app/api/clothes'

export const clothesKeys = {
  all: ['clothes'] as const,
  categories: () => [...clothesKeys.all, 'categories'] as const,
  colors: () => [...clothesKeys.all, 'colors'] as const,
  garments: () => [...clothesKeys.all, 'garments'] as const,
  garmentList: (query: ClothesGarmentListQuery) =>
    [...clothesKeys.garments(), 'list', query] as const,
  garment: (garmentId: number) => [...clothesKeys.garments(), garmentId] as const,
  garmentAttachments: (garmentId: number) =>
    [...clothesKeys.garment(garmentId), 'attachments'] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

const care = {
  washing: z
    .enum([
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
    ])
    .nullable(),
  drying: z.enum(['Any', 'Delicate', 'VeryDelicate']).nullable(),
  ironing: z.enum(['Any', 'Low', 'Medium', 'DoNotIron']).nullable(),
  dryCleaning: z.enum(['Any', 'DoNotDryClean']).nullable(),
}

export const clothesGarmentRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  status: z.enum(['Active', 'Unavailable', 'Deprecated']),
  size: optionalText(50),
  colorIds: z.array(z.number().int().positive()),
  washingCare: care.washing,
  dryingCare: care.drying,
  ironingCare: care.ironing,
  dryCleaningCare: care.dryCleaning,
  notes: optionalText(4000),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<CreateClothesGarmentRequest>

export const clothingColorRequestSchema = z.object({
  name: z.string().trim().min(1).max(100),
  colorValue: z.string().regex(/^#[0-9A-Fa-f]{6}$/),
})
