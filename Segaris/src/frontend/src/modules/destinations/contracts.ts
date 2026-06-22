import { z } from 'zod'

import type {
  CreateDestinationRequest,
  CreatePlaceRequest,
  DestinationListQuery,
  PlaceListQuery,
} from '@/app/api/destinations'

export const destinationsKeys = {
  all: ['destinations'] as const,
  categories: () => [...destinationsKeys.all, 'categories'] as const,
  placeCategories: () => [...destinationsKeys.all, 'place-categories'] as const,
  destinations: () => [...destinationsKeys.all, 'destinations'] as const,
  destinationList: (query: DestinationListQuery) =>
    [...destinationsKeys.destinations(), 'list', query] as const,
  destination: (destinationId: number) =>
    [...destinationsKeys.destinations(), destinationId] as const,
  destinationAttachments: (destinationId: number) =>
    [...destinationsKeys.destination(destinationId), 'attachments'] as const,
  places: (destinationId: number) =>
    [...destinationsKeys.destination(destinationId), 'places'] as const,
  placeList: (destinationId: number, query: PlaceListQuery) =>
    [...destinationsKeys.places(destinationId), 'list', query] as const,
  place: (destinationId: number, placeId: number) =>
    [...destinationsKeys.places(destinationId), placeId] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

export const placeRatings = [1, 2, 3, 4, 5] as const

export const destinationRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  country: optionalText(200),
  entryRequirements: optionalText(2000),
  isSchengenArea: z.boolean(),
  notes: optionalText(2000),
  visibility: z.enum(['Public', 'Private']),
}) satisfies z.ZodType<CreateDestinationRequest>

export const placeRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
  categoryId: z.number().int().positive(),
  rating: z
    .union([z.literal(1), z.literal(2), z.literal(3), z.literal(4), z.literal(5)])
    .nullable(),
  review: optionalText(2000),
  address: optionalText(200),
}) satisfies z.ZodType<CreatePlaceRequest>

export const destinationCategoryRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
})

export const placeCategoryRequestSchema = z.object({
  name: z.string().trim().min(1).max(200),
})
