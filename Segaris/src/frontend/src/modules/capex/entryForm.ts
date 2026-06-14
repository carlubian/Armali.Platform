import { z } from 'zod'

import type {
  CapexEntry,
  CapexEntryStatus,
  CapexMovementType,
  CapexVisibility,
  CreateCapexEntryRequest,
} from '@/app/api/capex'

const householdTimeZone = 'Europe/Madrid'

/** Today's civil date in the household time zone, as `yyyy-mm-dd`. */
export function todayInMadrid(reference: Date = new Date()): string {
  // `en-CA` formats as ISO `yyyy-mm-dd`, which is exactly the value a
  // `type="date"` input and the backend `DueDate` expect.
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: householdTimeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(reference)
}

/**
 * A monetary or quantity value as the user types it. Numeric fields stay as
 * strings in the form so empty inputs and partial decimals never collapse to
 * `NaN`; they are parsed and validated on submit and for the live total.
 */
export type AmountString = string

/** One editable item line. Amounts are strings; see {@link AmountString}. */
export interface ItemFormValues {
  description: string
  quantity: AmountString
  unitAmount: AmountString
}

/** The complete editor form shape. Catalog references are select-backed strings. */
export interface EntryFormValues {
  title: string
  movementType: CapexMovementType
  status: CapexEntryStatus
  dueDate: string
  categoryId: string
  supplierId: string
  costCenterId: string
  currencyId: string
  notes: string
  visibility: CapexVisibility
  items: ItemFormValues[]
}

const twoDecimals = /^\d+(\.\d{1,2})?$/

/** Parses a user amount string, returning `null` when it is not a valid number. */
export function parseAmount(value: string): number | null {
  const trimmed = value.trim()
  if (!twoDecimals.test(trimmed)) return null
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? parsed : null
}

/** Rounds a line total away from zero to two places, mirroring the backend. */
export function computeLineAmount(quantity: number, unitAmount: number): number {
  const cents = Math.round((quantity * unitAmount + Number.EPSILON) * 100)
  return cents / 100
}

/** Sums rounded line amounts for the live preview; the server stays authoritative. */
export function computeTotal(items: ItemFormValues[]): number {
  return items.reduce((total, item) => {
    const quantity = parseAmount(item.quantity)
    const unitAmount = parseAmount(item.unitAmount)
    if (quantity == null || unitAmount == null) return total
    return total + computeLineAmount(quantity, unitAmount)
  }, 0)
}

interface SchemaMessages {
  titleRequired: string
  titleTooLong: string
  dueDateRequired: string
  categoryRequired: string
  currencyRequired: string
  notesTooLong: string
  descriptionRequired: string
  descriptionTooLong: string
  quantityInvalid: string
  unitAmountInvalid: string
  itemsBounds: string
}

/**
 * Builds the editor validation schema with localized messages. The rules mirror
 * the backend domain validation so the dialog rejects the same inputs the API
 * would (`title` ≤ 200, `notes` ≤ 4000, 1–100 items, description ≤ 300, positive
 * quantity, nonnegative unit amount, both with at most two decimals).
 */
export function createEntrySchema(messages: SchemaMessages) {
  const itemSchema = z.object({
    description: z
      .string()
      .trim()
      .min(1, messages.descriptionRequired)
      .max(300, messages.descriptionTooLong),
    quantity: z.string().refine((value) => {
      const parsed = parseAmount(value)
      return parsed != null && parsed > 0
    }, messages.quantityInvalid),
    unitAmount: z.string().refine((value) => {
      const parsed = parseAmount(value)
      return parsed != null && parsed >= 0
    }, messages.unitAmountInvalid),
  })

  return z.object({
    title: z
      .string()
      .trim()
      .min(1, messages.titleRequired)
      .max(200, messages.titleTooLong),
    movementType: z.enum(['Income', 'Expense']),
    status: z.enum(['Planning', 'Completed', 'Canceled']),
    dueDate: z.string().min(1, messages.dueDateRequired),
    categoryId: z.string().min(1, messages.categoryRequired),
    supplierId: z.string(),
    costCenterId: z.string(),
    currencyId: z.string().min(1, messages.currencyRequired),
    notes: z.string().max(4000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
    items: z
      .array(itemSchema)
      .min(1, messages.itemsBounds)
      .max(100, messages.itemsBounds),
  })
}

/** A fresh item line: quantity `1`, unit amount `0`, empty description. */
export function blankItem(): ItemFormValues {
  return { description: '', quantity: '1', unitAmount: '0' }
}

interface DefaultsParams {
  categoryId: string
  currencyId: string
  dueDate?: string
}

/** Creation defaults from the requirements; catalog ids are resolved by the caller. */
export function buildDefaults({
  categoryId,
  currencyId,
  dueDate,
}: DefaultsParams): EntryFormValues {
  return {
    title: '',
    movementType: 'Expense',
    status: 'Planning',
    dueDate: dueDate ?? todayInMadrid(),
    categoryId,
    supplierId: '',
    costCenterId: '',
    currencyId,
    notes: '',
    visibility: 'Public',
    items: [blankItem()],
  }
}

/** Maps a loaded entry into editable form values. */
export function fromEntry(entry: CapexEntry): EntryFormValues {
  return {
    title: entry.title,
    movementType: entry.movementType,
    status: entry.status,
    dueDate: entry.dueDate.slice(0, 10),
    categoryId: String(entry.categoryId),
    supplierId: entry.supplierId == null ? '' : String(entry.supplierId),
    costCenterId: entry.costCenterId == null ? '' : String(entry.costCenterId),
    currencyId: String(entry.currencyId),
    notes: entry.notes ?? '',
    visibility: entry.visibility,
    items: entry.items.map((item) => ({
      description: item.description,
      quantity: String(item.quantity),
      unitAmount: String(item.unitAmount),
    })),
  }
}

/** Builds the create/update request body from validated form values. */
export function toRequest(values: EntryFormValues): CreateCapexEntryRequest {
  return {
    title: values.title.trim(),
    movementType: values.movementType,
    status: values.status,
    dueDate: values.dueDate,
    categoryId: Number(values.categoryId),
    supplierId: values.supplierId === '' ? null : Number(values.supplierId),
    costCenterId: values.costCenterId === '' ? null : Number(values.costCenterId),
    currencyId: Number(values.currencyId),
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
    visibility: values.visibility,
    items: values.items.map((item) => ({
      description: item.description.trim(),
      quantity: parseAmount(item.quantity) ?? 0,
      unitAmount: parseAmount(item.unitAmount) ?? 0,
    })),
  }
}
