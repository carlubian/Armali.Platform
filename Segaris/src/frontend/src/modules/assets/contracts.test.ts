import { describe, expect, it } from 'vitest'

import { assetPageSizes, assetsRoutePath } from '@/app/api/assets'

import { assetRequestSchema, assetsKeys } from './contracts'
import {
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  parseAssetDialogState,
  parseAssetsState,
  toListQuery,
} from './assetsState'

describe('assets contracts', () => {
  it('freezes route and pagination constants', () => {
    expect(assetsRoutePath).toBe('/assets')
    expect(assetPageSizes).toEqual([10, 25, 50, 100])
    expect(defaultPageSize).toBe(25)
    expect(defaultSort).toBe('name')
    expect(defaultSortDirection).toBe('asc')
  })

  it('freezes query keys for assets, catalogues, and attachments', () => {
    expect(assetsKeys.categories()).toEqual(['assets', 'categories'])
    expect(assetsKeys.locations()).toEqual(['assets', 'locations'])
    expect(assetsKeys.assetList({ page: 1 })).toEqual([
      'assets',
      'assets',
      'list',
      { page: 1 },
    ])
    expect(assetsKeys.asset(12)).toEqual(['assets', 'assets', 12])
    expect(assetsKeys.assetAttachments(12)).toEqual([
      'assets',
      'assets',
      12,
      'attachments',
    ])
  })

  it('parses URL-backed table state with defaults and bounded values', () => {
    const state = parseAssetsState(
      new URLSearchParams(
        'search=drill&category=2&location=4&status=Stored&visibility=Private&creator=9&sort=category&sortDirection=desc&page=3&pageSize=50',
      ),
      9,
    )

    expect(state).toEqual({
      search: 'drill',
      category: 2,
      location: 4,
      status: 'Stored',
      visibility: 'Private',
      mine: true,
      sort: 'category',
      sortDirection: 'desc',
      page: 3,
      pageSize: 50,
    })
    expect(toListQuery(state, 9)).toEqual({
      search: 'drill',
      category: 2,
      location: 4,
      status: 'Stored',
      visibility: 'Private',
      creator: 9,
      page: 3,
      pageSize: 50,
      sort: 'category',
      sortDirection: 'desc',
    })
  })

  it('parses asset dialog state separately from table state', () => {
    expect(parseAssetDialogState(new URLSearchParams('newAsset=true'))).toEqual({
      mode: 'create',
    })
    expect(parseAssetDialogState(new URLSearchParams('assetId=12'))).toEqual({
      mode: 'edit',
      assetId: 12,
    })
    expect(parseAssetDialogState(new URLSearchParams('assetId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates asset request boundaries', () => {
    const request = assetRequestSchema.parse({
      name: '  Drill  ',
      categoryId: 1,
      locationId: 2,
      status: 'Active',
      code: '',
      brandModel: 'Bosch',
      serialNumber: '',
      acquisitionDate: '2026-01-15',
      expectedEndOfLifeDate: '',
      notes: '',
      visibility: 'Public',
    })

    expect(request.name).toBe('Drill')
    expect(request.code).toBeNull()
    expect(request.serialNumber).toBeNull()
    expect(request.expectedEndOfLifeDate).toBeNull()
    expect(request.acquisitionDate).toBe('2026-01-15')
    expect(request.notes).toBeNull()
    expect(assetRequestSchema.safeParse({ ...request, status: 'Broken' }).success).toBe(
      false,
    )
    expect(
      assetRequestSchema.safeParse({
        ...request,
        expectedEndOfLifeDate: '15-01-2026',
      }).success,
    ).toBe(false)
  })
})
