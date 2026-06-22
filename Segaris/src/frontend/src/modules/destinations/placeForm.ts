import { z } from 'zod'

import type { CreatePlaceRequest, Place } from '@/app/api/destinations'

export interface PlaceFormValues {
  name: string
  categoryId: string
  rating: '' | '1' | '2' | '3' | '4' | '5'
  review: string
  address: string
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  categoryRequired: string
  reviewTooLong: string
  addressTooLong: string
}

export function createPlaceFormSchema(messages: SchemaMessages) {
  return z.object({
    name: z
      .string()
      .trim()
      .min(1, messages.nameRequired)
      .max(200, messages.nameTooLong),
    categoryId: z.string().min(1, messages.categoryRequired),
    rating: z.enum(['', '1', '2', '3', '4', '5']),
    review: z.string().max(2000, messages.reviewTooLong),
    address: z.string().max(200, messages.addressTooLong),
  })
}

export function buildPlaceDefaults(categoryId: string): PlaceFormValues {
  return {
    name: '',
    categoryId,
    rating: '',
    review: '',
    address: '',
  }
}

export function fromPlace(place: Place): PlaceFormValues {
  return {
    name: place.name,
    categoryId: String(place.categoryId),
    rating:
      place.rating == null ? '' : (String(place.rating) as PlaceFormValues['rating']),
    review: place.review ?? '',
    address: place.address ?? '',
  }
}

function nullableText(value: string): string | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

export function toPlaceRequest(values: PlaceFormValues): CreatePlaceRequest {
  return {
    name: values.name.trim(),
    categoryId: Number(values.categoryId),
    rating:
      values.rating === ''
        ? null
        : (Number(values.rating) as CreatePlaceRequest['rating']),
    review: nullableText(values.review),
    address: nullableText(values.address),
  }
}
