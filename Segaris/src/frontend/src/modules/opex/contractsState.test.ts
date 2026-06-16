import { describe, expect, it } from 'vitest'

import {
  activeFilterCount,
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  toListQuery,
} from './contractsState'
import type { ContractsState } from './contractsState'

function baseState(): ContractsState {
  return {
    search: '',
    type: '',
    status: '',
    frequency: '',
    category: null,
    supplier: null,
    costCenter: null,
    currency: null,
    visibility: '',
    mine: false,
    sort: defaultSort,
    sortDirection: defaultSortDirection,
    page: 1,
    pageSize: defaultPageSize,
  }
}

describe('contractsState defaults', () => {
  it('defaults to name ascending with page 1 and 25 rows', () => {
    const state = baseState()
    expect(state.sort).toBe('name')
    expect(state.sortDirection).toBe('asc')
    expect(state.page).toBe(1)
    expect(state.pageSize).toBe(25)
  })

  it('activeFilterCount returns zero for a clean state', () => {
    expect(activeFilterCount(baseState())).toBe(0)
  })

  it('counts each active filter independently', () => {
    const state: ContractsState = {
      ...baseState(),
      search: 'electricity',
      type: 'Expense',
      status: 'Active',
      frequency: 'Monthly',
      category: 1,
      supplier: 2,
      costCenter: 3,
      currency: 4,
      visibility: 'Private',
      mine: true,
    }
    expect(activeFilterCount(state)).toBe(10)
  })
})

describe('toListQuery', () => {
  it('maps a clean state to a minimal query', () => {
    const query = toListQuery(baseState(), null)
    expect(query.search).toBeNull()
    expect(query.type).toBeNull()
    expect(query.status).toBeNull()
    expect(query.frequency).toBeNull()
    expect(query.category).toBeNull()
    expect(query.supplier).toBeNull()
    expect(query.costCenter).toBeNull()
    expect(query.currency).toBeNull()
    expect(query.visibility).toBeNull()
    expect(query.creator).toBeNull()
    expect(query.page).toBe(1)
    expect(query.pageSize).toBe(25)
    expect(query.sort).toBe('name')
    expect(query.sortDirection).toBe('asc')
  })

  it('passes trimmed search and active filters through', () => {
    const state: ContractsState = {
      ...baseState(),
      search: '  water  ',
      type: 'Expense',
      status: 'Active',
      frequency: 'Monthly',
      category: 5,
      visibility: 'Public',
    }
    const query = toListQuery(state, null)
    expect(query.search).toBe('water')
    expect(query.type).toBe('Expense')
    expect(query.status).toBe('Active')
    expect(query.frequency).toBe('Monthly')
    expect(query.category).toBe(5)
    expect(query.visibility).toBe('Public')
  })

  it('maps mine to the current user id', () => {
    const state: ContractsState = { ...baseState(), mine: true }
    expect(toListQuery(state, 42).creator).toBe(42)
    expect(toListQuery(state, null).creator).toBeNull()
  })

  it('maps mine false to null creator', () => {
    const state: ContractsState = { ...baseState(), mine: false }
    expect(toListQuery(state, 7).creator).toBeNull()
  })
})
