import { describe, expect, it } from 'vitest'

import { inventoryPageSizes, inventoryRoutePath } from '@/app/api/inventory'
import {
  inventoryItemRequestSchema,
  inventoryKeys,
  inventoryOrderRequestSchema,
  inventoryStockAdjustmentRequestSchema,
} from './contracts'

describe('Inventory Wave 0 contracts', () => {
  it('freezes route, pagination, and hierarchical query keys', () => {
    expect(inventoryRoutePath).toBe('/inventory')
    expect(inventoryPageSizes).toEqual([10, 25, 50, 100])
    expect(inventoryKeys.item(7)).toEqual(['inventory', 'items', 7])
    expect(inventoryKeys.order(9)).toEqual(['inventory', 'orders', 9])
    expect(inventoryKeys.itemAttachments(7)).toEqual([
      'inventory',
      'items',
      7,
      'attachments',
    ])
  })

  it('accepts the documented item defaults', () => {
    const result = inventoryItemRequestSchema.safeParse({
      name: 'Olive oil',
      status: 'Candidate',
      notes: null,
      categoryId: 1,
      locationId: 1,
      currentStock: 0,
      minimumStock: 0,
      supplierIds: [1],
      visibility: 'Public',
    })

    expect(result.success).toBe(true)
  })

  it('requires at least one allowed supplier on an item', () => {
    expect(
      inventoryItemRequestSchema.safeParse({
        name: 'Olive oil',
        status: 'Candidate',
        notes: null,
        categoryId: 1,
        locationId: 1,
        currentStock: 0,
        minimumStock: 0,
        supplierIds: [],
        visibility: 'Public',
      }).success,
    ).toBe(false)
  })

  it('rejects non-positive or overprecise quick stock adjustments', () => {
    expect(
      inventoryStockAdjustmentRequestSchema.safeParse({
        direction: 'Decrease',
        quantity: 0,
      }).success,
    ).toBe(false)
    expect(
      inventoryStockAdjustmentRequestSchema.safeParse({
        direction: 'Increase',
        quantity: 1.001,
      }).success,
    ).toBe(false)
  })

  it('bounds an order between 1 and 100 lines', () => {
    const line = { itemId: 1, quantity: 1, lineTotal: 0 }
    const base = {
      supplierId: 1,
      status: 'Planning' as const,
      currencyId: 1,
      orderDate: '2026-06-16',
      expectedReceiptDate: '2026-06-23',
      notes: null,
      visibility: 'Public' as const,
    }

    expect(
      inventoryOrderRequestSchema.safeParse({ ...base, lines: [line] }).success,
    ).toBe(true)
    expect(inventoryOrderRequestSchema.safeParse({ ...base, lines: [] }).success).toBe(
      false,
    )
    expect(
      inventoryOrderRequestSchema.safeParse({
        ...base,
        lines: Array.from({ length: 101 }, () => line),
      }).success,
    ).toBe(false)
  })
})
