import { z } from 'zod'

import type {
  CreateMaintenanceTaskRequest,
  MaintenanceTaskListQuery,
} from '@/app/api/maintenance'

export const maintenanceKeys = {
  all: ['maintenance'] as const,
  types: () => [...maintenanceKeys.all, 'types'] as const,
  tasks: () => [...maintenanceKeys.all, 'tasks'] as const,
  taskList: (query: MaintenanceTaskListQuery) =>
    [...maintenanceKeys.tasks(), 'list', query] as const,
  task: (taskId: number) => [...maintenanceKeys.tasks(), taskId] as const,
  taskAttachments: (taskId: number) =>
    [...maintenanceKeys.task(taskId), 'attachments'] as const,
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

const optionalReference = z.number().int().positive().nullable()

export const maintenanceTaskRequestSchema = z.object({
  title: z.string().trim().min(1).max(200),
  maintenanceTypeId: z.number().int().positive(),
  status: z.enum(['Pending', 'InProgress', 'Completed', 'Cancelled']),
  priority: z.enum(['Low', 'Medium', 'High']),
  dueDate: optionalDate,
  notes: optionalText(4000),
  assetId: optionalReference,
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<CreateMaintenanceTaskRequest>
