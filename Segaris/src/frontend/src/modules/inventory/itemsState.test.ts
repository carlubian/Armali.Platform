import { describe, expect, it } from 'vitest'

import {
  activeItemFilterCount,
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  toListQuery,
  type ItemsState,
} from './itemsState'

function baseState(): ItemsState {
  return {
    search: '',
    status: '',
    category: null,
    location: null,
    supplier: null,
    visibility: '',
    mine: false,
    sort: defaultSort,
    sortDirection: defaultSortDirection,
    page: 1,
    pageSize: defaultPageSize,
  }
}

describe('itemsState defaults', () => {
  it('defaults to name ascending with page 1 and 25 rows', () => {
    const state = baseState()
    expect(state.sort).toBe('name')
    expect(state.sortDirection).toBe('asc')
    expect(state.page).toBe(1)
    expect(state.pageSize).toBe(25)
  })

  it('activeItemFilterCount returns zero for a clean state', () => {
    expect(activeItemFilterCount(baseState())).toBe(0)
  })

  it('counts each active filter independently', () => {
    const state: ItemsState = {
      ...baseState(),
      search: 'soap',
      status: 'Active',
      category: 1,
      location: 2,
      supplier: 3,
      visibility: 'Private',
      mine: true,
    }
    expect(activeItemFilterCount(state)).toBe(7)
  })
})

describe('itemsState toListQuery', () => {
  it('maps a clean state to a minimal query', () => {
    const query = toListQuery(baseState(), null)
    expect(query.search).toBeNull()
    expect(query.status).toBeNull()
    expect(query.category).toBeNull()
    expect(query.location).toBeNull()
    expect(query.supplier).toBeNull()
    expect(query.visibility).toBeNull()
    expect(query.creator).toBeNull()
    expect(query.page).toBe(1)
    expect(query.pageSize).toBe(25)
    expect(query.sort).toBe('name')
    expect(query.sortDirection).toBe('asc')
  })

  it('passes trimmed search and active filters through', () => {
    const state: ItemsState = {
      ...baseState(),
      search: '  detergent  ',
      status: 'Deprecated',
      category: 5,
      location: 6,
      visibility: 'Public',
    }
    const query = toListQuery(state, null)
    expect(query.search).toBe('detergent')
    expect(query.status).toBe('Deprecated')
    expect(query.category).toBe(5)
    expect(query.location).toBe(6)
    expect(query.visibility).toBe('Public')
  })

  it('maps mine to the current user id and false to null', () => {
    expect(toListQuery({ ...baseState(), mine: true }, 42).creator).toBe(42)
    expect(toListQuery({ ...baseState(), mine: true }, null).creator).toBeNull()
    expect(toListQuery({ ...baseState(), mine: false }, 7).creator).toBeNull()
  })
})
