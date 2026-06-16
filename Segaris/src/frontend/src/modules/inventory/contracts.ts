import { z } from 'zod'

import type {
  CreateInventoryItemRequest,
  CreateInventoryOrderRequest,
  InventoryItemListQuery,
  InventoryOrderLineRequest,
  InventoryOrderListQuery,
  InventoryStockAdjustmentRequest,
} from '@/app/api/inventory'

export const inventoryKeys = {
  all: ['inventory'] as const,
  categories: () => [...inventoryKeys.all, 'categories'] as const,
  locations: () => [...inventoryKeys.all, 'locations'] as const,
  items: () => [...inventoryKeys.all, 'items'] as const,
  itemList: (query: InventoryItemListQuery) =>
    [...inventoryKeys.items(), 'list', query] as const,
  item: (itemId: number) => [...inventoryKeys.items(), itemId] as const,
  itemAttachments: (itemId: number) =>
    [...inventoryKeys.item(itemId), 'attachments'] as const,
  orders: () => [...inventoryKeys.all, 'orders'] as const,
  orderList: (query: InventoryOrderListQuery) =>
    [...inventoryKeys.orders(), 'list', query] as const,
  order: (orderId: number) => [...inventoryKeys.orders(), orderId] as const,
  orderAttachments: (orderId: number) =>
    [...inventoryKeys.order(orderId), 'attachments'] as const,
}

const stockSchema = z.number().nonnegative().multipleOf(0.01)
const positiveQuantitySchema = z.number().positive().multipleOf(0.01)
const lineTotalSchema = z.number().nonnegative().multipleOf(0.01)

export const inventoryItemRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  status: z.enum(['Candidate', 'Active', 'Deprecated']),
  notes: z.string().max(4000).nullable(),
  categoryId: z.number().int().positive(),
  locationId: z.number().int().positive(),
  currentStock: stockSchema,
  minimumStock: stockSchema,
  supplierIds: z.array(z.number().int().positive()).min(1),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<CreateInventoryItemRequest>

export const inventoryStockAdjustmentRequestSchema = z.object({
  direction: z.enum(['Increase', 'Decrease']),
  quantity: positiveQuantitySchema,
}) satisfies z.ZodType<InventoryStockAdjustmentRequest>

export const inventoryOrderLineRequestSchema = z.object({
  itemId: z.number().int().positive(),
  quantity: positiveQuantitySchema,
  lineTotal: lineTotalSchema,
}) satisfies z.ZodType<InventoryOrderLineRequest>

export const inventoryOrderRequestSchema = z.object({
  supplierId: z.number().int().positive(),
  status: z.enum(['Planning', 'Active', 'Received', 'Cancelled']),
  currencyId: z.number().int().positive(),
  orderDate: z.iso.date().nullable(),
  expectedReceiptDate: z.iso.date().nullable(),
  notes: z.string().max(4000).nullable(),
  visibility: z.enum(['Public', 'Private']),
  lines: z.array(inventoryOrderLineRequestSchema).min(1).max(100),
}) satisfies z.ZodType<CreateInventoryOrderRequest>
