import { describe, expect, it } from 'vitest'

import { opexPageSizes, opexRoutePath } from '@/app/api/opex'
import {
  opexContractRequestSchema,
  opexKeys,
  opexOccurrenceRequestSchema,
} from './contracts'

describe('Opex Wave 0 contracts', () => {
  it('freezes route, pagination, and hierarchical query keys', () => {
    expect(opexRoutePath).toBe('/opex')
    expect(opexPageSizes).toEqual([10, 25, 50, 100])
    expect(opexKeys.contract(7)).toEqual(['opex', 'contracts', 7])
    expect(opexKeys.occurrence(7, 9)).toEqual([
      'opex',
      'contracts',
      7,
      'occurrences',
      9,
    ])
  })

  it('accepts the documented contract defaults', () => {
    const result = opexContractRequestSchema.safeParse({
      name: 'Electricity',
      movementType: 'Expense',
      status: 'Planning',
      startDate: null,
      closedDate: null,
      estimatedAnnualAmount: null,
      expectedFrequency: 'None',
      categoryId: 1,
      supplierId: null,
      costCenterId: null,
      currencyId: 1,
      notes: null,
      visibility: 'Public',
    })

    expect(result.success).toBe(true)
  })

  it('rejects invalid amounts and overlong occurrence descriptions', () => {
    expect(
      opexOccurrenceRequestSchema.safeParse({
        effectiveDate: '2026-06-15',
        actualAmount: -1,
        description: 'x'.repeat(301),
        notes: null,
      }).success,
    ).toBe(false)
    expect(
      opexOccurrenceRequestSchema.safeParse({
        effectiveDate: '2026-06-15',
        actualAmount: 1.001,
        description: null,
        notes: null,
      }).success,
    ).toBe(false)
  })
})
