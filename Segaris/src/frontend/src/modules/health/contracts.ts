import { z } from 'zod'

import type {
  DiseaseListQuery,
  DiseaseRequest,
  MedicineListQuery,
  MedicineRequest,
} from '@/app/api/health'

export const healthKeys = {
  all: ['health'] as const,
  diseaseCategories: () => [...healthKeys.all, 'disease-categories'] as const,
  medicineCategories: () => [...healthKeys.all, 'medicine-categories'] as const,
  diseases: () => [...healthKeys.all, 'diseases'] as const,
  diseaseList: (query: DiseaseListQuery) =>
    [...healthKeys.diseases(), 'list', query] as const,
  disease: (diseaseId: number) => [...healthKeys.diseases(), diseaseId] as const,
  diseaseMedicines: (diseaseId: number) =>
    [...healthKeys.disease(diseaseId), 'medicines'] as const,
  medicines: () => [...healthKeys.all, 'medicines'] as const,
  medicineList: (query: MedicineListQuery) =>
    [...healthKeys.medicines(), 'list', query] as const,
  medicine: (medicineId: number) => [...healthKeys.medicines(), medicineId] as const,
  medicineDiseases: (medicineId: number) =>
    [...healthKeys.medicine(medicineId), 'diseases'] as const,
  medicineAttachments: (medicineId: number) =>
    [...healthKeys.medicine(medicineId), 'attachments'] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

export const diseaseRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  symptoms: optionalText(2000),
  averageDurationDays: z.number().int().min(1).max(100_000).nullable(),
  notes: optionalText(2000),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<DiseaseRequest>

export const medicineRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  posology: optionalText(2000),
  requiresPrescription: z.boolean().default(false),
  inventoryItemId: z.number().int().positive().nullable(),
  notes: optionalText(2000),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<MedicineRequest>

export const healthCategoryRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
})
