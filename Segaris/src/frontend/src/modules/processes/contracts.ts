import { z } from 'zod'

import type {
  CreateProcessRequest,
  ProcessListQuery,
  StepListItemRequest,
  UpdateStepListRequest,
} from '@/app/api/processes'
import { processVisibilities } from '@/app/api/processes'

export const processesKeys = {
  all: ['processes'] as const,
  categories: () => [...processesKeys.all, 'categories'] as const,
  list: (query: ProcessListQuery) => [...processesKeys.all, 'list', query] as const,
  process: (processId: number) => [...processesKeys.all, processId] as const,
  steps: (processId: number) => [...processesKeys.process(processId), 'steps'] as const,
  attachments: (processId: number) =>
    [...processesKeys.process(processId), 'attachments'] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

const optionalDate = z
  .string()
  .trim()
  .refine((value) => value.length === 0 || /^\d{4}-\d{2}-\d{2}$/.test(value), {
    message: 'Date must be an ISO civil date (yyyy-MM-dd).',
  })
  .transform((value) => (value.length === 0 ? null : value))
  .nullable()

export const processRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  dueDate: optionalDate,
  notes: optionalText(4000),
  visibility: z.enum(processVisibilities),
}) satisfies z.ZodType<CreateProcessRequest>

export const stepListItemSchema = z.object({
  id: z.number().int().positive().nullable(),
  description: z.string().trim().min(1).max(500),
  dueDate: optionalDate,
  notes: optionalText(1000),
  isOptional: z.boolean(),
}) satisfies z.ZodType<StepListItemRequest>

export const updateStepListSchema = z.object({
  steps: z.array(stepListItemSchema),
}) satisfies z.ZodType<UpdateStepListRequest>
