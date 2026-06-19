import { z } from 'zod'

import type {
  Asset,
  AssetStatus,
  AssetVisibility,
  CreateAssetRequest,
} from '@/app/api/assets'

export interface AssetFormValues {
  name: string
  categoryId: string
  locationId: string
  status: AssetStatus
  code: string
  brandModel: string
  serialNumber: string
  acquisitionDate: string
  expectedEndOfLifeDate: string
  notes: string
  visibility: AssetVisibility
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  locationRequired: string
  codeTooLong: string
  brandModelTooLong: string
  serialNumberTooLong: string
  dateInvalid: string
  notesTooLong: string
}

const dateOnly = /^\d{4}-\d{2}-\d{2}$/

function optionalDate(message: string) {
  return z
    .string()
    .trim()
    .refine((value) => value.length === 0 || dateOnly.test(value), message)
}

export function createAssetSchema(messages: SchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().min(1, messages.categoryRequired),
    locationId: z.string().min(1, messages.locationRequired),
    status: z.enum(['Active', 'Stored', 'Retired']),
    code: z.string().trim().max(50, messages.codeTooLong),
    brandModel: z.string().trim().max(200, messages.brandModelTooLong),
    serialNumber: z.string().trim().max(200, messages.serialNumberTooLong),
    acquisitionDate: optionalDate(messages.dateInvalid),
    expectedEndOfLifeDate: optionalDate(messages.dateInvalid),
    notes: z.string().max(4000, messages.notesTooLong),
    visibility: z.enum(['Public', 'Private']),
  })
}

export function buildDefaults(params: {
  categoryId: string
  locationId: string
}): AssetFormValues {
  return {
    name: '',
    categoryId: params.categoryId,
    locationId: params.locationId,
    status: 'Active',
    code: '',
    brandModel: '',
    serialNumber: '',
    acquisitionDate: '',
    expectedEndOfLifeDate: '',
    notes: '',
    visibility: 'Public',
  }
}

export function fromAsset(asset: Asset): AssetFormValues {
  return {
    name: asset.name,
    categoryId: String(asset.categoryId),
    locationId: String(asset.locationId),
    status: asset.status,
    code: asset.code ?? '',
    brandModel: asset.brandModel ?? '',
    serialNumber: asset.serialNumber ?? '',
    acquisitionDate: asset.acquisitionDate ?? '',
    expectedEndOfLifeDate: asset.expectedEndOfLifeDate ?? '',
    notes: asset.notes ?? '',
    visibility: asset.visibility,
  }
}

function nullableText(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

function nullableDate(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

export function toRequest(values: AssetFormValues): CreateAssetRequest {
  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    locationId: Number(values.locationId),
    status: values.status,
    code: nullableText(values.code),
    brandModel: nullableText(values.brandModel),
    serialNumber: nullableText(values.serialNumber),
    acquisitionDate: nullableDate(values.acquisitionDate),
    expectedEndOfLifeDate: nullableDate(values.expectedEndOfLifeDate),
    notes: nullableText(values.notes),
    visibility: values.visibility,
  }
}
