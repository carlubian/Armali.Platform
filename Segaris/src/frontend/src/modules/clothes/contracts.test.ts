import { describe, expect, it } from 'vitest'

import { clothesPageSizes, clothesRoutePath } from '@/app/api/clothes'

import { clothesGarmentRequestSchema, clothesKeys } from './contracts'
import {
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  parseGarmentDialogState,
  parseGarmentsState,
  toListQuery,
} from './garmentsState'

describe('clothes contracts', () => {
  it('freezes route and pagination constants', () => {
    expect(clothesRoutePath).toBe('/clothes')
    expect(clothesPageSizes).toEqual([10, 25, 50, 100])
    expect(defaultPageSize).toBe(25)
    expect(defaultSort).toBe('name')
    expect(defaultSortDirection).toBe('asc')
  })

  it('freezes query keys for garments, catalogues, and attachments', () => {
    expect(clothesKeys.categories()).toEqual(['clothes', 'categories'])
    expect(clothesKeys.colors()).toEqual(['clothes', 'colors'])
    expect(clothesKeys.garmentList({ page: 1 })).toEqual([
      'clothes',
      'garments',
      'list',
      { page: 1 },
    ])
    expect(clothesKeys.garment(12)).toEqual(['clothes', 'garments', 12])
    expect(clothesKeys.garmentAttachments(12)).toEqual([
      'clothes',
      'garments',
      12,
      'attachments',
    ])
  })

  it('parses URL-backed gallery state with defaults and bounded values', () => {
    const state = parseGarmentsState(
      new URLSearchParams(
        'search=jacket&category=2&status=Active&color=5&visibility=Private&creator=9&sort=category&sortDirection=desc&page=3&pageSize=50',
      ),
      9,
    )

    expect(state).toEqual({
      search: 'jacket',
      category: 2,
      status: 'Active',
      color: 5,
      visibility: 'Private',
      mine: true,
      sort: 'category',
      sortDirection: 'desc',
      page: 3,
      pageSize: 50,
    })
    expect(toListQuery(state, 9)).toEqual({
      search: 'jacket',
      category: 2,
      status: 'Active',
      color: 5,
      visibility: 'Private',
      creator: 9,
      page: 3,
      pageSize: 50,
      sort: 'category',
      sortDirection: 'desc',
    })
  })

  it('parses garment dialog state separately from gallery state', () => {
    expect(parseGarmentDialogState(new URLSearchParams('newGarment=true'))).toEqual({
      mode: 'create',
    })
    expect(parseGarmentDialogState(new URLSearchParams('garmentId=12'))).toEqual({
      mode: 'edit',
      garmentId: 12,
    })
    expect(parseGarmentDialogState(new URLSearchParams('garmentId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates garment request boundaries', () => {
    const request = clothesGarmentRequestSchema.parse({
      name: '  Jacket  ',
      categoryId: 1,
      status: 'Active',
      size: '',
      colorIds: [2, 3],
      washingCare: 'Wash30',
      dryingCare: null,
      ironingCare: 'Low',
      dryCleaningCare: 'DoNotDryClean',
      notes: '',
      visibility: 'Public',
    })

    expect(request.name).toBe('Jacket')
    expect(request.size).toBeNull()
    expect(request.notes).toBeNull()
    expect(
      clothesGarmentRequestSchema.safeParse({
        ...request,
        washingCare: 'Bleach',
      }).success,
    ).toBe(false)
  })
})
