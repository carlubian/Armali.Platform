import { z } from 'zod'

import type {
  CreateInventoryItemRequest,
  InventoryItem,
  InventoryItemStatus,
  InventoryVisibility,
} from '@/app/api/inventory'

export interface ItemFormValues {
  name: string
  status: InventoryItemStatus
  categoryId: string
  locationId: string
  currentStock: string
  minimumStock: string
  supplierIds: number[]
  notes: string
  visibility: InventoryVisibility
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  locationRequired: string
  stockInvalid: string
  suppliersRequired: string
  notesTooLong: string
}

const twoDecimals = /^\d+(\.\d{1,2})?$/

/** Parses a non-negative amount with at most two decimals, or `null`. */
export function parseAmount(value: string): number | null {
  const trimmed = value.trim()
  if (!twoDecimals.test(trimmed)) return null
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : null
}

export function createItemSchema(messages: SchemaMessages) {
  const stock = z
    .string()
    .refine((value) => parseAmount(value) != null, messages.stockInvalid)
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    status: z.enum(['Candidate', 'Active', 'Deprecated']),
    categoryId: z.string().min(1, messages.categoryRequired),
    locationId: z.string().min(1, messages.locationRequired),
    currentStock: stock,
    minimumStock: stock,
    supplierIds: z
      .array(z.number().int().positive())
      .min(1, messages.suppliersRequired),
    notes: z.string().max(4000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

interface DefaultsParams {
  categoryId: string
  locationId: string
}

export function buildDefaults({
  categoryId,
  locationId,
}: DefaultsParams): ItemFormValues {
  return {
    name: '',
    status: 'Candidate',
    categoryId,
    locationId,
    currentStock: '0',
    minimumStock: '0',
    supplierIds: [],
    notes: '',
    visibility: 'Public',
  }
}

export function fromItem(item: InventoryItem): ItemFormValues {
  return {
    name: item.name,
    status: item.status,
    categoryId: String(item.categoryId),
    locationId: String(item.locationId),
    currentStock: String(item.currentStock),
    minimumStock: String(item.minimumStock),
    supplierIds: item.suppliers.map((supplier) => supplier.supplierId),
    notes: item.notes ?? '',
    visibility: item.visibility,
  }
}

export function toRequest(values: ItemFormValues): CreateInventoryItemRequest {
  return {
    name: values.name.trim(),
    status: values.status,
    categoryId: Number(values.categoryId),
    locationId: Number(values.locationId),
    currentStock: parseAmount(values.currentStock) ?? 0,
    minimumStock: parseAmount(values.minimumStock) ?? 0,
    supplierIds: [...values.supplierIds],
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
    visibility: values.visibility,
  }
}
