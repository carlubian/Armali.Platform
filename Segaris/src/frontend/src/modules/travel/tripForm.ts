import { z } from 'zod'

import type {
  CreateTravelTripRequest,
  TravelItineraryEntry,
  TravelTrip,
  TravelTripStatus,
  TravelVisibility,
} from '@/app/api/travel'

export interface ItineraryEntryFormValues {
  date: string
  time: string
  title: string
  place: string
  reservationLocator: string
  note: string
}

export interface TripFormValues {
  name: string
  tripTypeId: string
  destinationId: string
  startDate: string
  endDate: string
  status: TravelTripStatus
  notes: string
  visibility: TravelVisibility
  itinerary: ItineraryEntryFormValues[]
}

interface SchemaMessages {
  nameRequired: string
  nameTooLong: string
  tripTypeRequired: string
  startDateRequired: string
  endDateRequired: string
  endBeforeStart: string
  notesTooLong: string
  entryDateRequired: string
  entryTitleRequired: string
  entryTitleTooLong: string
  entryPlaceTooLong: string
  entryLocatorTooLong: string
  entryNoteTooLong: string
}

export const maxItineraryEntries = 100

export function createTripSchema(messages: SchemaMessages) {
  const entry = z.object({
    date: z.string().min(1, messages.entryDateRequired),
    time: z.string().max(16),
    title: z
      .string()
      .trim()
      .min(1, messages.entryTitleRequired)
      .max(200, messages.entryTitleTooLong),
    place: z.string().max(200, messages.entryPlaceTooLong),
    reservationLocator: z.string().max(200, messages.entryLocatorTooLong),
    note: z.string().max(1000, messages.entryNoteTooLong),
  })

  return z
    .object({
      name: z
        .string()
        .trim()
        .min(1, messages.nameRequired)
        .max(200, messages.nameTooLong),
      tripTypeId: z.string().min(1, messages.tripTypeRequired),
      destinationId: z.string(),
      startDate: z.string().min(1, messages.startDateRequired),
      endDate: z.string().min(1, messages.endDateRequired),
      status: z.enum(['Planned', 'Ongoing', 'Completed', 'Cancelled']),
      notes: z.string().max(4000, messages.notesTooLong),
      visibility: z.enum(['Public', 'Private']),
      itinerary: z.array(entry).max(maxItineraryEntries),
    })
    .refine((value) => value.endDate >= value.startDate, {
      path: ['endDate'],
      message: messages.endBeforeStart,
    })
}

/** The household's local date (Europe/Madrid) as a `yyyy-mm-dd` string. */
export function householdToday(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Madrid',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date())
}

export function blankItineraryEntry(date: string): ItineraryEntryFormValues {
  return {
    date,
    time: '',
    title: '',
    place: '',
    reservationLocator: '',
    note: '',
  }
}

interface DefaultsParams {
  tripTypeId: string
}

export function buildDefaults({ tripTypeId }: DefaultsParams): TripFormValues {
  const today = householdToday()
  return {
    name: '',
    tripTypeId,
    destinationId: '',
    startDate: today,
    endDate: today,
    status: 'Planned',
    notes: '',
    visibility: 'Public',
    itinerary: [],
  }
}

function entryFromApi(entry: TravelItineraryEntry): ItineraryEntryFormValues {
  return {
    date: entry.date,
    time: entry.time ?? '',
    title: entry.title,
    place: entry.place ?? '',
    reservationLocator: entry.reservationLocator ?? '',
    note: entry.note ?? '',
  }
}

export function fromTrip(trip: TravelTrip): TripFormValues {
  return {
    name: trip.name,
    tripTypeId: String(trip.tripTypeId),
    destinationId: trip.destinationId == null ? '' : String(trip.destinationId),
    startDate: trip.startDate,
    endDate: trip.endDate,
    status: trip.status,
    notes: trip.notes ?? '',
    visibility: trip.visibility,
    itinerary: trip.itinerary.map(entryFromApi),
  }
}

const blankToNull = (value: string): string | null => {
  const trimmed = value.trim()
  return trimmed === '' ? null : trimmed
}

const referenceToNull = (value: string): number | null => {
  const trimmed = value.trim()
  return trimmed === '' ? null : Number(trimmed)
}

export function toRequest(values: TripFormValues): CreateTravelTripRequest {
  return {
    name: values.name.trim(),
    tripTypeId: Number(values.tripTypeId),
    destinationId: referenceToNull(values.destinationId),
    startDate: values.startDate,
    endDate: values.endDate,
    status: values.status,
    notes: blankToNull(values.notes),
    visibility: values.visibility,
    itinerary: values.itinerary.map((entry) => ({
      date: entry.date,
      time: blankToNull(entry.time),
      title: entry.title.trim(),
      place: blankToNull(entry.place),
      reservationLocator: blankToNull(entry.reservationLocator),
      note: blankToNull(entry.note),
    })),
  }
}
