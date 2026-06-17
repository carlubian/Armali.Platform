import { z } from 'zod'

import type {
  CreateTravelExpenseRequest,
  CreateTravelTripRequest,
  TravelExpenseListQuery,
  TravelItineraryEntryRequest,
  TravelTripListQuery,
} from '@/app/api/travel'

export const travelKeys = {
  all: ['travel'] as const,
  tripTypes: () => [...travelKeys.all, 'tripTypes'] as const,
  expenseCategories: () => [...travelKeys.all, 'expenseCategories'] as const,
  trips: () => [...travelKeys.all, 'trips'] as const,
  tripList: (query: TravelTripListQuery) => [...travelKeys.trips(), 'list', query] as const,
  trip: (tripId: number) => [...travelKeys.trips(), tripId] as const,
  tripAttachments: (tripId: number) => [...travelKeys.trip(tripId), 'attachments'] as const,
  expenses: (tripId: number) => [...travelKeys.trip(tripId), 'expenses'] as const,
  expenseList: (tripId: number, query: TravelExpenseListQuery) =>
    [...travelKeys.expenses(tripId), 'list', query] as const,
  expense: (tripId: number, expenseId: number) =>
    [...travelKeys.expenses(tripId), expenseId] as const,
  expenseAttachments: (tripId: number, expenseId: number) =>
    [...travelKeys.expense(tripId, expenseId), 'attachments'] as const,
}

const optionalText = (max: number) =>
  z
    .string()
    .trim()
    .max(max)
    .transform((value) => (value.length === 0 ? null : value))
    .nullable()

const requiredText = (max: number) => z.string().trim().min(1).max(max)
const moneySchema = z.number().nonnegative().multipleOf(0.01)

export const travelItineraryEntryRequestSchema = z.object({
  date: z.iso.date(),
  time: z.string().trim().max(16).nullable(),
  title: requiredText(200),
  place: optionalText(200),
  reservationLocator: optionalText(200),
  note: optionalText(1000),
}) satisfies z.ZodType<TravelItineraryEntryRequest>

export const travelTripRequestSchema = z
  .object({
    name: requiredText(200),
    tripTypeId: z.number().int().positive(),
    destination: optionalText(200),
    startDate: z.iso.date(),
    endDate: z.iso.date(),
    status: z.enum(['Planned', 'Ongoing', 'Completed', 'Cancelled']),
    notes: optionalText(4000),
    visibility: z.enum(['Public', 'Private']),
    itinerary: z.array(travelItineraryEntryRequestSchema).max(100),
  })
  .refine((value) => value.endDate >= value.startDate, {
    path: ['endDate'],
  }) satisfies z.ZodType<CreateTravelTripRequest>

export const travelExpenseRequestSchema = z.object({
  expenseCategoryId: z.number().int().positive(),
  description: requiredText(200),
  date: z.iso.date(),
  amount: moneySchema,
  currencyId: z.number().int().positive(),
  supplierId: z.number().int().positive().nullable(),
  costCenterId: z.number().int().positive().nullable(),
  notes: optionalText(4000),
}) satisfies z.ZodType<CreateTravelExpenseRequest>
