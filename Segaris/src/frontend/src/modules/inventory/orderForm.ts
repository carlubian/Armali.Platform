import { z } from 'zod'

import type {
  CreateInventoryOrderRequest,
  InventoryOrder,
  InventoryOrderStatus,
  InventoryVisibility,
} from '@/app/api/inventory'

import { parseAmount } from './itemForm'

export interface OrderLineFormValues {
  itemId: string
  quantity: string
  lineTotal: string
}

export interface OrderFormValues {
  supplierId: string
  status: InventoryOrderStatus
  currencyId: string
  orderDate: string
  expectedReceiptDate: string
  notes: string
  visibility: InventoryVisibility
  lines: OrderLineFormValues[]
}

interface SchemaMessages {
  supplierRequired: string
  currencyRequired: string
  notesTooLong: string
  linesRequired: string
  itemRequired: string
  quantityInvalid: string
  lineTotalInvalid: string
}

/** Parses a strictly positive amount with at most two decimals, or `null`. */
export function parsePositiveAmount(value: string): number | null {
  const parsed = parseAmount(value)
  return parsed != null && parsed > 0 ? parsed : null
}

export function createOrderSchema(messages: SchemaMessages) {
  const line = z.object({
    itemId: z.string().min(1, messages.itemRequired),
    quantity: z
      .string()
      .refine((value) => parsePositiveAmount(value) != null, messages.quantityInvalid),
    lineTotal: z
      .string()
      .refine((value) => parseAmount(value) != null, messages.lineTotalInvalid),
  })
  return z.object({
    supplierId: z.string().min(1, messages.supplierRequired),
    status: z.enum(['Planning', 'Active', 'Received', 'Cancelled']),
    currencyId: z.string().min(1, messages.currencyRequired),
    orderDate: z.string(),
    expectedReceiptDate: z.string(),
    notes: z.string().max(4000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
    lines: z.array(line).min(1, messages.linesRequired).max(100),
  })
}

/** The household's local date (Europe/Madrid) as a `yyyy-mm-dd` string. */
export function householdToday(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Madrid',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date())
}

function addDays(isoDate: string, days: number): string {
  const date = new Date(`${isoDate}T00:00:00Z`)
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}

export function blankLine(): OrderLineFormValues {
  return { itemId: '', quantity: '1', lineTotal: '0' }
}

interface DefaultsParams {
  supplierId: string
  currencyId: string
}

export function buildDefaults({
  supplierId,
  currencyId,
}: DefaultsParams): OrderFormValues {
  const today = householdToday()
  return {
    supplierId,
    status: 'Planning',
    currencyId,
    orderDate: today,
    expectedReceiptDate: addDays(today, 7),
    notes: '',
    visibility: 'Public',
    lines: [blankLine()],
  }
}

export function fromOrder(order: InventoryOrder): OrderFormValues {
  return {
    supplierId: String(order.supplierId),
    status: order.status,
    currencyId: String(order.currencyId),
    orderDate: order.orderDate ?? '',
    expectedReceiptDate: order.expectedReceiptDate ?? '',
    notes: order.notes ?? '',
    visibility: order.visibility,
    lines:
      order.lines.length > 0
        ? order.lines.map((line) => ({
            itemId: String(line.itemId),
            quantity: String(line.quantity),
            lineTotal: String(line.lineTotal),
          }))
        : [blankLine()],
  }
}

export function toRequest(values: OrderFormValues): CreateInventoryOrderRequest {
  return {
    supplierId: Number(values.supplierId),
    status: values.status,
    currencyId: Number(values.currencyId),
    orderDate: values.orderDate === '' ? null : values.orderDate,
    expectedReceiptDate:
      values.expectedReceiptDate === '' ? null : values.expectedReceiptDate,
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
    visibility: values.visibility,
    lines: values.lines.map((line) => ({
      itemId: Number(line.itemId),
      quantity: parsePositiveAmount(line.quantity) ?? 0,
      lineTotal: parseAmount(line.lineTotal) ?? 0,
    })),
  }
}
