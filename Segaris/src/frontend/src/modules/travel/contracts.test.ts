import { describe, expect, it } from 'vitest'

import { travelPageSizes, travelRoutePath } from '@/app/api/travel'

import {
  travelExpenseRequestSchema,
  travelItineraryEntryRequestSchema,
  travelKeys,
  travelTripRequestSchema,
} from './contracts'
import {
  activeTripFilterCount,
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  parseTripDialogState,
  parseTripsState,
  toListQuery,
} from './tripsState'

describe('travel contracts', () => {
  it('freezes route and pagination constants', () => {
    expect(travelRoutePath).toBe('/travel')
    expect(travelPageSizes).toEqual([10, 25, 50, 100])
    expect(defaultPageSize).toBe(25)
    expect(defaultSort).toBe('startDate')
    expect(defaultSortDirection).toBe('desc')
  })

  it('freezes query keys for trips, expenses, and attachments', () => {
    expect(travelKeys.tripTypes()).toEqual(['travel', 'tripTypes'])
    expect(travelKeys.expenseCategories()).toEqual(['travel', 'expenseCategories'])
    expect(travelKeys.tripList({ page: 1 })).toEqual([
      'travel',
      'trips',
      'list',
      { page: 1 },
    ])
    expect(travelKeys.trip(12)).toEqual(['travel', 'trips', 12])
    expect(travelKeys.tripAttachments(12)).toEqual([
      'travel',
      'trips',
      12,
      'attachments',
    ])
    expect(travelKeys.expenseList(12, { page: 1 })).toEqual([
      'travel',
      'trips',
      12,
      'expenses',
      'list',
      { page: 1 },
    ])
    expect(travelKeys.expenseAttachments(12, 34)).toEqual([
      'travel',
      'trips',
      12,
      'expenses',
      34,
      'attachments',
    ])
  })

  it('parses URL-backed list state with defaults and bounded values', () => {
    const state = parseTripsState(
      new URLSearchParams(
        'search=porto&tripType=2&status=Ongoing&visibility=Private&creator=9&sort=name&sortDirection=asc&page=3&pageSize=50',
      ),
      9,
    )

    expect(state).toEqual({
      search: 'porto',
      tripType: 2,
      status: 'Ongoing',
      visibility: 'Private',
      mine: true,
      sort: 'name',
      sortDirection: 'asc',
      page: 3,
      pageSize: 50,
    })
    expect(toListQuery(state, 9)).toEqual({
      search: 'porto',
      tripType: 2,
      status: 'Ongoing',
      visibility: 'Private',
      creator: 9,
      page: 3,
      pageSize: 50,
      sort: 'name',
      sortDirection: 'asc',
    })
    expect(activeTripFilterCount(state)).toBe(5)
  })

  it('parses trip dialog state separately from list state', () => {
    expect(parseTripDialogState(new URLSearchParams('newTrip=true'))).toEqual({
      mode: 'create',
    })
    expect(parseTripDialogState(new URLSearchParams('tripId=12'))).toEqual({
      mode: 'edit',
      tripId: 12,
    })
    expect(parseTripDialogState(new URLSearchParams('tripId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates trip and itinerary request boundaries', () => {
    const itinerary = travelItineraryEntryRequestSchema.parse({
      date: '2026-06-17',
      time: null,
      title: 'Flight to Porto',
      place: '',
      reservationLocator: 'ABC123',
      note: '',
    })
    expect(itinerary.place).toBeNull()
    expect(itinerary.note).toBeNull()

    expect(
      travelTripRequestSchema.safeParse({
        name: 'Porto',
        tripTypeId: 1,
        destinationId: 10,
        startDate: '2026-06-17',
        endDate: '2026-06-16',
        status: 'Planned',
        notes: null,
        visibility: 'Public',
        itinerary: [],
      }).success,
    ).toBe(false)
  })

  it('validates expense request boundaries', () => {
    expect(
      travelExpenseRequestSchema.parse({
        expenseCategoryId: 1,
        description: 'Hotel',
        date: '2026-06-17',
        amount: 123.45,
        currencyId: 2,
        supplierId: null,
        costCenterId: null,
        notes: '',
      }).notes,
    ).toBeNull()

    expect(
      travelExpenseRequestSchema.safeParse({
        expenseCategoryId: 1,
        description: 'Hotel',
        date: '2026-06-17',
        amount: 12.345,
        currencyId: 2,
        supplierId: null,
        costCenterId: null,
        notes: null,
      }).success,
    ).toBe(false)
  })
})
