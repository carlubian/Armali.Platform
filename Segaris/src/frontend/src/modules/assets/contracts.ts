import { z } from 'zod'

import type { AssetListQuery, CreateAssetRequest } from '@/app/api/assets'

export const assetsKeys = {
  all: ['assets'] as const,
  categories: () => [...assetsKeys.all, 'categories'] as const,
  locations: () => [...assetsKeys.all, 'locations'] as const,
  assets: () => [...assetsKeys.all, 'assets'] as const,
  assetList: (query: AssetListQuery) =>
    [...assetsKeys.assets(), 'list', query] as const,
  asset: (assetId: number) => [...assetsKeys.assets(), assetId] as const,
  assetAttachments: (assetId: number) =>
    [...assetsKeys.asset(assetId), 'attachments'] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

const optionalDate = z
  .string()
  .trim()
  .refine((value) => value.length === 0 || /^\d{4}-\d{2}-\d{2}$/.test(value), {
    message: 'Date must be an ISO civil date (yyyy-MM-dd).',
  })
  .transform((value) => (value.length === 0 ? null : value))
  .nullable()

export const assetRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  locationId: z.number().int().positive(),
  status: z.enum(['Active', 'Stored', 'Retired']),
  code: optionalText(50),
  brandModel: optionalText(200),
  serialNumber: optionalText(200),
  acquisitionDate: optionalDate,
  expectedEndOfLifeDate: optionalDate,
  notes: optionalText(4000),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<CreateAssetRequest>
