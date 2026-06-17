import { z } from 'zod'

import type { CreateTravelExpenseRequest, TravelExpense } from '@/app/api/travel'

import { householdToday } from './tripForm'

export interface ExpenseFormValues {
  expenseCategoryId: string
  description: string
  date: string
  amount: string
  currencyId: string
  supplierId: string
  costCenterId: string
  notes: string
}

interface SchemaMessages {
  categoryRequired: string
  descriptionRequired: string
  descriptionTooLong: string
  dateRequired: string
  amountInvalid: string
  currencyRequired: string
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

export function createExpenseSchema(messages: SchemaMessages) {
  return z.object({
    expenseCategoryId: z.string().min(1, messages.categoryRequired),
    description: z
      .string()
      .trim()
      .min(1, messages.descriptionRequired)
      .max(200, messages.descriptionTooLong),
    date: z.string().min(1, messages.dateRequired),
    amount: z
      .string()
      .refine((value) => parseAmount(value) != null, messages.amountInvalid),
    currencyId: z.string().min(1, messages.currencyRequired),
    supplierId: z.string(),
    costCenterId: z.string(),
    notes: z.string().max(4000, messages.notesTooLong),
  })
}

interface DefaultsParams {
  expenseCategoryId: string
}

export function buildDefaults({
  expenseCategoryId,
}: DefaultsParams): ExpenseFormValues {
  return {
    expenseCategoryId,
    description: '',
    date: householdToday(),
    amount: '0',
    currencyId: '',
    supplierId: '',
    costCenterId: '',
    notes: '',
  }
}

export function fromExpense(expense: TravelExpense): ExpenseFormValues {
  return {
    expenseCategoryId: String(expense.expenseCategoryId),
    description: expense.description,
    date: expense.date,
    amount: String(expense.amount),
    currencyId: String(expense.currencyId),
    supplierId: expense.supplierId == null ? '' : String(expense.supplierId),
    costCenterId: expense.costCenterId == null ? '' : String(expense.costCenterId),
    notes: expense.notes ?? '',
  }
}

const idOrNull = (value: string): number | null =>
  value.trim() === '' ? null : Number(value)

export function toRequest(values: ExpenseFormValues): CreateTravelExpenseRequest {
  return {
    expenseCategoryId: Number(values.expenseCategoryId),
    description: values.description.trim(),
    date: values.date,
    amount: parseAmount(values.amount) ?? 0,
    currencyId: Number(values.currencyId),
    supplierId: idOrNull(values.supplierId),
    costCenterId: idOrNull(values.costCenterId),
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
  }
}
