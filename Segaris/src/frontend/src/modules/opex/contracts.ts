import { z } from 'zod'

import type {
  CreateOpexContractRequest,
  CreateOpexOccurrenceRequest,
  OpexContractListQuery,
  OpexOccurrenceListQuery,
} from '@/app/api/opex'

export const opexKeys = {
  all: ['opex'] as const,
  categories: () => [...opexKeys.all, 'categories'] as const,
  contracts: () => [...opexKeys.all, 'contracts'] as const,
  contractList: (query: OpexContractListQuery) =>
    [...opexKeys.contracts(), 'list', query] as const,
  contract: (contractId: number) => [...opexKeys.contracts(), contractId] as const,
  contractAttachments: (contractId: number) =>
    [...opexKeys.contract(contractId), 'attachments'] as const,
  occurrences: (contractId: number) =>
    [...opexKeys.contract(contractId), 'occurrences'] as const,
  occurrenceList: (contractId: number, query: OpexOccurrenceListQuery) =>
    [...opexKeys.occurrences(contractId), 'list', query] as const,
  occurrence: (contractId: number, occurrenceId: number) =>
    [...opexKeys.occurrences(contractId), occurrenceId] as const,
  occurrenceAttachments: (contractId: number, occurrenceId: number) =>
    [...opexKeys.occurrence(contractId, occurrenceId), 'attachments'] as const,
}

const amountSchema = z.number().nonnegative().multipleOf(0.01)
const optionalIdSchema = z.number().int().positive().nullable()

export const opexContractRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  movementType: z.enum(['Income', 'Expense']),
  status: z.enum(['Planning', 'Active', 'OnHold', 'Closed']),
  startDate: z.iso.date().nullable(),
  closedDate: z.iso.date().nullable(),
  estimatedAnnualAmount: amountSchema.nullable(),
  expectedFrequency: z.enum([
    'None',
    'Weekly',
    'Monthly',
    'Quarterly',
    'SemiAnnual',
    'Annual',
    'Irregular',
  ]),
  categoryId: z.number().int().positive(),
  supplierId: optionalIdSchema,
  costCenterId: optionalIdSchema,
  currencyId: z.number().int().positive(),
  notes: z.string().max(4000).nullable(),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<CreateOpexContractRequest>

export const opexOccurrenceRequestSchema = z.object({
  effectiveDate: z.iso.date(),
  actualAmount: amountSchema,
  description: z.string().trim().max(300).nullable(),
  notes: z.string().max(4000).nullable(),
}) satisfies z.ZodType<CreateOpexOccurrenceRequest>
