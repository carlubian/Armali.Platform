import { z } from 'zod'

import type {
  CreateMaintenanceTaskRequest,
  MaintenancePriority,
  MaintenanceStatus,
  MaintenanceTask,
  MaintenanceVisibility,
} from '@/app/api/maintenance'

export interface MaintenanceFormValues {
  title: string
  maintenanceTypeId: string
  status: MaintenanceStatus
  priority: MaintenancePriority
  dueDate: string
  notes: string
  assetId: string
  visibility: MaintenanceVisibility
}

interface SchemaMessages {
  titleRequired: string
  titleTooLong: string
  typeRequired: string
  dateInvalid: string
  notesTooLong: string
}

const dateOnly = /^\d{4}-\d{2}-\d{2}$/

export function createMaintenanceSchema(messages: SchemaMessages) {
  return z.object({
    title: z
      .string()
      .trim()
      .min(1, messages.titleRequired)
      .max(200, messages.titleTooLong),
    maintenanceTypeId: z.string().min(1, messages.typeRequired),
    status: z.enum(['Pending', 'InProgress', 'Completed', 'Cancelled']),
    priority: z.enum(['Low', 'Medium', 'High']),
    dueDate: z
      .string()
      .trim()
      .refine((value) => value.length === 0 || dateOnly.test(value), {
        message: messages.dateInvalid,
      }),
    notes: z.string().max(4000, messages.notesTooLong),
    assetId: z.string(),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(typeId: string): MaintenanceFormValues {
  return {
    title: '',
    maintenanceTypeId: typeId,
    status: 'Pending',
    priority: 'Medium',
    dueDate: '',
    notes: '',
    assetId: '',
    visibility: 'Public',
  }
}

export function fromTask(task: MaintenanceTask): MaintenanceFormValues {
  return {
    title: task.title,
    maintenanceTypeId: String(task.maintenanceTypeId),
    status: task.status,
    priority: task.priority,
    dueDate: task.dueDate ?? '',
    notes: task.notes ?? '',
    assetId: task.assetId == null ? '' : String(task.assetId),
    visibility: task.visibility,
  }
}

function nullableText(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

function nullableDate(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

function nullableReference(value: string): number | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : Number(trimmed)
}

export function toRequest(values: MaintenanceFormValues): CreateMaintenanceTaskRequest {
  return {
    title: values.title.trim(),
    maintenanceTypeId: Number(values.maintenanceTypeId),
    status: values.status,
    priority: values.priority,
    dueDate: nullableDate(values.dueDate),
    notes: nullableText(values.notes),
    assetId: nullableReference(values.assetId),
    visibility: values.visibility,
  }
}
