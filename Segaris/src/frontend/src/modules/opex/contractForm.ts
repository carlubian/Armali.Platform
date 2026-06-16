import { z } from 'zod'

import type {
  CreateOpexContractRequest,
  OpexContract,
  OpexContractStatus,
  OpexExpectedFrequency,
  OpexMovementType,
  OpexVisibility,
} from '@/app/api/opex'

export interface ContractFormValues {
  name: string
  movementType: OpexMovementType
  status: OpexContractStatus
  startDate: string
  closedDate: string
  estimatedAnnualAmount: string
  expectedFrequency: OpexExpectedFrequency
  categoryId: string
  supplierId: string
  costCenterId: string
  currencyId: string
  notes: string
  visibility: OpexVisibility
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  currencyRequired: string
  estimatedAmountInvalid: string
  notesTooLong: string
}

const twoDecimals = /^\d+(\.\d{1,2})?$/

export function parseAmount(value: string): number | null {
  const trimmed = value.trim()
  if (!twoDecimals.test(trimmed)) return null
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? parsed : null
}

export function createContractSchema(messages: SchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    movementType: z.enum(['Income', 'Expense']),
    status: z.enum(['Planning', 'Active', 'OnHold', 'Closed']),
    startDate: z.string(),
    closedDate: z.string(),
    estimatedAnnualAmount: z.string().refine((value) => {
      if (value.trim() === '') return true
      const parsed = parseAmount(value)
      return parsed != null && parsed >= 0
    }, messages.estimatedAmountInvalid),
    expectedFrequency: z.enum([
      'None',
      'Weekly',
      'Monthly',
      'Quarterly',
      'SemiAnnual',
      'Annual',
      'Irregular',
    ]),
    categoryId: z.string().min(1, messages.categoryRequired),
    supplierId: z.string(),
    costCenterId: z.string(),
    currencyId: z.string().min(1, messages.currencyRequired),
    notes: z.string().max(4000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

interface DefaultsParams {
  categoryId: string
  currencyId: string
}

export function buildDefaults({
  categoryId,
  currencyId,
}: DefaultsParams): ContractFormValues {
  return {
    name: '',
    movementType: 'Expense',
    status: 'Planning',
    startDate: '',
    closedDate: '',
    estimatedAnnualAmount: '',
    expectedFrequency: 'None',
    categoryId,
    supplierId: '',
    costCenterId: '',
    currencyId,
    notes: '',
    visibility: 'Public',
  }
}

export function fromContract(contract: OpexContract): ContractFormValues {
  return {
    name: contract.name,
    movementType: contract.movementType,
    status: contract.status,
    startDate: contract.startDate?.slice(0, 10) ?? '',
    closedDate: contract.closedDate?.slice(0, 10) ?? '',
    estimatedAnnualAmount:
      contract.estimatedAnnualAmount == null
        ? ''
        : String(contract.estimatedAnnualAmount),
    expectedFrequency: contract.expectedFrequency,
    categoryId: String(contract.categoryId),
    supplierId: contract.supplierId == null ? '' : String(contract.supplierId),
    costCenterId: contract.costCenterId == null ? '' : String(contract.costCenterId),
    currencyId: String(contract.currencyId),
    notes: contract.notes ?? '',
    visibility: contract.visibility,
  }
}

export function toRequest(values: ContractFormValues): CreateOpexContractRequest {
  const trimmedAmount = values.estimatedAnnualAmount.trim()
  return {
    name: values.name.trim(),
    movementType: values.movementType,
    status: values.status,
    startDate: values.startDate === '' ? null : values.startDate,
    closedDate: values.closedDate === '' ? null : values.closedDate,
    estimatedAnnualAmount:
      trimmedAmount === '' ? null : (parseAmount(trimmedAmount) ?? null),
    expectedFrequency: values.expectedFrequency,
    categoryId: Number(values.categoryId),
    supplierId: values.supplierId === '' ? null : Number(values.supplierId),
    costCenterId: values.costCenterId === '' ? null : Number(values.costCenterId),
    currencyId: Number(values.currencyId),
    notes: values.notes.trim() === '' ? null : values.notes.trim(),
    visibility: values.visibility,
  }
}
