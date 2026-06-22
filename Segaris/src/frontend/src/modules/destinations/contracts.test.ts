import { describe, expect, it } from 'vitest'

import {
  destinationPageSizes,
  destinationPlacesRoutePath,
  destinationsRoutePath,
  placePageSizes,
} from '@/app/api/destinations'

import {
  destinationRequestSchema,
  destinationsKeys,
  placeRatings,
  placeRequestSchema,
} from './contracts'
import {
  defaultDestinationPageSize,
  defaultDestinationSort,
  defaultDestinationSortDirection,
  parseDestinationDialogState,
  parseDestinationsState,
  toDestinationListQuery,
} from './destinationsState'
import {
  defaultPlacePageSize,
  defaultPlaceSort,
  defaultPlaceSortDirection,
  parsePlaceDialogState,
  parsePlacesState,
  toPlaceListQuery,
} from './placesState'

describe('destinations contracts', () => {
  it('freezes route and pagination constants', () => {
    expect(destinationsRoutePath).toBe('/destinations')
    expect(destinationPlacesRoutePath(12)).toBe('/destinations/12/places')
    expect(destinationPageSizes).toEqual([10, 25, 50, 100])
    expect(placePageSizes).toEqual([10, 25, 50, 100])
    expect(defaultDestinationPageSize).toBe(25)
    expect(defaultPlacePageSize).toBe(25)
    expect(defaultDestinationSort).toBe('name')
    expect(defaultDestinationSortDirection).toBe('asc')
    expect(defaultPlaceSort).toBe('name')
    expect(defaultPlaceSortDirection).toBe('asc')
    expect(placeRatings).toEqual([1, 2, 3, 4, 5])
  })

  it('freezes query keys for destinations, places, catalogues, and attachments', () => {
    expect(destinationsKeys.categories()).toEqual(['destinations', 'categories'])
    expect(destinationsKeys.placeCategories()).toEqual([
      'destinations',
      'place-categories',
    ])
    expect(destinationsKeys.destinationList({ page: 1 })).toEqual([
      'destinations',
      'destinations',
      'list',
      { page: 1 },
    ])
    expect(destinationsKeys.destination(12)).toEqual([
      'destinations',
      'destinations',
      12,
    ])
    expect(destinationsKeys.destinationAttachments(12)).toEqual([
      'destinations',
      'destinations',
      12,
      'attachments',
    ])
    expect(destinationsKeys.placeList(12, { page: 1 })).toEqual([
      'destinations',
      'destinations',
      12,
      'places',
      'list',
      { page: 1 },
    ])
    expect(destinationsKeys.place(12, 45)).toEqual([
      'destinations',
      'destinations',
      12,
      'places',
      45,
    ])
  })

  it('parses URL-backed destination gallery state with defaults and bounded values', () => {
    const state = parseDestinationsState(
      new URLSearchParams(
        'search=barcelona&category=2&isSchengenArea=true&sort=category&sortDirection=desc&page=3&pageSize=50',
      ),
    )

    expect(state).toEqual({
      search: 'barcelona',
      category: 2,
      isSchengenArea: true,
      sort: 'category',
      sortDirection: 'desc',
      page: 3,
      pageSize: 50,
    })
    expect(toDestinationListQuery(state)).toEqual({
      search: 'barcelona',
      category: 2,
      isSchengenArea: true,
      page: 3,
      pageSize: 50,
      sort: 'category',
      sortDirection: 'desc',
    })
  })

  it('parses destination dialog state separately from gallery state', () => {
    expect(
      parseDestinationDialogState(new URLSearchParams('newDestination=true')),
    ).toEqual({
      mode: 'create',
    })
    expect(
      parseDestinationDialogState(new URLSearchParams('destinationId=12')),
    ).toEqual({
      mode: 'edit',
      destinationId: 12,
    })
    expect(parseDestinationDialogState(new URLSearchParams('destinationId=0'))).toEqual(
      {
        mode: 'closed',
      },
    )
  })

  it('parses URL-backed place list state under destination scope', () => {
    const state = parsePlacesState(
      new URLSearchParams(
        'search=hotel&category=3&rating=5&sort=rating&sortDirection=desc&page=2&pageSize=100',
      ),
    )

    expect(state).toEqual({
      search: 'hotel',
      category: 3,
      rating: 5,
      sort: 'rating',
      sortDirection: 'desc',
      page: 2,
      pageSize: 100,
    })
    expect(toPlaceListQuery(state)).toEqual({
      search: 'hotel',
      category: 3,
      rating: 5,
      page: 2,
      pageSize: 100,
      sort: 'rating',
      sortDirection: 'desc',
    })
  })

  it('parses place dialog state separately from place list state', () => {
    expect(parsePlaceDialogState(new URLSearchParams('newPlace=true'))).toEqual({
      mode: 'create',
    })
    expect(parsePlaceDialogState(new URLSearchParams('placeId=45'))).toEqual({
      mode: 'edit',
      placeId: 45,
    })
    expect(parsePlaceDialogState(new URLSearchParams('placeId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates destination request boundaries', () => {
    const request = destinationRequestSchema.parse({
      name: '  Barcelona  ',
      categoryId: 1,
      country: '  Spain  ',
      entryRequirements: '',
      isSchengenArea: true,
      notes: '',
      visibility: 'Public',
    })

    expect(request.name).toBe('Barcelona')
    expect(request.country).toBe('Spain')
    expect(request.entryRequirements).toBeNull()
    expect(request.notes).toBeNull()
    expect(
      destinationRequestSchema.safeParse({ ...request, visibility: 'Shared' }).success,
    ).toBe(false)
  })

  it('validates place request boundaries and rating bounds', () => {
    const request = placeRequestSchema.parse({
      name: '  Hotel  ',
      categoryId: 1,
      rating: 5,
      review: '',
      address: '  Main street  ',
    })

    expect(request.name).toBe('Hotel')
    expect(request.rating).toBe(5)
    expect(request.review).toBeNull()
    expect(request.address).toBe('Main street')
    expect(placeRequestSchema.safeParse({ ...request, rating: 0 }).success).toBe(false)
    expect(placeRequestSchema.safeParse({ ...request, rating: 6 }).success).toBe(false)
  })
})
