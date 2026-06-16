import { describe, expect, it } from 'vitest'

import {
  activeOrderFilterCount,
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  toListQuery,
  type OrdersState,
} from './ordersState'

function baseState(): OrdersState {
  return {
    search: '',
    supplier: null,
    status: '',
    currency: null,
    visibility: '',
    mine: false,
    sort: defaultSort,
    sortDirection: defaultSortDirection,
    page: 1,
    pageSize: defaultPageSize,
  }
}

describe('ordersState defaults', () => {
  it('defaults to order date descending with page 1 and 25 rows', () => {
    const state = baseState()
    expect(state.sort).toBe('orderDate')
    expect(state.sortDirection).toBe('desc')
    expect(state.page).toBe(1)
    expect(state.pageSize).toBe(25)
  })

  it('activeOrderFilterCount returns zero for a clean state', () => {
    expect(activeOrderFilterCount(baseState())).toBe(0)
  })

  it('counts each active filter independently', () => {
    const state: OrdersState = {
      ...baseState(),
      search: 'paint',
      supplier: 1,
      status: 'Active',
      currency: 2,
      visibility: 'Private',
      mine: true,
    }
    expect(activeOrderFilterCount(state)).toBe(6)
  })
})

describe('ordersState toListQuery', () => {
  it('maps a clean state to a minimal query', () => {
    const query = toListQuery(baseState(), null)
    expect(query.search).toBeNull()
    expect(query.supplier).toBeNull()
    expect(query.status).toBeNull()
    expect(query.currency).toBeNull()
    expect(query.visibility).toBeNull()
    expect(query.creator).toBeNull()
    expect(query.sort).toBe('orderDate')
    expect(query.sortDirection).toBe('desc')
  })

  it('passes trimmed search and active filters through', () => {
    const state: OrdersState = {
      ...baseState(),
      search: '  brushes  ',
      supplier: 4,
      status: 'Received',
      currency: 1,
      visibility: 'Public',
    }
    const query = toListQuery(state, null)
    expect(query.search).toBe('brushes')
    expect(query.supplier).toBe(4)
    expect(query.status).toBe('Received')
    expect(query.currency).toBe(1)
    expect(query.visibility).toBe('Public')
  })

  it('maps mine to the current user id and false to null', () => {
    expect(toListQuery({ ...baseState(), mine: true }, 9).creator).toBe(9)
    expect(toListQuery({ ...baseState(), mine: false }, 9).creator).toBeNull()
  })
})
